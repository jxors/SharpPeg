using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner
{
    public class Capture : IComparable<Capture>
    {
        public int Key { get; }

        public int StartPosition { get; }

        public int EndPosition { get; }

        public int OpenIndex { get; }

        public int BasePosition { get; }
        
        public Capture(int key, int startPosition, int endPosition, int openIndex, int basePosition)
        {
            Key = key;
            StartPosition = startPosition;
            EndPosition = endPosition;
            OpenIndex = openIndex;
            BasePosition = basePosition;
        }

        public override string ToString()
        {
            return $"Capture #{Key}: ({StartPosition}...{EndPosition}) OpenIndex={OpenIndex}";
        }

        public int CompareTo(Capture other)
        {
            if (other.StartPosition != StartPosition)
            {
                // Sort ascending on start position
                return StartPosition - other.StartPosition;
            }

            if (other.OpenIndex != OpenIndex)
            {
                // Sort descending on open index
                return OpenIndex - other.OpenIndex;
            }

            return other.BasePosition - BasePosition;
        }
    }
}
