using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner
{
    public class Capture : IComparable<Capture>
    {
        public int Key { get; }

        public int StartPosition { get; }

        public int EndPosition { get; set; }

        public int CloseIndex { get; set; }

        public Capture(int key, int startPosition) : this(key, startPosition, 0, 0)
        { }

        public Capture(int key, int startPosition, int endPosition, int closeIndex)
        {
            Key = key;
            StartPosition = startPosition;
            EndPosition = endPosition;
            CloseIndex = closeIndex;
        }

        public override string ToString()
        {
            return $"Capture #{Key}: ({StartPosition}...{EndPosition}) CloseIndex={CloseIndex}";
        }

        public int CompareTo(Capture other)
        {
            if(other.StartPosition == StartPosition)
            {
                // Sort descending on close index
                return other.CloseIndex - CloseIndex;
            }
            else
            {
                // Sort ascending on start position
                return StartPosition - other.StartPosition;
            }
        }
    }
}
