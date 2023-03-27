using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CollaborativeEditing
{
    public class Document
    {
        public int Revision { get; private set; } = 0;
        public JsonObject Content { get; private set; }


        public Document(int revision, string contentJson)
        {
            var node = JsonNode.Parse(contentJson);            

            if (node == null || node is JsonArray)
            {
                var root = new JsonObject();
                root.TryAdd("children", node ?? new JsonArray());
                Content = root;
            }
            else
            {
                Content = node.AsObject();
            }

            Revision = revision;
        }

        public string GetJson()
        {
            if (Content.TryGetPropertyValue("children", out var children) && children != null)
            {
                return children.ToJsonString();
            }

            return "";
        }

        public JsonObject GetNode(int[] path)
        {
            var currentElement = Content;
            foreach (var entry in path)
            {
                if (currentElement.TryGetPropertyValue("children", out var children) && children != null)
                {
                    currentElement = children.AsArray().ElementAt(entry)?.AsObject() ?? throw new Exception("Failed to obtain node by path"); ;
                }
                else
                {
                    throw new Exception("Failed to obtain node by path");
                }
            }

            return currentElement;
        }

        public JsonObject GetParent(int[] path)
        {
            if (path.Length == 1)
            {
                return Content;
            }

            return GetNode(path.Take(path.Length - 1).ToArray());
        }
    }
}
