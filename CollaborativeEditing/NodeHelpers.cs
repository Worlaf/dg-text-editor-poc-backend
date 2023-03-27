using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public static class NodeHelpers
    {
        public static bool IsText(this JsonNode node)
        {
            return node is JsonObject jsonObject && jsonObject.ContainsKey("text");
        }

        public static bool IsElement(this JsonNode node)
        {
            return node is JsonObject jsonObject && jsonObject.ContainsKey("children");
        }

        public static string GetText(this JsonObject node)
        {
            return node["text"]?.AsValue().GetValue<string>() ?? throw new Exception("Failed to obtain text value");
        }

        public static void SetText(this JsonObject node, string text)
        {
            node.Remove("text");
            node.Add("text", text);
        }

        public static JsonArray GetChildren(this JsonObject node)
        {
            return node["children"]?.AsArray() ?? throw new Exception("Failed to obtain node children");
        }

        public static void SetChildren(this JsonObject node, IEnumerable<JsonNode?> children)
        {
            node.Remove("children");
            node.Add("children", new JsonArray(children.Select(node => node.Deserialize<JsonNode>()).ToArray()));
        }

        public static void PushChildren(this JsonObject node, IEnumerable<JsonNode?> newChildren)
        {
            var children = node.GetChildren();
            foreach (var child in newChildren)
            {
                children.Add(child.Deserialize<JsonNode>());
            }
        }

        public static T Copy<T>(this T node) where T: JsonNode => node.Deserialize<T>() ?? throw new Exception("Failed to copy node");
    }
}
