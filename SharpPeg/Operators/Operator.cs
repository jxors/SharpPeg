using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public abstract class Operator
    {
        public static Operator Optional(Operator op)
        {
            return new PrioritizedChoice(new[] { op, new Empty() });
        }

        public static Operator OneOrMore(Operator op)
        {
            return new Sequence(new[] { op, new ZeroOrMore(op) });
        }

        public static Operator And(Operator op)
        {
            return new Not(new Not(op));
        }

        public abstract IEnumerable<Operator> Children { get; }

        public Operator Duplicate(Dictionary<Operator, Operator> mapping)
        {
            if(!mapping.TryGetValue(this, out var newOperator))
            {
                newOperator =  mapping[this] = DuplicateInternal(mapping);
            }

            return newOperator;
        }

        protected abstract Operator DuplicateInternal(Dictionary<Operator, Operator> mapping);

        public IEnumerable<Operator> GetDescendants()
        {
            var stack = new Stack<Operator>(Children);
            while(stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                foreach (var child in current.Children.Reverse())
                {
                    stack.Push(child);
                }
            }
        }

        protected bool NeedsNoBrackets(Operator op)
        {
            return op is CharacterClass || op is ZeroOrMore || op is Empty || op is Any || (op is Sequence s && s.IsPartOfString);
        }

        public static Operator EndingWith(Operator loop, Operator ending)
        {
            return new Sequence(new ZeroOrMore(new Sequence(new Not(ending), loop)), ending);
        }

        public static Operator EndingWithGreedy(Operator loop, Operator ending)
        {
            return new Sequence(
                new ZeroOrMore(
                    new Sequence(
                        new Not(
                            new Sequence(
                                And(ending),
                                new Not(
                                    new Sequence(
                                        loop,
                                        new ZeroOrMore(
                                            new Sequence(new Not(ending), loop)
                                        ),
                                        ending
                                    )
                                )
                            )
                        ),
                        loop
                    )
                ),
                ending
            );

            // 'Flatter' expression, just as fast as above
            //return new Sequence(
            //    new ZeroOrMore(
            //        new PrioritizedChoice(
            //            new Sequence(
            //                And(ending), 
            //                OneOrMore(
            //                    new Sequence(
            //                        loop, 
            //                        new ZeroOrMore(new Sequence(new Not(ending), loop)), 
            //                        And(ending)
            //                    )
            //                )
            //            ),
            //            new Sequence(new Not(ending), loop)
            //        )
            //    ),
            //    ending
            //);
        }

        public static Operator Repeat(int n, Operator op)
        {
            if(n == 0)
            {
                return new Empty();
            }else if(n == 1)
            {
                return op;
            }
            else
            {
                return new Sequence(Enumerable.Range(0, n).Select(_ => op));
            }
        }

        public static implicit operator Operator(char c) => new CharacterClass(c);
        public static implicit operator Operator(string s) => CharacterClass.String(s);
        public static Operator operator /(Operator a, Operator b) => new PrioritizedChoice(a, b);
        public static Operator operator /(Operator a, string b) => new PrioritizedChoice(a, CharacterClass.String(b));
        public static Operator operator /(string a, Operator b) => new PrioritizedChoice(CharacterClass.String(a), b);
    }
}
