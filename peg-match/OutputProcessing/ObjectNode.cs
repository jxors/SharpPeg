using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PegMatch.OutputProcessing
{
    public class ObjectNode : Node, IEnumerable<Node>
    {
        private List<Node> nodes = new List<Node>();

        public ObjectNode(string key, IEnumerable<Node> nodes) : base(key)
        {
            this.nodes = nodes.ToList();
        }

        public IEnumerator<Node> GetEnumerator()
        {
            return ((IEnumerable<Node>)nodes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Node>)nodes).GetEnumerator();
        }
    }
}
