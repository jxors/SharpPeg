using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public abstract class OptimizationBase
    {
        public abstract bool Optimize(OptimizationContext context);

        protected static int[] CalculateLabelPositions(OptimizationContext context)
        {
            var labelPositions = new int[context.LabelAllocator];
            for (var i = 0; i < context.Count; i++)
            {
                if (context[i].Matches(InstructionType.MarkLabel, out var label))
                {
                    labelPositions[label] = i;
                }
            }

            return labelPositions;
        }

        protected ushort AddStub(OptimizationContext context, ushort targetLabel, Instruction instruction, ref int position)
        {
            var newLabel = context.LabelAllocator++;
            var labelPosition = context.GetLabelPosition(targetLabel);

            if (labelPosition > 0 && context[labelPosition - 1].IsEnding)
            {
                // No need for jump, because the instruction before the MarkLabel instruction will never advance the pc by 1
                context.InsertRange(labelPosition, new[]{
                    Instruction.MarkLabel(newLabel),
                    instruction,
                });

                if (labelPosition < position)
                {
                    position += 2;
                }
            }
            else
            {
                context.InsertRange(labelPosition, new[]{
                    Instruction.Jump(targetLabel),
                    Instruction.MarkLabel(newLabel),
                    instruction,
                });

                if (labelPosition < position)
                {
                    position += 3;
                }
            }

            return newLabel;
        }

        protected ushort AddStubByPosition(OptimizationContext context, int targetPosition, Instruction instruction)
        {
            var skipLabel = context.LabelAllocator++;
            var newLabel = context.LabelAllocator++;
            context.InsertRange(targetPosition, new[]{
                Instruction.Jump(skipLabel),
                Instruction.MarkLabel(newLabel),
                instruction,
                Instruction.MarkLabel(skipLabel),
            });

            return newLabel;
        }
    }
}
