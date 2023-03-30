using CollaborativeEditing.DocumentSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public abstract class OperationBase
    {
        public abstract string Type { get; }
    }



    public abstract class NodeOperationBase : OperationBase, IPathOperation
    {
        public int[] Path { get; set; }
    }

    public interface IPathOperation
    {
        public int[] Path { get; set; }
    }

    public interface IPositionOperation : IPathOperation
    {
        public int Position { get; set; }
    }

    public interface IOffsetOperation : IPathOperation
    {
        public int Offset { get; set; }
    }

    public class InsertNodeOperation : NodeOperationBase
    {
        public JsonNode Node { get; set; }

        public override string Type => "insert_node";
    }

    public class InsertTextOperation : NodeOperationBase, IOffsetOperation
    {
        public int Offset { get; set; }
        public string Text { get; set; }

        public override string Type => "insert_text";

        public override string ToString() => $"insert '{Text}'[{string.Join(", ", Path.Select(p => p.ToString()))}]@{Offset}";
    }

    public class MergeNodeOperation : NodeOperationBase, IPositionOperation
    {
        public int Position { get; set; }
        public JsonNode Properties { get; set; } // todo:
        public override string Type => "merge_node";
    }

    public class MoveNodeOperation : NodeOperationBase
    {
        public int[] NewPath { get; set; }
        public override string Type => "move_node";
    }

    public class RemoveNodeOperation : NodeOperationBase
    {
        public JsonNode Node { get; set; }
        public override string Type => "remove_node";
    }

    public class RemoveTextOperation : NodeOperationBase, IOffsetOperation
    {
        public int Offset { get; set; }
        public string Text { get; set; }
        public override string Type => "remove_text";

        public override string ToString() => $"remove '{Text}'[{string.Join(", ", Path.Select(p => p.ToString()))}]@{Offset}";
    }

    public class SetNodeOperation : NodeOperationBase
    {
        public JsonObject Properties { get; set; }
        public JsonObject NewProperties { get; set; }
        public override string Type => "set_node";
    }

    public class SplitNodeOperation : NodeOperationBase, IPositionOperation
    {
        public int Position { get; set; }
        public JsonNode Properties { get; set; }
        public override string Type => "split_node";
    }

    public class OperationFactory
    {
        public IEnumerable<OperationBase> FromJson(string operationsJson)
        {
            var jsonArray = JsonArray.Parse(operationsJson)?.AsArray();
            if (jsonArray == null)
            {
                throw new Exception("Failed to parse operation array");
            }

            return FromJson(jsonArray);
        }

        public IEnumerable<OperationBase> FromJson(JsonArray operationsJson)
        {
            foreach (var jsonItem in operationsJson)
            {
                var operation = GetOperation(jsonItem);
                if (operation != null)
                {
                    yield return operation;
                }
            }
        }

        public OperationBase Clone(OperationBase operation)
        {
            if (operation is InsertNodeOperation insertNodeOperation)
            {
                return new InsertNodeOperation()
                {
                    Node = insertNodeOperation.Node,
                    Path = insertNodeOperation.Path,
                };
            }
            if (operation is InsertTextOperation insertTextOperation)
            {
                return new InsertTextOperation()
                {
                    Text = insertTextOperation.Text,
                    Path = insertTextOperation.Path,
                    Offset = insertTextOperation.Offset
                };
            }
            if (operation is MergeNodeOperation mergeNodeOperation)
            {
                return new MergeNodeOperation()
                {
                    Path = mergeNodeOperation.Path,
                    Position = mergeNodeOperation.Position,
                    Properties = mergeNodeOperation.Properties,
                };
            }
            if (operation is MoveNodeOperation moveNodeOperation)
            {
                return new MoveNodeOperation()
                {
                    NewPath = moveNodeOperation.NewPath,
                    Path = moveNodeOperation.Path,
                };
            }
            if (operation is RemoveNodeOperation removeNodeOperation)
            {
                return new RemoveNodeOperation()
                {
                    Node = removeNodeOperation.Node,
                    Path = removeNodeOperation.Path,
                };
            }
            if (operation is RemoveTextOperation removeTextOperation)
            {
                return new RemoveTextOperation()
                {
                    Text = removeTextOperation.Text,
                    Offset = removeTextOperation.Offset,
                    Path = removeTextOperation.Path,
                };
            }
            if (operation is SetNodeOperation setNodeOperation)
            {
                return new SetNodeOperation()
                {
                    Properties = setNodeOperation.Properties,
                    NewProperties = setNodeOperation.NewProperties,
                    Path = setNodeOperation.Path,
                };
            }
            if (operation is SplitNodeOperation splitNodeOperation)
            {
                return new SplitNodeOperation()
                {
                    Path = splitNodeOperation.Path,
                    Position = splitNodeOperation.Position,
                    Properties = splitNodeOperation.Properties,
                };
            }

            throw new Exception($"Unsupported operation type: {operation.Type}");
        }

        private OperationBase? GetOperation(JsonNode? jsonNode)
        {
            var jsonObject = jsonNode?.AsObject();
            if (jsonObject != null && jsonObject.TryGetPropertyValue("type", out var typePropertyNode) && typePropertyNode != null)
            {
                var type = typePropertyNode.GetValue<string>();
                var serializerOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };

                switch (type)
                {
                    case "insert_node":
                        return jsonObject.Deserialize<InsertNodeOperation>(serializerOptions);
                    case "insert_text":
                        return jsonObject.Deserialize<InsertTextOperation>(serializerOptions);
                    case "merge_node":
                        return jsonObject.Deserialize<MergeNodeOperation>(serializerOptions);
                    case "move_node":
                        return jsonObject.Deserialize<MoveNodeOperation>(serializerOptions);
                    case "remove_node":
                        return jsonObject.Deserialize<RemoveNodeOperation>(serializerOptions);
                    case "remove_text":
                        return jsonObject.Deserialize<RemoveTextOperation>(serializerOptions);
                    case "set_node":
                        return jsonObject.Deserialize<SetNodeOperation>(serializerOptions);
                    case "split_node":
                        return jsonObject.Deserialize<SplitNodeOperation>(serializerOptions);
                    default:
                        throw new NotSupportedException($"Unsupported operation type '{type}'");
                }
            }

            throw new Exception("Failed to parse operation");
        }

    }
}