using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.OutputProcessing
{
    public abstract class Node
    {
        public string Key { get; }

        protected Node(string key)
        {
            Key = key;
        }

        public override string ToString()
        {
            return Key;
        }
    }
}
