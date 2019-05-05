using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class PrefixUnificationOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            // TODO: We could support contained store/restore positions here as long as the values don't leak outside the area we're replacing
            for (var i = 0; i < context.Count; i++)
            {
                if(context[i].Matches(InstructionType.StorePosition))
                {
                    for(var j = 0; j < context.Count - 1; j++)
                    {
                        if(context[j].Matches(InstructionType.MarkLabel, out var label)
                            && context[j + 1].Matches(InstructionType.RestorePosition) 
                            && context[j + 1].Data1 == context[i].Data1)
                        {
                            var leftPosition = i + 1;
                            var rightPosition = j + 2;
                            var length = 0;
                            while (leftPosition + length < context.Count
                                && rightPosition + length < context.Count
                                && Unifies(context, leftPosition, rightPosition, length + 1) )
                            {
                                length++;
                            }

                            var replaceableLength = 0;
                            while(leftPosition + replaceableLength < context.Count
                                && GetJumpTargets(context, context[leftPosition + replaceableLength]).All(target => target == label)
                                && !context[leftPosition + replaceableLength].IsEnding
                                && context[leftPosition + replaceableLength].Type != InstructionType.StorePosition
                                && context[leftPosition + replaceableLength].Type != InstructionType.RestorePosition
                                && context[leftPosition + replaceableLength].Type != InstructionType.MarkLabel)
                            {
                                replaceableLength++;
                            }

                            if (length > 0 && replaceableLength > length)
                            {
                                changed = true;
                                // Hooray! We can optimize this.
                                // Replace left fail labels with right fail labels:
                                // TODO: This doesn't work if we want to support Stores & Restores
                                for (var k = 0; k < length; k++)
                                {
                                    context[leftPosition + k] = context[rightPosition + k];
                                }

                                var newVariable = context.VariableAllocator++;
                                var fastStartLabel = context.LabelAllocator++;
                                context.Insert(leftPosition + length, Instruction.StorePosition(newVariable));
                                if(rightPosition > leftPosition) { rightPosition++; }

                                var oldRightPosition = rightPosition;
                                var newLabel = AddStub(context, label, new[] {
                                    Instruction.RestorePosition(0, newVariable),
                                    Instruction.Jump(fastStartLabel),
                                }, ref rightPosition);

                                if(rightPosition < leftPosition) { leftPosition += rightPosition - oldRightPosition; }

                                context.Insert(rightPosition + length, Instruction.MarkLabel(fastStartLabel));

                                for(var k = length + 1; k < replaceableLength + 1; k++)
                                {
                                    if(context[k + leftPosition].CanJumpToLabel)
                                    {
                                        context[k + leftPosition] = context[k + leftPosition].WithLabel(newLabel);
                                    } else if(context[k + leftPosition].Type == InstructionType.Call)
                                    {
                                        var result = (ushort)context.FailureLabelMap.Count;
                                        context.FailureLabelMap.Add(new LabelMap(context.FailureLabelMap[context[k + leftPosition].Data2].Mapping.Select(kvp => (kvp.failureLabel, newLabel))));
                                        context[k + leftPosition] = context[k + leftPosition].WithData2(result);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return changed;
        }

        private bool Unifies(OptimizationContext context, int leftOffset, int rightOffset, int length)
        {
            var labelMapping = new Dictionary<ushort, ushort>();
            var variableMapping = new Dictionary<ushort, ushort>();

            for(var i = 0; i < length; i++)
            {
                var left = context[leftOffset + i];
                var right = context[rightOffset + i];

                if(left.Type != right.Type
                    || left.Data1 != right.Data1
                    || (left.Type != InstructionType.Call && right.Type != InstructionType.Call && left.Data2 != right.Data2)
                    || left.Type == InstructionType.RestorePosition
                    || left.Type == InstructionType.StorePosition
                    || left.Type == InstructionType.MarkLabel)
                {
                    return false;
                }

                if (left.Type != InstructionType.Call)
                {
                    if(!Unify(variableMapping, left.Label, right.Label))
                    {
                        return false;
                    }
                }
                else
                {
                    var leftMapping = context.FailureLabelMap[left.Data2];
                    var rightMapping = context.FailureLabelMap[right.Data2];
                    if (leftMapping.Mapping.Count != rightMapping.Mapping.Count)
                    {
                        return false;
                    }

                    foreach (var (failureLabel, jumpTarget) in leftMapping.Mapping)
                    {
                        if(!rightMapping.TryGet(failureLabel, out var rightLabel))
                        {
                            return false;
                        }

                        if(!Unify(variableMapping, jumpTarget, rightLabel))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Unify(Dictionary<ushort, ushort> variableMapping, ushort leftLabel, ushort rightLabel)
        {
            if (variableMapping.TryGetValue(leftLabel, out var storedRightLabel))
            {
                if (storedRightLabel != rightLabel)
                {
                    return false;
                }
            }
            else
            {
                variableMapping[leftLabel] = rightLabel;
            }

            return true;
        }
    }
}
