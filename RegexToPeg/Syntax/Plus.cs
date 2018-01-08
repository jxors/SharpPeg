using System;
using System.Collections.Generic;
using System.Text;

namespace RegexToPeg.Syntax
{
    class Plus : Expression
    {
        public Expression Expression { get; }

        public Plus(Expression expression)
        {
            this.Expression = expression;
        }
    }
}
