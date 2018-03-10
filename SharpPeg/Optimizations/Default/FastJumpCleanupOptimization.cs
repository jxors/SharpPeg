using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class FastJumpCleanupOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instructionCount = context.Count;
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                    case InstructionType.Call:
                    case InstructionType.Jump:
                        if (context[context.GetLabelPosition(instruction.Label) + 1].Matches(InstructionType.Jump, out var newTarget))
                        {
                            context.NonDestructiveUpdate(i, instruction.WithLabel(newTarget));
                        } else if (i + 1 < context.Count && instruction.Matches(InstructionType.Jump, out var jumpTarget) && context[i + 1].Matches(InstructionType.MarkLabel, jumpTarget))
                        {
                            context.RemoveAt(i, true);
                        }

                        break;
                }
            }

            return changed;
        }
    }
}
