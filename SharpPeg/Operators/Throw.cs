using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Operators
{
    public class Throw : Operator
    {
        public int FailureLabel { get; }

        public override IEnumerable<Operator> Children => new Operator[0];

        public Throw(int failureLabel)
        {
            if(failureLabel <= 0 || failureLabel > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("Failure label must be 0 < failureLabel <= ushort.MaxValue.");
            }

            FailureLabel = failureLabel;
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping)
        {
            return this;
        }
    }
}
