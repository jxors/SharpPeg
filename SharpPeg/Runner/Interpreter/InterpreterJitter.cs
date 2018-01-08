using System;
using System.Collections.Generic;
using System.Text;
using SharpPeg.Operators;
using System.Linq;
using SharpPeg.Common;

namespace SharpPeg.Runner.Interpreter
{
    public class InterpreterJitter : IJitter
    {
        public IRunner Compile(CompiledPeg compiledPeg)
        {
            var labelPositions = compiledPeg.Methods.Select(item => CalculateLabelPositions(item)).ToArray();
            return new InterpreterRunner(compiledPeg.StartPatternIndex, compiledPeg.Methods, labelPositions);
        }

        private int[] CalculateLabelPositions(Method item)
        {
            var instructions = item.Instructions;
            var labelPositions = new int[item.LabelCount];

            for (var i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Matches(InstructionType.MarkLabel, out var label))
                {
                    labelPositions[label] = i + 1;
                }
            }

            return labelPositions;
        }
    }
}
