using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnusedAdvancesOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = context.Count -1 ; i >= 0; i--)
            {
                var instructionType = context[i].Type;
                if (instructionType == InstructionType.Advance)
                {
                    if(context[i].Offset == 0 || !IsNeeded(context, i))
                    {
                        changed = true;
                        context.RemoveAt(i);
                    }
                }
            }

            for (var i = 0; i < context.Count; i++)
            {
                if (context[i].Type == InstructionType.Advance && context[i + 1].Matches(InstructionType.Jump, out var jumpTargetLabel))
                {
                    var jumpTarget = context.GetLabelPosition(jumpTargetLabel) + 1;
                    if (context[jumpTarget].Type == InstructionType.Advance)
                    {
                        changed = true;
                        var newLabel = context.LabelAllocator++;
                        context[i] = Instruction.Advance((short)(context[i].Offset + context[jumpTarget].Offset));
                        context[i + 1] = Instruction.Jump(newLabel);

                        context.Insert(jumpTarget + 1, Instruction.MarkLabel(newLabel));
                    }
                }
            }

            return changed;
        }

        private static bool IsNeeded(OptimizationContext context, int i)
        {
            switch(context[i].Type)
            {
                case InstructionType.RestorePosition:
                    return false;
                case InstructionType.MarkLabel:
                case InstructionType.Advance:
                    return IsNeeded(context, i + 1);
                case InstructionType.Jump:
                    return IsNeeded(context, context.GetLabelPosition(context[i].Label));
                default:
                    return true;
            }
        }
    }
}
