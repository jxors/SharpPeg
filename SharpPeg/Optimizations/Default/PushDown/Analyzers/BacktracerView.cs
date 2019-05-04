using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    public class BacktracerView
    {
        private readonly Backtracer backtracer;
        private readonly int startPosition;
        private readonly bool firstInstructionWasJump;
        private Dictionary<(int offset, ushort data1, ushort data2), EvaluationResult> cachedCharResults = new Dictionary<(int offset, ushort data1, ushort data2), EvaluationResult>();

        public BacktracerView(Backtracer backtracer, int startPosition, bool firstInstructionWasJump)
        {
            this.backtracer = backtracer;
            this.startPosition = startPosition;
            this.firstInstructionWasJump = firstInstructionWasJump;
        }

        public EvaluationResult CheckBounds(int offset)
        {
            return backtracer.CheckBounds(startPosition, firstInstructionWasJump, offset);
        }

        public EvaluationResult CheckChars(Instruction oneOfTheseMustSucceed, int offset)
        {
            var key = (offset, oneOfTheseMustSucceed.Data1, oneOfTheseMustSucceed.Data2);
            if (cachedCharResults.TryGetValue(key, out var result))
            {
                return result;
            }

            return cachedCharResults[key] = backtracer.CheckChars(startPosition, firstInstructionWasJump, oneOfTheseMustSucceed, offset);
        }
    }
}
