using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public class PrioritizedChoice : MultiChildOperator
    {
        public IReadOnlyCollection<int> CaughtFailureLabels;

        public PrioritizedChoice(Operator childA, Operator childB) : this(childA, childB, (IEnumerable<int>)null)
        { }

        public PrioritizedChoice(Operator childA, Operator childB, IEnumerable<int> caughtFailureLabels) : base(childA, childB)
        {
            if (caughtFailureLabels != null)
            {
                CaughtFailureLabels = caughtFailureLabels.ToList();
            }
        }

        public PrioritizedChoice(params Operator[] children) : this((IEnumerable<Operator>)children) { }

        private PrioritizedChoice() : base(null, null) { }

        public PrioritizedChoice(IEnumerable<Operator> children, IEnumerable<int> caughtFailureLabels) : base(null, null)
        {
            var stack = new Stack<Operator>(children);
            if (stack.Count <= 2)
            {
                ChildB = stack.Pop();
                ChildA = stack.Pop();
            }
            else
            {
                var current = new PrioritizedChoice(null, stack.Pop(), caughtFailureLabels);

                while (stack.Count > 2)
                {
                    current.ChildA = stack.Pop();
                    current = new PrioritizedChoice(null, current, caughtFailureLabels);
                }

                current.ChildA = stack.Pop();
                ChildA = stack.Pop();
                ChildB = current;
            }
        }


        public PrioritizedChoice(IEnumerable<Operator> children) : this(children, null)
        { }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new PrioritizedChoice(ChildA.Duplicate(mapping), ChildB.Duplicate(mapping));

        public override string ToString()
        {
            var needsNoBracketsA = NeedsNoBrackets(ChildA);
            var needsNoBracketsB = NeedsNoBrackets(ChildB) || ChildB is PrioritizedChoice;

            if (needsNoBracketsA && needsNoBracketsB)
            {
                return $"{ChildA} / {ChildB}";
            }
            else if(needsNoBracketsA)
            {
                return $"{ChildA} / ({ChildB})";
            }
            else if(needsNoBracketsB)
            {
                return $"({ChildA}) / {ChildB}";
            }

            return $"({ChildA}) / ({ChildB})";
        }
    }
}
