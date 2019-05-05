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

        protected static IEnumerable<ushort> GetJumpTargets(OptimizationContext context, Instruction instruction)
        {
            if(instruction.CanJumpToLabel)
            {
                yield return instruction.Label;
            } else if(instruction.Type == InstructionType.Call)
            {
                foreach(var kvp in context.FailureLabelMap[instruction.Data2].Mapping)
                {
                    yield return kvp.jumpTarget;
                }
            }
        }

        protected ushort AddStub(OptimizationContext context, ushort targetLabel, Instruction instruction, ref int position)
        {
            return AddStub(context, targetLabel, new[] { instruction }, ref position);
        }

        protected ushort AddStub(OptimizationContext context, ushort targetLabel, Instruction[] instructions, ref int position)
        {
            var labelPosition = context.GetLabelPosition(targetLabel);

            if (labelPosition > 0 && context[labelPosition - 1].IsEnding)
            {
                var newLabel = context.LabelAllocator++;
                // No need for jump, because the instruction before the MarkLabel instruction will never advance the pc by 1
                context.InsertRange(labelPosition, new[]{
                    Instruction.MarkLabel(newLabel),
                }.Concat(instructions).ToArray());

                if (labelPosition < position)
                {
                    position += 1 + instructions.Length;
                }

                return newLabel;
            }
            else
            {
                var existing = FindOrMarkExistingStub(context, instructions, targetLabel, ref position);

                if (existing.HasValue)
                {
                    return existing.Value;
                }
                else
                {
                    var newLabel = context.LabelAllocator++;
                        context.InsertRange(labelPosition, new[]{
                        Instruction.Jump(targetLabel),
                        Instruction.MarkLabel(newLabel),
                    }.Concat(instructions).ToArray());

                    if (labelPosition < position)
                    {
                        position += 2 + instructions.Length;
                    }

                    return newLabel;
                }
            }
        }

        protected ushort? FindOrMarkExistingStub(OptimizationContext context, Instruction[] neededInstructions, ushort targetLabel, ref int position)
        {
            for (var i = 0; i < context.Count; i++)
            {
                if (SequencesEqual(context, i, neededInstructions)
                    && (context[i + neededInstructions.Length].Matches(InstructionType.MarkLabel, targetLabel) || context[i + neededInstructions.Length].Matches(InstructionType.Jump, targetLabel)))
                {
                    if (i > 0 && context[i - 1].Matches(InstructionType.MarkLabel, out var existingLabel))
                    {
                        return existingLabel;
                    }
                    else
                    {
                        var newLabel = context.LabelAllocator++;
                        context.Insert(i, Instruction.MarkLabel(newLabel));
                        if (position >= i)
                        {
                            position++;
                        }

                        return newLabel;
                    }
                }
            }

            return null;
        }

        protected bool SequencesEqual(OptimizationContext context, int offset, Instruction[] neededInstructions)
        {
            for (var j = 0; j < neededInstructions.Length; j++)
            {
                if (context[offset + j] != neededInstructions[j])
                {
                    return false;
                }
            }

            return true;
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
