using System;
using SharpPeg.Optimizations.Default.Analyzers;
using SharpPeg.Common;

namespace SharpPeg.Optimizations.Default
{
    public class ConsolidateBoundChecksOptimization : PushDown.PushDownOptimization
    {
        public ConsolidateBoundChecksOptimization() : base(InstructionType.BoundsCheck)
        { }

        protected override bool InnerOptimize(OptimizationContext context, Instruction matchedInstruction, Backtracer backtracer, int i)
        {
            var changed = false;
            var j = i;
            context.RemoveAt(j);

            while (!IsChainBreaker(context[j], matchedInstruction, null))
            {
                var instruction = context[j];
                if (instruction.Matches(InstructionType.BoundsCheck, matchedInstruction.Label))
                {
                    changed = true;
                    matchedInstruction = Instruction.BoundsCheck(instruction.Label, Math.Max(instruction.Offset, context[j].Offset));
                    context.RemoveAt(j);
                }
                else
                {
                    j++;
                }
            }

            context.Insert(i, matchedInstruction);

            return changed;
        }

        protected override bool IsChainBreaker(Instruction instruction, Instruction source, BacktracerView btv)
        {
            switch (instruction.Type)
            {
                case InstructionType.BoundsCheck:
                case InstructionType.Char:
                    return source.Label != instruction.Label;
            }

            return true;
        }
    }
}
