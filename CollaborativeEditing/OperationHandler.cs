using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class OperationHandler
    {
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

        public void Apply(Document document, InsertNodeOperation operation)
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

        public void Apply(Document document, InsertTextOperation operation)
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

        public void Apply(Document document, MergeNodeOperation operation)
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

        public void Apply(Document document, MoveNodeOperation operation)
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

        public void Apply(Document document, RemoveNodeOperation operation)
        {
            var index = operation.Path.Last();
            var parent = document.GetParent(operation.Path);

            parent.GetChildren().RemoveAt(index);
        }

        public void Apply(Document document, RemoveTextOperation operation)
        {
            if (operation.Text.Length == 0) return;

            var node = document.GetNode(operation.Path);
            if (!NodeHelpers.IsText(node)) throw new Exception("Can't remove text from non-text node");

            var text = node.GetText();
            var before = text.Substring(0, operation.Offset);
            var after = text.Substring(operation.Offset + operation.Text.Length);

            node.SetText(before + after);
        }

        public void Apply(Document document, SetNodeOperation operation)
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

        public void Apply(Document document, SplitNodeOperation operation)
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
    }
}
