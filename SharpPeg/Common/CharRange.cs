using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Common
{
    public struct CharRange
    {
        public char Min { get; }

        public char Max { get; }

        public CharRange(char min, char max)
        {
            if(min > max)
            {
                throw new ArgumentException("min should be less than max.");
            }

            Min = min;
            Max = max;
        }

        public override string ToString()
        {
            if(Min == Max)
            {
                return $"{Min}";
            }
            else
            {
                return $"{Min}-{Max}";
            }
        }
    }
}
