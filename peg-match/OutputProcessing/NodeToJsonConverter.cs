using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PegMatch.OutputProcessing
{
    public class NodeToJsonConverter
    {
        public Node Root { get; }
        public JsonSerializerSettings Settings { get; }

        public NodeToJsonConverter(Node root, JsonSerializerSettings settings = null)
        {
            Root = root;
            Settings = settings;
        }

        private JToken ToJObject(Node node)
        {
            switch(node)
            {
                case ListNode ln:
                    return new JArray(ln.Nodes.Select(child => ToJObject(child)));
                case ObjectNode on:
                    var obj = new JObject();
                    foreach(var item in on)
                    {
                        obj.Add(item.Key, ToJObject(item));
                    }

                    return obj;
                case ScalarNode sn:
                    return sn.Value;
            }

            throw new NotImplementedException();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(ToJObject(Root), Settings);
        }
    }
}
