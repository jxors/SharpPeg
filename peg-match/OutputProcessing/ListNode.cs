using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PegMatch.OutputProcessing
{
    public class ListNode : Node
    {
        public ListNode(string key, IEnumerable<Node> nodes) : base(key)
        {
            Nodes = nodes.ToList();
        }

        public List<Node> Nodes { get; }
    }
}
