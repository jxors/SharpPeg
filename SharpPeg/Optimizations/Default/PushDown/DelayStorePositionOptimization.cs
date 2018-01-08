using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System.Collections.Generic;

namespace SharpPeg.Optimizations.Default
{
    public class DelayStorePositionOptimization : PushDown.PushDownOptimization
    {
        public DelayStorePositionOptimization() : base(InstructionType.StorePosition)
        { }

        protected override bool InnerOptimize(OptimizationContext context, Instruction matchedInstruction, Backtracer backtracer, int i)
        {
            var changed = false;
            var j = i;
            var patchedLabels = new Dictionary<ushort, ushort>();
            context.RemoveAt(j);

            while (!IsChainBreaker(context[j], matchedInstruction, null))
            {
                var instruction = context[j];
                switch (instruction.Type)
                {
                    case InstructionType.StorePosition:
                        j++;
                        break;
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                        var targetLabel = instruction.Label;
                        if (patchedLabels.TryGetValue(targetLabel, out var newTarget))
                        {
                            targetLabel = newTarget;
                        }
                        else
                        {
                            var oldLabel = targetLabel;
                            targetLabel = AddStub(context, targetLabel, matchedInstruction, ref j);
                            patchedLabels[oldLabel] = targetLabel;
                        }

                        changed = true;
                        context[j] = new Instruction(instruction.Type, targetLabel, instruction.Offset, instruction.Data1, instruction.Data2);
                        j++;
                        break;
                }
            }

            context.Insert(j, matchedInstruction);

            return changed;
        }

        protected override bool IsChainBreaker(Instruction instruction, Instruction source, BacktracerView btv)
        {
            switch (instruction.Type)
            {
                // TODO: Add an Advance to the jump target of these instructions so we can consolidate advances into just one advance at the end.
                case InstructionType.StorePosition:
                case InstructionType.BoundsCheck:
                case InstructionType.Char:
                    return false;
            }

            return true;
        }
    }
}
