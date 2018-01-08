using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Operators
{
    public class CharacterRange
    {
        public char Min { get; }
        public char Max { get; }

        public int Size => Max - Min + 1;

        public CharacterRange(char min, char max)
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
