using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public class Empty : Operator
    {
        public override IEnumerable<Operator> Children => Enumerable.Empty<Operator>();

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => this;

        public override string ToString()
        {
            return $"e";
        }
    }
}
