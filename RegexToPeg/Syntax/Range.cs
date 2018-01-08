using System;
using System.Collections.Generic;
using System.Text;

namespace RegexToPeg.Syntax
{
    public class Range : Expression
    {
        public char Min { get; }
        public char Max { get; }

        public int Size => Max - Min + 1;

        public Range(char min, char max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString()
        {
            return $"{Min}-{Max} ({Size} characters)";
        }
    }
}
