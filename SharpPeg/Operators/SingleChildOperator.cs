using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Operators
{
    public abstract class SingleChildOperator : Operator
    {
        public Operator Child { get; }

        public override IEnumerable<Operator> Children
        {
            get
            {
                yield return Child;
            }
        }

        public SingleChildOperator(Operator child)
        {
            this.Child = child;
        }
    }
}
