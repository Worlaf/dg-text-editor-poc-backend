using CollaborativeEditing.DocumentSchema;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public abstract class NodeOperationBase : OperationBase
    {
        public int[] Path { get; set; }
    }

    public class InsertNodeOperation : NodeOperationBase
    {
        public JsonNode Node { get; set; }

        public override string Type => "insert_node";
    }

    public class InsertTextOperation : NodeOperationBase
    {
        public int Offset { get; set; }
        public string Text { get; set; }

        public override string Type => "insert_text";
    }

    public class MergeNodeOperation : NodeOperationBase
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

    public class RemoveTextOperation : NodeOperationBase
    {
        public int Offset { get; set; }
        public string Text { get; set; }
        public override string Type => "remove_text";
    }

    public class SetNodeOperation : NodeOperationBase
    {
        public JsonObject Properties { get; set; }
        public JsonObject NewProperties { get; set; }
        public override string Type => "set_node";
    }

    public class SplitNodeOperation : NodeOperationBase
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