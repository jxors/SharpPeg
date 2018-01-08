using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class FastPathOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;

            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instructionCount = context.Count;
                var instruction = context[i];
                if(instruction.Matches(InstructionType.Jump, out var targetLabel) && context.GetLabelPosition(targetLabel) < i)
                {
                    var startPosition = context.GetLabelPosition(targetLabel);
                    var endPosition = i;

                    if(context[i + 1].Matches(InstructionType.MarkLabel))
                    {
                        endPosition++;
                    }

                    var labelMap = new ushort?[context.LabelAllocator];
                    var maxBoundsCheck = -1;

                    // By default, keep al labels the same
                    for(var j = 0; j < labelMap.Length; j++)
                    {
                        labelMap[j] = (ushort)j;
                    }

                    for(var j = startPosition; j <= endPosition; j++)
                    {
                        if(context[j].Matches(InstructionType.BoundsCheck, out var _, out var offset))
                        {
                            if(offset > maxBoundsCheck)
                            {
                                maxBoundsCheck = offset;
                            }
                        }else if(context[j].Matches(InstructionType.MarkLabel, out var label))
                        {
                            // ... except for labels that are marked inside the loop...
                            labelMap[label] = null;
                        }
                    }

                    {
                        if (maxBoundsCheck >= 3 && (!context[startPosition + 1].Matches(InstructionType.BoundsCheck, out var _, out var offset) || offset < maxBoundsCheck))
                        {
                            var canMakeFastPath = true;
                            for(var j = 0; j < context.Count; j++)
                            {
                                if(context[j].CanJumpToLabel)
                                {
                                    if (context[j].Matches(InstructionType.BoundsCheck, out var boundsCheckTargetLabel) && boundsCheckTargetLabel == targetLabel)
                                    {
                                        canMakeFastPath = false;
                                    }else if(j >= startPosition && j <= endPosition && context.GetLabelPosition(context[j].Label) < startPosition || context.GetLabelPosition(context[j].Label) > endPosition)
                                    {
                                        canMakeFastPath = false;
                                    }
                                }
                            }

                            if (canMakeFastPath && context.Backtracer.CheckBounds(startPosition, false, maxBoundsCheck) != Analyzers.EvaluationResult.Fail)
                            {
                                // Create fast path
                                var newInstructions = InstructionHelper.DuplicateLabels(context, context.Skip(startPosition).Take(endPosition - startPosition + 1).ToList(), labelMap).ToList();
                                var goToSlowPathLabel = context.LabelAllocator++;
                                newInstructions.Insert(1, Instruction.BoundsCheck(goToSlowPathLabel, (short)maxBoundsCheck));
                                newInstructions.Add(Instruction.MarkLabel(goToSlowPathLabel));
                                
                                context.InsertRange(startPosition, newInstructions);
                                changed = true;
                            }
                        }
                    }
                }
            }

            return changed;
        }
    }
}
