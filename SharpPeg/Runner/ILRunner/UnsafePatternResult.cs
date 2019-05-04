using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner.ILRunner
{
    public unsafe struct UnsafePatternResult
    {
        public int Label;

        public char* Position;

        public UnsafePatternResult(int label, char* position)
        {
            this.Label = label;
            this.Position = position;
        }
    }
}
