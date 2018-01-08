using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegexToPeg.Syntax
{
    public class CharSet : Expression
    {
        public char[] Chars { get; }

        public CharSet(params char[] chars)
        {
            Chars = chars;
        }

        public CharSet(IEnumerable<char> chars)
        {
            Chars = chars.ToArray();
        }
    }
}
