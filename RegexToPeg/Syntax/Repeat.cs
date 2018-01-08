using RegexToPeg.Syntax;

namespace RegexToPeg.Syntax
{
    internal class Repeat : Expression
    {
        public Expression Expression { get; }
        public RepeatRange RepeatRange { get; }

        public Repeat(Expression expression, RepeatRange repeatRange)
        {
            this.Expression = expression;
            this.RepeatRange = repeatRange;
        }
    }
}