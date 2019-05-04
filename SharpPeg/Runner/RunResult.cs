using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Runner
{
    public struct RunResult
    {
        public bool IsSuccessful => (Combined & 0x80000000) > 0;

        public int FailureLabel { get; }

        public int InputPosition { get; }

        public int ProgramCounter => (int)(Combined & 0x7fffffff);

        private uint Combined;

        public RunResult(bool successful, int failureLabel, int inputPos, int pc)
        {
            FailureLabel = failureLabel;
            InputPosition = inputPos;
            Combined = (successful ? 0x80000000 : 0) | (uint)pc;
        }
    }
}
