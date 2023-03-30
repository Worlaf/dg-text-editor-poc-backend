using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class OperationHandler
    {
        public OperationBatch ApplyOperationBatch(Document document, OperationBatch batch, IEnumerable<OperationBatch> revisionLog)
        {
            if (document.Revision == batch.DocumentRevision)
            {
                Debug.WriteLine($">>>Apply batch {batch.DocumentRevision} vs {document.Revision}:\r\n{batch}");
                foreach (var operation in batch.Operations)
                {
                    ApplyOperation(document, operation);
                }

                document.IncreaseRevision();

                Debug.WriteLine($"Result: {document.GetJson()}\r\n\r\n");

                return batch;
            }
            else
            {                
                var revisionsToApply = revisionLog.Where(revision => revision.DocumentRevision >= batch.DocumentRevision);
                if (!revisionsToApply.Any())
                {
                    throw new Exception("No revision to apply transforms against!");
                }

                var transformedBatch = revisionsToApply.Aggregate(batch, (result, revisionToApply) => TransformOperationBatch(result, revisionToApply));

                return ApplyOperationBatch(document, transformedBatch, revisionLog);
            }
        }

        public void ApplyOperation(Document document, OperationBase operation)
        {
            if (operation is InsertNodeOperation insertNodeOperation) { Apply(document, insertNodeOperation); }
            if (operation is InsertTextOperation insertTextOperation) { Apply(document, insertTextOperation); }
            if (operation is MergeNodeOperation mergeNodeOperation) { Apply(document, mergeNodeOperation); }
            if (operation is MoveNodeOperation moveNodeOperation) { Apply(document, moveNodeOperation); }
            if (operation is RemoveNodeOperation removeNodeOperation) { Apply(document, removeNodeOperation); }
            if (operation is RemoveTextOperation removeTextOperation) { Apply(document, removeTextOperation); }
            if (operation is SetNodeOperation setNodeOperation) { Apply(document, setNodeOperation); }
            if (operation is SplitNodeOperation splitNodeOperation) { Apply(document, splitNodeOperation); }
        }

        private void Apply(Document document, InsertNodeOperation operation)
        {
            var parent = document.GetParent(operation.Path);
            var index = operation.Path.Last();

            if (parent.TryGetPropertyValue("children", out var parentChildrenProperty) && parentChildrenProperty != null)
            {
                var parentChildren = parentChildrenProperty.AsArray();

                if (index <= parentChildren.Count)
                {
                    parentChildren.Insert(index, operation.Node);

                    return;
                }
            }

            throw new Exception("Failed to execute Insert Node Operation");
        }

        private void Apply(Document document, InsertTextOperation operation)
        {
            var node = document.GetNode(operation.Path);
            // check if it is leaf?
            if (node.IsText())
            {
                var nodeText = node.GetText();
                var before = nodeText.Substring(0, operation.Offset);
                var after = nodeText.Substring(operation.Offset);

                node.SetText(before + operation.Text + after);
                return;
            }

            throw new Exception("Failed to execute Insert Text Operation");
        }

        private void Apply(Document document, MergeNodeOperation operation)
        {
            var node = document.GetNode(operation.Path);
            var prevPath = PathHelpers.GetPrevious(operation.Path);
            var prev = document.GetNode(prevPath);
            var parent = document.GetParent(operation.Path);
            var index = operation.Path.Last();

            if (node.IsText() && prev.IsText())
            {
                prev.SetText(prev.GetText() + node.GetText());
            }
            else if (node.IsElement() && prev.IsElement())
            {
                prev.PushChildren(node.GetChildren());
            }
            else
            {
                throw new Exception("Failed to merge nodes because of different interfaces");
            }

            parent.GetChildren().RemoveAt(index);
        }

        private void Apply(Document document, MoveNodeOperation operation)
        {
            if (PathHelpers.IsAncestor(operation.Path, operation.NewPath))
            {
                throw new Exception("Cannot move node inside itself");
            }

            var node = document.GetNode(operation.Path);
            var parent = document.GetParent(operation.Path);
            var index = operation.Path.Last();

            parent.GetChildren().RemoveAt(index);
            var truePath = PathHelpers.Transform(operation.Path, operation);
            if (truePath == null) throw new Exception("Failed to move node: failed to transform path");

            var newParent = document.GetParent(truePath);
            var newIndex = truePath.Last();

            newParent.GetChildren().Insert(newIndex, node); // probably need to copy node to detach from previous parent
        }

        private void Apply(Document document, RemoveNodeOperation operation)
        {
            var index = operation.Path.Last();
            var parent = document.GetParent(operation.Path);

            parent.GetChildren().RemoveAt(index);
        }

        private void Apply(Document document, RemoveTextOperation operation)
        {
            if (operation.Text.Length == 0) return;

            var node = document.GetNode(operation.Path);
            if (!NodeHelpers.IsText(node)) throw new Exception("Can't remove text from non-text node");

            var text = node.GetText();
            var before = text.Substring(0, operation.Offset);
            var after = text.Substring(operation.Offset + operation.Text.Length);

            node.SetText(before + after);
        }

        private void Apply(Document document, SetNodeOperation operation)
        {
            if (operation.Path.Length == 0) throw new Exception("Can't set properties for root node");

            var node = document.GetNode(operation.Path);
            foreach (var prop in operation.NewProperties)
            {
                if (prop.Key == "children" || prop.Key == "text") throw new Exception($"Cannot set the {prop.Key} property of nodes");

                var value = prop.Value?.AsValue();
                node.Remove(prop.Key);
                if (value != null)
                {
                    node.Add(prop.Key, value.Copy());
                }
            }

            foreach (var prop in operation.Properties)
            {
                if (!operation.NewProperties.ContainsKey(prop.Key))
                {
                    node.Remove(prop.Key);
                }
            }
        }

        private void Apply(Document document, SplitNodeOperation operation)
        {
            var node = document.GetNode(operation.Path);
            var parent = document.GetParent(operation.Path);
            var index = operation.Path.Last();

            JsonObject newNode;
            if (node.IsText())
            {
                var text = node.GetText();
                var before = text.Substring(0, operation.Position);
                var after = text.Substring(operation.Position);

                node.SetText(before);

                newNode = operation.Properties.AsObject();
                newNode.Add("text", after);
            }
            else if (node.IsElement())
            {
                var children = node.GetChildren();

                var before = children.Take(operation.Position);
                var after = children.Skip(operation.Position);

                node.SetChildren(before);

                newNode = operation.Properties.AsObject();
                newNode.SetChildren(after);
            }
            else
            {
                throw new Exception("Failed to execute Split Node Operation");
            }

            parent["children"]?.AsArray().Insert(index + 1, newNode);
        }

        // this logic should be same as on frontend
        // this is not correct implementation, just the basic
        private OperationBatch TransformOperationBatch(OperationBatch batch, OperationBatch transformAgainst)
        {
            if (transformAgainst.DocumentRevision - batch.DocumentRevision > 0) throw new Exception("Document revision of batch being transformed is too old");

            Debug.WriteLine($">>>Transform batch {batch.DocumentRevision} against {transformAgainst.DocumentRevision}.\r\nBatch:{batch}\r\nAgainst:{transformAgainst}");

            var transformedOperations = batch.Operations
                .Select(operation => TransformOperation(operation, transformAgainst))
                .OfType<OperationBase>()
                .ToArray();

            var result = new OperationBatch()
            {
                DocumentRevision = transformAgainst.DocumentRevision + 1,
                Operations = transformedOperations
            };

            Debug.WriteLine($"Result:{result}");

            return result;
        }

        private OperationBase? TransformOperation(OperationBase operation, OperationBatch transformAgainst)
        {
            var clone = new OperationFactory().Clone(operation);
            foreach (var against in transformAgainst.Operations)
            {
                if (against is IPathOperation againstPosition)
                {
                    if (clone is IPositionOperation positionOperation)
                    {
                        var result = TransformPoint(positionOperation.Path, positionOperation.Position, againstPosition);
                        if (result != null)
                        {
                            positionOperation.Position = result.Offset;
                            positionOperation.Path = result.Path;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (clone is IOffsetOperation offsetOperation)
                    {
                        var result = TransformPoint(offsetOperation.Path, offsetOperation.Offset, againstPosition);
                        if (result != null)
                        {
                            offsetOperation.Offset = result.Offset;
                            offsetOperation.Path = result.Path;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (clone is IPathOperation pathOperation)
                    {
                        var result = TransformPath(pathOperation.Path, againstPosition);
                        if (result != null)
                        {
                            pathOperation.Path = result;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }

            Debug.WriteLine($"Transform operation result: [{operation}] => [{clone}]");

            return clone;
        }

        private Point? TransformPoint(int[] initialPath, int initialOfset, IPathOperation operation, bool isBackwardAffinity = false)
        {
            var path = initialPath;
            var offset = initialOfset;

            if (operation is InsertNodeOperation || operation is MoveNodeOperation)
            {
                path = TransformPath(path, operation, isBackwardAffinity);
            }
            else if (operation is InsertTextOperation insertTextOperation)
            {
                if (
                    PathHelpers.Equals(operation.Path, path) &&
                    (insertTextOperation.Offset < offset ||
                      (insertTextOperation.Offset == offset && !isBackwardAffinity))
                  )
                {
                    offset += insertTextOperation.Text.Length;
                }
            }
            else if (operation is MergeNodeOperation mergeOperation)
            {
                if (PathHelpers.Equals(operation.Path, path))
                {
                    offset += mergeOperation.Position;
                }

                path = TransformPath(path, operation, isBackwardAffinity);
            }
            else if (operation is RemoveTextOperation removeTextOperation)
            {
                if (PathHelpers.Equals(operation.Path, path) && removeTextOperation.Offset <= offset)
                {
                    offset -= Math.Min(offset - removeTextOperation.Offset, removeTextOperation.Text.Length);
                }
            }
            else if (operation is RemoveNodeOperation removeNodeOperation)
            {
                if (PathHelpers.Equals(operation.Path, path) || PathHelpers.IsAncestor(operation.Path, path))
                {
                    return null;
                }

                path = TransformPath(path, operation, isBackwardAffinity);
            }
            else if (operation is SplitNodeOperation splitNodeOperation)
            {
                if (PathHelpers.Equals(operation.Path, path))
                {
                    if (
                          splitNodeOperation.Position < offset ||
                          (splitNodeOperation.Position == offset && !isBackwardAffinity)
                        )
                    {
                        offset -= splitNodeOperation.Position;

                        path = TransformPath(path, operation);
                    }
                }
                else
                {
                    path = TransformPath(path, operation, isBackwardAffinity);
                }
            }

            if (path == null)
            {
                throw new Exception("path can't be null :(");
            }

            return new Point
            {
                Offset = offset,
                Path = path,
            };
        }

        private int[]? TransformPath(int[] path, IPathOperation operation, bool isBackwardAffinity = false)
        {
            var p = new int[path.Length];
            path.CopyTo(p, 0);

            if (path.Length == 0) return path;

            if (operation is InsertNodeOperation insertNodeOperation)
            {
                var op = insertNodeOperation.Path;

                if (PathHelpers.Equals(op, p) || PathHelpers.EndsBefore(op, p) || PathHelpers.IsAncestor(op, p))
                {
                    p[op.Length - 1] += 1;
                }
            }
            else if (operation is RemoveNodeOperation removeNodeOperation)
            {
                var op = removeNodeOperation.Path;

                if (PathHelpers.Equals(op, p) || PathHelpers.IsAncestor(op, p))
                {
                    return null;
                }
                else if (PathHelpers.EndsBefore(op, p))
                {
                    p[op.Length - 1] -= 1;
                }
            }
            else if (operation is MergeNodeOperation mergeNodeOperation)
            {
                var op = mergeNodeOperation.Path;

                if (PathHelpers.Equals(op, p) || PathHelpers.EndsBefore(op, p))
                {
                    p[op.Length - 1] -= 1;
                }
                else if (PathHelpers.IsAncestor(op, p))
                {
                    p[op.Length - 1] -= 1;
                    p[op.Length] += mergeNodeOperation.Position;
                }
            }
            else if (operation is SplitNodeOperation splitNodeOperation)
            {
                var op = splitNodeOperation.Path;
                var position = splitNodeOperation.Position;

                if (PathHelpers.Equals(op, p))
                {
                    if (!isBackwardAffinity)
                    {
                        p[p.Length - 1] += 1;
                    }
                }
                else if (PathHelpers.EndsBefore(op, p))
                {
                    p[op.Length - 1] += 1;
                }
                else if (PathHelpers.IsAncestor(op, p) && path[op.Length] >= position)
                {
                    p[op.Length - 1] += 1;
                    p[op.Length] -= position;
                }
            }
            else if (operation is MoveNodeOperation moveNodeOperation)
            {
                var op = moveNodeOperation.Path;
                var onp = moveNodeOperation.NewPath;

                if (PathHelpers.Equals(op, onp))
                {
                    return p;
                }

                if (PathHelpers.IsAncestor(op, p) || PathHelpers.Equals(op, p))
                {
                    var copy = onp.Skip(0).ToArray();

                    if (PathHelpers.EndsBefore(op, onp) && op.Length < onp.Length)
                    {
                        copy[op.Length - 1] -= 1;
                    }

                    return copy.Concat(p.Skip(op.Length)).ToArray();
                }
                else if (PathHelpers.IsSibling(op, onp) && (PathHelpers.IsAncestor(onp, p) || PathHelpers.Equals(onp, p)))
                {
                    if (PathHelpers.EndsBefore(op, p))
                    {
                        p[op.Length - 1] -= 1;
                    }
                    else
                    {
                        p[op.Length - 1] += 1;
                    }
                }
                else if (PathHelpers.EndsBefore(onp, p) || PathHelpers.Equals(onp, p) || PathHelpers.IsAncestor(onp, p)
                )
                {
                    if (PathHelpers.EndsBefore(op, p))
                    {
                        p[op.Length - 1] -= 1;
                    }

                    p[onp.Length - 1] += 1;
                }
                else if (PathHelpers.EndsBefore(op, p))
                {
                    if (PathHelpers.Equals(onp, p))
                    {
                        p[onp.Length - 1] += 1;
                    }

                    p[op.Length - 1] -= 1;
                }

            }

            return p;
        }

        private class Point
        {
            public int[] Path { get; set; }
            public int Offset { get; set; }
        }
    }
}
