using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;

namespace SharpPeg.Optimizations.Default
{
    public class JumpOptimization : OptimizationBase
    {
        public bool FullPathSearch { get; }

        public JumpOptimization(bool fullPathSearch)
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
                    // TODO: Can we optimize a Call-instruction here?
                    case InstructionType.Char:
                    case InstructionType.BoundsCheck:
                    case InstructionType.Jump:
                        {
                            var backtracer = new BacktracerView(context.Backtracer, i, true);
                            if (!instruction.IsCharOrBoundsCheck)
                            {
                                backtracer = new BacktracerView(context.Backtracer, i - 1, false);
                            }

                            var result = InstructionHelper.FindJumpTargetEx(context, backtracer, context.GetLabelPosition(instruction.Label), FullPathSearch, false);
                            if (!context[result.Position - 1].Matches(InstructionType.MarkLabel, instruction.Label))
                            {
                                if (context[result.Position - 1].Matches(InstructionType.MarkLabel, out var newTarget))
                                {
                                    context.NonDestructiveUpdate(i, instruction.WithLabel(newTarget));
                                }
                                else
                                {
                                    var newLabel = context.LabelAllocator++;
                                    context.NonDestructiveUpdate(i, instruction.WithLabel(newLabel));
                                    context.Insert(result.Position, Instruction.MarkLabel(newLabel), true);
                                }

                                changed = true;
                            }
                        }
                        break;
                }
            }

            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instructionCount = context.Count;
                var instruction = context[i];
                if (instruction.Matches(InstructionType.Jump, out var targetLabel))
                {
                    var jumpingTo = context[context.GetLabelPosition(targetLabel) + 1];
                    if (jumpingTo.Matches(InstructionType.Return))
                    {
                        context[i] = jumpingTo;
                    }
                }
            }

            return changed;
        }
    }
}
