using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public class Sequence : MultiChildOperator
    {
        public Sequence(Operator childA, Operator childB) : base(childA, childB)
        {
        }

        public Sequence(params Operator[] children) : this((IEnumerable<Operator>)children) { }

        public Sequence(IEnumerable<Operator> children) : base(null, null)
        {
            var stack = new Stack<Operator>(children);
            if (stack.Count <= 2)
            {
                ChildB = stack.Pop();
                ChildA = stack.Pop();
            }
            else
            {
                var current = new Sequence(null, stack.Pop());

                while (stack.Count > 2)
                {
                    current.ChildA = stack.Pop();
                    current = new Sequence(null, current);
                }

                current.ChildA = stack.Pop();
                ChildA = stack.Pop();
                ChildB = current;
            }
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new Sequence(ChildA.Duplicate(mapping), ChildB.Duplicate(mapping));

        public bool IsPartOfString => ((ChildA is CharacterClass ccA && ccA.NumChars == 1) || (ChildA is Sequence sA && sA.IsPartOfString))
            && ((ChildB is CharacterClass ccB && ccB.NumChars == 1) || (ChildB is Sequence sB && sB.IsPartOfString));

        private void SubstringToString(StringBuilder str)
        {
            if(ChildA is CharacterClass ccA)
            {
                str.Append(ccA.Value.First());
            }else if(ChildA is Sequence sA)
            {
                sA.SubstringToString(str);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (ChildB is CharacterClass ccB)
            {
                str.Append(ccB.Value.First());
            }
            else if (ChildB is Sequence sB)
            {
                sB.SubstringToString(str);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            if(IsPartOfString)
            {
                var str = new StringBuilder();
                str.Append("'");
                SubstringToString(str);
                str.Append("'");

                return str.ToString();
            }

            if(NeedsNoBrackets(ChildA))
            {
                if(NeedsNoBrackets(ChildB))
                {
                    return $"{ChildA} {ChildB}";
                }
                else
                {
                    return $"{ChildA} ({ChildB})";
                }
            }else if(NeedsNoBrackets(ChildB))
            {
                return $"({ChildA}) {ChildB}";
            }

            return $"({ChildA}) ({ChildB})";
        }
    }
}
