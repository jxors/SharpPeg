using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class QuickCleanupOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            var labelsUsed = new int[context.LabelAllocator];
            var labelsMarked = new bool[context.LabelAllocator];
            var labelMappings = new int[context.LabelAllocator];
            var variablesUsed = new int[context.VariableAllocator];

            // Count label usage
            for (var i = 0; i < context.Count; i++)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.Call:
                        foreach(var (_, targetLabel) in context.FailureLabelMap[instruction.Data2].Mapping)
                        {
                            labelsUsed[targetLabel]++;
                        }
                        break;
                    case InstructionType.Char:
                    case InstructionType.BoundsCheck:
                    case InstructionType.Jump:
                        labelsUsed[instruction.Label]++;
                        break;
                    case InstructionType.RestorePosition:
                    case InstructionType.Capture:
                        variablesUsed[instruction.Data1]++;
                        break;
                    case InstructionType.MarkLabel:
                        for (; i < context.Count && context[i].Type == InstructionType.MarkLabel; i++)
                        {
                            labelMappings[context[i].Label] = instruction.Label;
                            if (labelsMarked[context[i].Label])
                            {
                                throw new ArgumentException($"Duplicate MarkLabel for label {context[i].Label}");
                            }
                            else
                            {
                                labelsMarked[context[i].Label] = true;
                            }
                        }
                        i--;
                        break;
                }

                // Remove dead code
                if (instruction.IsEnding)
                {
                    changed |= RemoveDeadCodeAfter(context, labelsUsed, i, instruction);
                }
            }

            // Remove unused labels & variables & restores
            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instruction = context[i];
                if (context[i].Type == InstructionType.MarkLabel && labelsUsed[context[i].Label] <= 0)
                {
                    context.RemoveAt(i, true);
                    changed = true;

                    if (i > 0 && context[i - 1].IsEnding)
                    {
                        changed |= RemoveDeadCodeAfter(context, labelsUsed, i - 1, instruction, true);
                        i = context.Count;
                    }
                }
                else if (context[i].Type == InstructionType.StorePosition && variablesUsed[context[i].Data1] == 0)
                {
                    context.RemoveAt(i, true);
                    changed = true;
                }else if(i + 1 < context.Count && context[i].Matches(InstructionType.StorePosition, out var _, out var _, out var variable, out var _)
                    && context[i + 1].Matches(InstructionType.RestorePosition) && context[i + 1].Data1 == variable)
                {
                    context.RemoveAt(i + 1, true);
                }
            }

            // Remap consecutive labels
            for (var i = 0; i < context.Count; i++)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.MarkLabel:
                        for (; i < context.Count && context[i].Type == InstructionType.MarkLabel; i++)
                        {
                            labelMappings[context[i].Label] = instruction.Label;
                        }
                        i--;
                        break;
                }
            }

            // Remap label-jump patterns
            for (var i = context.Count - 2; i >= 0; i--)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.MarkLabel:
                        if (context[i + 1].Matches(InstructionType.Jump, out var secondTarget))
                        {
                            labelMappings[instruction.Label] = labelMappings[secondTarget];
                            context.RemoveAt(i);
                        }
                        break;
                }
            }

            // Re-index labels & variables
            changed |= ReindexLabels(context, labelsUsed, labelMappings);
            changed |= ReindexVariables(context, variablesUsed);

            return changed;
        }

        private static bool ReindexLabels(OptimizationContext context, int[] labelsUsed, int[] labelMappings)
        {
            var reindexedLabels = new ushort[context.LabelAllocator];
            var numUsedLabels = (ushort)0;
            for (var i = 0; i < labelsUsed.Length; i++)
            {
                if (labelsUsed[i] > 0 && labelMappings[i] == i)
                {
                    reindexedLabels[i] = numUsedLabels++;
                }
            }

            if (context.LabelAllocator != numUsedLabels)
            {
                for (var i = context.Count - 1; i >= 0; i--)
                {
                    var instruction = context[i];
                    switch (instruction.Type)
                    {
                        case InstructionType.Char:
                        case InstructionType.BoundsCheck:
                        case InstructionType.Jump:
                        case InstructionType.MarkLabel:
                            var newLabel = reindexedLabels[labelMappings[instruction.Label]];
                            if (i + 1 < context.Count && context[i + 1].Matches(InstructionType.MarkLabel, newLabel))
                            {
                                context.RemoveAt(i);
                            }
                            else
                            {
                                context[i] = context[i].WithLabel(newLabel);
                            }
                            break;
                    }
                }

                context.FailureLabelMap = context.FailureLabelMap
                    .Select(map => new LabelMap(map
                        .Mapping
                        .Select(kvp => (kvp.failureLabel, reindexedLabels[labelMappings[kvp.jumpTarget]])))
                    )
                    .ToList();
                context.LabelAllocator = numUsedLabels;
                context.ClearCache();
                return true;
            }

            return false;
        }

        private static bool ReindexVariables(OptimizationContext context, int[] variablesUsed)
        {
            var reindexedVariables = new ushort[context.VariableAllocator];
            var numUsedVariables = (ushort)0;
            for (var i = 0; i < variablesUsed.Length; i++)
            {
                if (variablesUsed[i] > 0)
                {
                    reindexedVariables[i] = numUsedVariables++;
                }
            }

            if (context.VariableAllocator != numUsedVariables)
            {
                for (var i = context.Count - 1; i >= 0; i--)
                {
                    var instruction = context[i];
                    switch (instruction.Type)
                    {
                        case InstructionType.RestorePosition:
                        case InstructionType.StorePosition:
                        case InstructionType.Capture:
                            var newVariables = reindexedVariables[instruction.Data1];
                            context[i] = context[i].WithData1(newVariables);
                            break;
                    }
                }

                context.VariableAllocator = numUsedVariables;

                return true;
            }

            return false;
        }

        private static bool RemoveDeadCodeAfter(OptimizationContext context, int[] labelsUsed, int i, Instruction instruction, bool subtractValues = false)
        {
            var changed = false;
            while (i + 1 < context.Count && !context[i + 1].Matches(InstructionType.MarkLabel))
            {
                if (subtractValues)
                {
                    switch (context[i + 1].Type)
                    {
                        case InstructionType.Call:
                            foreach (var (_, targetLabel) in context.FailureLabelMap[instruction.Data2].Mapping)
                            {
                                labelsUsed[targetLabel]--;
                            }
                            break;
                        case InstructionType.Char:
                        case InstructionType.BoundsCheck:
                        case InstructionType.Jump:
                            labelsUsed[instruction.Label]--;
                            break;
                    }
                }

                context.RemoveAt(i + 1, true);
                changed = true;
            }

            return changed;
        }
    }
}
