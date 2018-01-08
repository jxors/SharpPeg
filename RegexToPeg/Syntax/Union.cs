using System;
using System.Collections.Generic;
using System.Text;

namespace RegexToPeg.Syntax
{
    class Union : Expression
    {
        public Expression ChildA { get; private set; }
        public Expression ChildB { get; private set; }

        public Union(Expression childA, Expression childB)
        {
            this.ChildA = childA;
            this.ChildB = childB;
        }

        public Union(params Expression[] children) : this((IEnumerable<Expression>)children)
        { }

        public Union(IEnumerable<Expression> children)
        {
            var stack = new Stack<Expression>(children);
            if (stack.Count <= 2)
            {
                ChildB = stack.Pop();
                ChildA = stack.Pop();
            }
            else
            {
                var current = new Union(null, stack.Pop());

                while (stack.Count > 2)
                {
                    current.ChildA = stack.Pop();
                    current = new Union(null, current);
                }

                current.ChildA = stack.Pop();
                ChildA = stack.Pop();
                ChildB = current;
            }
        }
    }
}
