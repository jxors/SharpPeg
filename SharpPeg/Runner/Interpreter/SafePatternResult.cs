using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner.Interpreter
{
    public struct SafePatternResult
    {
        public int Label;

        public int Position;

        public SafePatternResult(int label, int position)
        {
            this.Label = label;
            this.Position = position;
        }
    }
}
