using System;
using System.Collections.Generic;
using System.Text;

namespace RegexToPeg.Syntax
{
    class Concatenation : Expression
    {
        public Expression ChildA { get; private set; }
        public Expression ChildB { get; private set; }

        public Concatenation(Expression childA, Expression childB)
        {
            this.ChildA = childA;
            this.ChildB = childB;
        }

        public Concatenation(params Expression[] children) : this((IEnumerable<Expression>)children)
        { }

        public Concatenation(IEnumerable<Expression> children)
        {
            var stack = new Stack<Expression>(children);
            if (stack.Count <= 2)
            {
                ChildB = stack.Pop();
                ChildA = stack.Pop();
            }
            else
            {
                var current = new Concatenation(null, stack.Pop());

                while (stack.Count > 2)
                {
                    current.ChildA = stack.Pop();
                    current = new Concatenation(null, current);
                }

                current.ChildA = stack.Pop();
                ChildA = stack.Pop();
                ChildB = current;
            }
        }
    }
}
