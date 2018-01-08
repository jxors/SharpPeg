using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System.Collections.Generic;

namespace SharpPeg.Optimizations.Default
{
    public class DelayAdvanceOptimization : PushDown.PushDownOptimization
    {
        public DelayAdvanceOptimization() : base(InstructionType.Advance)
        { }

        protected override bool InnerOptimize(OptimizationContext context, Instruction matchedInstruction, Backtracer backtracer, int i)
        {
            var changed = false;
            var j = i;
            var patchedLabels = new Dictionary<(ushort label, short offset), ushort>();
            var offset = matchedInstruction.Offset;
            context.RemoveAt(j);

            while (!IsChainBreaker(context[j], Instruction.Advance(offset), null))
            {
                var instruction = context[j];
                switch (instruction.Type)
                {
                    case InstructionType.Advance:
                        changed = true;
                        offset += instruction.Offset;
                        context.RemoveAt(j);
                        break;
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                    case InstructionType.Call:
                        var targetLabel = instruction.Label;
                        if (context[context.GetLabelPosition(targetLabel) + 1].Type != InstructionType.RestorePosition)
                        {
                            if (patchedLabels.TryGetValue((targetLabel, offset), out var newTarget))
                            {
                                targetLabel = newTarget;
                            }
                            else if (offset != 0)
                            {
                                var oldLabel = targetLabel;
                                targetLabel = AddStub(context, targetLabel, Instruction.Advance(offset), ref j);
                                patchedLabels[(oldLabel, offset)] = targetLabel;
                            }
                        }

                        changed = true;
                        context[j] = new Instruction(instruction.Type, targetLabel, (short)(offset + instruction.Offset), instruction.Data1, instruction.Data2);

                        j++;
                        break;
                }
            }

            if (offset != 0)
            {
                context.Insert(j, Instruction.Advance(offset));
            }

            return changed || j != i;
        }

        protected override bool IsChainBreaker(Instruction instruction, Instruction source, BacktracerView btv)
        {
            switch (instruction.Type)
            {
                // TODO: Add an Advance to the jump target of these instructions so we can consolidate advances into just one advance at the end.
                case InstructionType.BoundsCheck:
                case InstructionType.Char:
                    return instruction.Offset + source.Offset < 0;
                case InstructionType.Advance:
                    return false;
            }

            return true;
        }
    }
}
