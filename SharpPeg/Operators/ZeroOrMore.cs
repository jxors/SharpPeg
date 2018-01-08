using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Operators
{
    public class ZeroOrMore : SingleChildOperator
    {
        public ZeroOrMore(Operator child) : base(child)
        { }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new ZeroOrMore(Child.Duplicate(mapping));

        public override string ToString()
        {
            if (NeedsNoBrackets(Child))
            {
                return $"{Child}*";
            }
            else
            {
                return $"({Child})*";
            }
        }
    }
}
