using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class MergeVariableOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = 0; i < context.Count - 1; i++)
            {
                if (context[i].Matches(InstructionType.StorePosition)
                    && context[i + 1].Matches(InstructionType.StorePosition))
                {
                    var variableA = context[i].Data1;
                    var variableB = context[i + 1].Data1;
                    // Are these two variables always saved at the same time?

                    if (VariablesAreDuplicates(context, variableA, variableB))
                    {
                        // Substitute variableA for variableB everywhere
                        for (var j = context.Count - 1; j >= 0; j--)
                        {
                            var instruction = context[j];
                            switch (instruction.Type)
                            {
                                case InstructionType.StorePosition when instruction.Data1 == variableB:
                                    context[j] = instruction.WithData1(variableA);
                                    break;
                                case InstructionType.RestorePosition:
                                case InstructionType.Capture:
                                    if (instruction.Data1 == variableB)
                                    {
                                        context[j] = instruction.WithData1(variableA);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            return changed;
        }

        private bool VariablesAreDuplicates(OptimizationContext context, ushort variableA, ushort variableB)
        {
            for (var i = 0; i < context.Count; i++)
            {
                var instruction = context[i];
                if (instruction.Matches(InstructionType.StorePosition) && instruction.Data1 == variableA)
                {
                    // Find variableB
                    if (!HasVariableAround(context, i, variableB))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool HasVariableAround(OptimizationContext context, int i, ushort variable)
        {
            var pos = i;
            while (pos > 0 && context[pos].Type == InstructionType.StorePosition)
            {
                if (context[pos].Data1 == variable)
                {
                    return true;
                }

                pos--;
            }

            pos = i;
            while (pos < context.Count && context[pos].Type == InstructionType.StorePosition)
            {
                if (context[pos].Data1 == variable)
                {
                    return true;
                }

                pos++;
            }

            return false;
        }
    }
}
