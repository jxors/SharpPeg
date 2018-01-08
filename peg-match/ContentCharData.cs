using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch
{
    class ContentCharData
    {
        public char[] Data { get; }
        public int Length { get; }

        public ContentCharData(char[] data, int length)
        {
            Data = data;
            Length = length;
        }
    }
}
