using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public abstract class MultiChildOperator : Operator
    {
        public Operator ChildA { get; protected set; }

        public Operator ChildB { get; protected set; }

        public override IEnumerable<Operator> Children
        {
            get
            {
                yield return ChildA;
                yield return ChildB;
            }
        }

        public MultiChildOperator(Operator childA, Operator childB)
        {
            this.ChildA = childA;
            this.ChildB = childB;
        }
    }
}
