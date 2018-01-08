using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.OutputProcessing
{
    public class ScalarNode : Node
    {
        public string Value { get; }

        public ScalarNode(string key, string value) : base(key)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"'{Value}'";
        }
    }
}
