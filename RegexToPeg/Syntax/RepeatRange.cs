using RegexToPeg.Syntax;

namespace RegexToPeg.Syntax
{
    internal class RepeatRange : Expression
    {
        public int Min { get; }
        public int Max { get; }

        public RepeatRange(int min, int max)
        {
            this.Min = min;
            this.Max = max;
        }
    }
}