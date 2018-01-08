using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnneededChecksOptimization : OptimizationBase
    {
        public bool FullPathSearch { get; }

        public RemoveUnneededChecksOptimization(bool fullPathSearch)
        {
            FullPathSearch = fullPathSearch;
        }

        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            
            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instructionCount = context.Count;
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.Char:
                    case InstructionType.BoundsCheck:
                        var backtracerView = new BacktracerView(context.Backtracer, i - 1, false);

                        switch (instruction.Type == InstructionType.Char ? backtracerView.CheckChars(instruction, instruction.Offset) : backtracerView.CheckBounds(instruction.Offset))
                        {
                            case EvaluationResult.Fail:
                                context.NonDestructiveUpdate(i, Instruction.Jump(instruction.Label));
                                changed = true;
                                break;
                            case EvaluationResult.Success:
                                context.RemoveAt(i, true);
                                changed = true;
                                break;
                            case EvaluationResult.Inconclusive:
                                var targetPosition = -1;
                                if (context[i + 1].Matches(InstructionType.MarkLabel, out var markedLabel))
                                {
                                    targetPosition = i + 2;
                                }
                                else if (context[i + 1].Matches(InstructionType.Jump, out var jumpTarget))
                                {
                                    targetPosition = context.GetLabelPosition(jumpTarget) + 1;
                                }

                                if (targetPosition >= 0)
                                {
                                    var normalPosition = InstructionHelper.FindJumpTarget(context, new BacktracerView(context.Backtracer, i - 1, false), context.GetLabelPosition(instruction.Label), FullPathSearch);
                                    if (normalPosition == targetPosition)
                                    {
                                        // Label will be replaced in next step.
                                        context.NonDestructiveUpdate(i, Instruction.Jump(instruction.Label));
                                        changed = true;
                                    }
                                }
                                break;
                            default: throw new NotImplementedException();
                        }
                        break;
                }

                if (i + 1 < context.Count)
                {
                    var backtracerView = new BacktracerView(context.Backtracer, i, false);

                    var result = InstructionHelper.FindJumpTargetEx(context, backtracerView, i + 1, FullPathSearch, false);
                    if (result.Position != InstructionHelper.TracePath(context, null, i + 1))
                    {
                        if (context[result.Position - 1].Matches(InstructionType.MarkLabel, out var newTarget))
                        {
                            context.Insert(i + 1, Instruction.Jump(newTarget));
                        }
                        else
                        {
                            var newLabel = context.LabelAllocator++;
                            context.Insert(i + 1, Instruction.Jump(newLabel));
                            context.Insert(result.Position + (result.Position >= i + 1 ? 1 : 0), Instruction.MarkLabel(newLabel), true);
                        }

                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}
