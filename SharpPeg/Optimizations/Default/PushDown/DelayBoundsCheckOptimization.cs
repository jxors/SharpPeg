using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System.Collections.Generic;

namespace SharpPeg.Optimizations.Default
{
    /// <summary>
    /// Delays a bounds check if the bound has already been checked for some lower value.
    /// 
    /// For example (| = bounds check):
    /// Transform '|Tom' / '|Finn' into '|Tom' / 'Fin|n' (if the first bounds check is successful of course.
    /// 
    /// Might not be possible if we need duplicate code for this...
    /// </summary>
    public class DelayBoundsCheckOptimization : PushDown.PushDownOptimization
    {
        public DelayBoundsCheckOptimization() : base(InstructionType.BoundsCheck)
        { }

        protected override bool InnerOptimize(OptimizationContext context, Instruction matchedInstruction, Backtracer backtracer, int i)
        {
            var changed = false;
            var j = i;
            var patchedLabels = new Dictionary<(ushort label, short offset), ushort>();
            context.RemoveAt(j);

            while (!IsChainBreaker(context[j], matchedInstruction, new BacktracerView(backtracer, j - 1, false)))
            {
                var instruction = context[j];
                switch (instruction.Type)
                {
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                        var targetLabel = instruction.Label;
                        if (patchedLabels.TryGetValue((targetLabel, matchedInstruction.Offset), out var newTarget))
                        {
                            targetLabel = newTarget;
                        }
                        else
                        {
                            var oldLabel = targetLabel;
                            targetLabel = AddStub(context, targetLabel, matchedInstruction, ref j);
                            patchedLabels[(oldLabel, matchedInstruction.Offset)] = targetLabel;
                        }

                        backtracer = new Backtracer(context);
                        changed = true;
                        context[j] = new Instruction(instruction.Type, targetLabel, instruction.Offset, instruction.Data1, instruction.Data2);
                        j++;
                        break;
                }
            }

            context.Insert(j, matchedInstruction);

            return changed || i != j;
        }

        protected override bool IsChainBreaker(Instruction instruction, Instruction source, BacktracerView btv)
        {
            switch (instruction.Type)
            {
                case InstructionType.BoundsCheck:
                case InstructionType.Char:
                    return instruction.Offset > source.Offset || btv.CheckBounds(instruction.Offset) != EvaluationResult.Success;
            }

            return true;
        }
    }
}
