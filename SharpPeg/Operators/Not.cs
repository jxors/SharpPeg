using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Operators
{
    public class Not : SingleChildOperator
    {
        public Not(Operator child) : base(child)
        { }

        public override string ToString()
        {
            if (NeedsNoBrackets(Child))
            {
                return $"!{Child}";
            }
            else
            {
                return $"!({Child})";
            }
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new Not(Child.Duplicate(mapping));
    }
}
