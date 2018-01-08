using System;
using System.Collections.Generic;
using System.Text;

namespace RegexToPeg.Syntax
{
    class Star : Expression
    {
        public Expression Expression { get; }

        public Star(Expression expression)
        {
            this.Expression = expression;
        }
    }
}
