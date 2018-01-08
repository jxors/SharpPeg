using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class DeduplicationOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            changed |= Deduplicate(context);
            //changed |= Stitch(context);

            return changed;
        }

        private bool Stitch(OptimizationContext context)
        {
            var changed = false;
            for (var i = 1; i < context.Count; i++)
            {
                var instruction = context[i];
                if (instruction.Matches(InstructionType.MarkLabel)
                    && context[i - 1].IsEnding)
                {
                    var j = i + 1;
                    while(j < context.Count && !context[j].Matches(InstructionType.MarkLabel))
                    {
                        if(context[j].Matches(InstructionType.Jump, out var secondLabel))
                        {
                            var targetPosition = context.GetLabelPosition(secondLabel);
                            if (targetPosition > 0)
                            {
                                if (context[targetPosition - 1].IsEnding)
                                {
                                    // Don't include jump
                                    var toInline = context.Skip(i).Take(j - i).ToArray();

                                    if (i < targetPosition)
                                    {
                                        context.InsertRange(targetPosition, toInline);
                                        context.RemoveRange(i, j - i + 1); // Include jump
                                    }
                                    else
                                    {
                                        context.InsertRange(targetPosition, toInline);
                                        context.RemoveRange(i, j - i + 1); // Include jump
                                    }

                                    changed = true;
                                }
                            }

                            break;
                        }

                        j++;
                    }
                }
            }

            return changed;
        }

        private static bool Deduplicate(OptimizationContext context)
        {
            var changed = false;
            for (var i = 0; i < context.Count; i++)
            {
                if (context[i].Type == InstructionType.MarkLabel)
                {
                    var label = context[i].Label;
                    var refPoint = -1;
                    var refCount = int.MaxValue;
                    var callSources = new List<int>();

                    for (var j = 1; j < context.Count && refCount > 0; j++)
                    {
                        if (context[j].Matches(InstructionType.Jump, label))
                        {
                            var source = j - 1;
                            if (refPoint == -1)
                            {
                                refPoint = source;
                                callSources.Add(source);
                            }
                            else
                            {
                                var newCount = CheckEquality(context, refPoint, refCount, source);
                                if (newCount != 0)
                                {
                                    refCount = newCount;
                                    callSources.Add(source);
                                }
                            }
                        }
                    }

                    if (refPoint != -1 && i != 0 && refCount > 0)
                    {
                        var includesFallthrough = false;
                        if (i > 0 && context[i - 1].Type != InstructionType.Jump)
                        {
                            var newCount = CheckEquality(context, refPoint, refCount, i - 1);
                            if (newCount != 0)
                            {
                                callSources.Add(i - 1);
                                refCount = newCount;
                                includesFallthrough = true;
                            }
                        }

                        if (refCount > 0 && callSources.Count > 1)
                        {
                            // Ugly hack to ensure that we're not deduplicating code from a loop
                            // This is undesired because it leads to situations like this:
                            //
                            //  | while (position + 1 < this.dataEndPtr && position[1] - 'a' <= '...')
                            //
                            // (Note the +1 in the loop condition, caused by pushing an Advance-instruction one full iteration through the loop)
                            if (callSources.All(p => p < i))
                            {
                                changed = true;
                                var newLabel = context.LabelAllocator++;
                                callSources.Sort();

                                var duplicateInstructions = context
                                    .Skip(callSources.First() - refCount + 1)
                                    .Take(refCount).ToList();

                                foreach (var source in callSources)
                                {
                                    if (context[source + 1].Type == InstructionType.Jump)
                                    {
                                        context[source + 1] = context[source + 1].WithLabel(newLabel);
                                    }
                                }

                                for (var k = callSources.Count - 1; k >= 0; k--)
                                {
                                    context.RemoveRange(callSources[k] - refCount + 1, refCount);
                                }

                                var pos = context.GetLabelPosition(label);

                                if (includesFallthrough)
                                {
                                    context.Insert(pos, Instruction.MarkLabel(newLabel));
                                    context.InsertRange(pos + 1, duplicateInstructions);
                                }
                                else
                                {
                                    context.Insert(pos, Instruction.Jump(label));
                                    context.Insert(pos + 1, Instruction.MarkLabel(newLabel));
                                    context.InsertRange(pos + 2, duplicateInstructions);
                                }
                            }
                        }
                    }
                }
            }

            return changed;
        }

        private static int CheckEquality(OptimizationContext context, int refPoint, int refCount, int point)
        {
            for (var offset = 0; offset < refCount && point - offset >= 0; offset++)
            {
                if(context[refPoint - offset] != context[point - offset])
                {
                    return offset;
                }
            }

            return 0;
        }
    }
}
