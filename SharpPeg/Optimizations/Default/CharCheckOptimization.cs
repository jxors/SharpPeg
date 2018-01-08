using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class CharCheckOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = 0; i < context.Count - 1; i++)
            {
                if (context[i].Matches(InstructionType.Char, out var label)
                    && context[i + 1].Matches(InstructionType.Char, label)
                    && GetNumChars(context[i + 1], context) < GetNumChars(context[i], context))

                {
                    changed = true;
                    var temp = context[i];
                    context[i] = context[i + 1];
                    context[i + 1] = temp;
                }
            }

            return changed;
        }

        private static int GetNumChars(Instruction instruction, OptimizationContext context)
        {
            var numChars = 0;

            for (var i = instruction.Data1; i < instruction.Data2; i++)
            {
                var range = context.CharacterRanges[i];
                numChars += range.Max - range.Min + 1;
            }

            return numChars;
        }
    }
}
