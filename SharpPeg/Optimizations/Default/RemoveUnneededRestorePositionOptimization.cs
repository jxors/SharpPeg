using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnneededRestorePositionOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            var positionNotModifiedSinceStoring = new List<int>();

            for (var i = 0; i < context.Count; i++)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.StorePosition:
                        positionNotModifiedSinceStoring.Add((int)context[i].Offset);
                        break;
                    case InstructionType.BoundsCheck:
                    case InstructionType.Char:
                        var jumpTarget = context.GetLabelPosition(context[i].Label);
                        if (jumpTarget > i)
                        {
                            var targetInstruction = context[jumpTarget + 1];
                            if ((targetInstruction.Type == InstructionType.RestorePosition)
                                && positionNotModifiedSinceStoring.Contains(targetInstruction.Data1))
                            {
                                var offset = 2;
                                var newLabelId = (ushort)0;
                                if (context[jumpTarget + offset].Type == InstructionType.MarkLabel)
                                {
                                    newLabelId = context[jumpTarget + offset].Label;
                                }
                                else
                                {
                                    newLabelId = context.LabelAllocator++;
                                    context.Insert(jumpTarget + offset, new Instruction(InstructionType.MarkLabel, newLabelId, 0, 0, 0));
                                }

                                context[i] = new Instruction(context[i].Type, newLabelId, context[i].Offset, context[i].Data1, context[i].Data2);
                                changed = true;
                            }
                        }
                        break;
                    default:
                        positionNotModifiedSinceStoring.Clear();
                        break;
                }
            }

            //TODO: the preconditions are narrow. Need a more generic matching approach!
            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instruction = context[i];
                if (instruction.Matches(InstructionType.RestorePosition, out var _, out var offset, out var variable, out var _)
                    && context[i + 1].Matches(InstructionType.BoundsCheck, out var boundsCheckFailLabel, out var boundsCheckOffset)
                    && context[i + 2].Matches(InstructionType.Advance, out var advanceOffset)
                    && context[i + 3].Matches(InstructionType.Jump, out var jumpTarget)
                    && context[context.GetLabelPosition(jumpTarget) + 1].Matches(InstructionType.StorePosition, out var _, 0, variable)
                    && context.GetLabelPosition(jumpTarget) < i)
                {
                    var nd = new NdAnalyzer(context, i + 2, context.GetLabelPosition(jumpTarget) + 1, variable);
                    if (nd.Result)
                    {
                        context.RemoveAt(i);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private bool WillHaveAdvancedAtSince(OptimizationContext context, int position, int sincePosition, bool[] processed)
        {
            if(processed[position])
            {
                return true;
            }

            if(position == sincePosition)
            {
                return false;
            }

            var instruction = context[position];
            switch (instruction.Type)
            {
                case InstructionType.BoundsCheck:
                case InstructionType.Char:
                case InstructionType.Jump:
                case InstructionType.Call:
                case InstructionType.Capture:
                case InstructionType.StorePosition:
                    return WillHaveAdvancedAtSince(context, position - 1, sincePosition, processed);
                case InstructionType.MarkLabel:
                    {
                        for (var i = 0; i < context.Count; i++)
                        {
                            switch (context[i].Type)
                            {
                                case InstructionType.BoundsCheck:
                                case InstructionType.Char:
                                case InstructionType.Jump:
                                case InstructionType.Call:
                                    foreach (var (_, jumpTarget) in context.FailureLabelMap[context[i].Data2].Mapping)
                                    {
                                        if (jumpTarget == instruction.Label)
                                        {
                                            if (!WillHaveAdvancedAtSince(context, i, sincePosition, processed))
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }

                        if(!context[position - 1].IsEnding)
                        {
                            if(!WillHaveAdvancedAtSince(context, position - 1, sincePosition, processed))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                case InstructionType.Advance:
                    return true;
                case InstructionType.Return:
                case InstructionType.RestorePosition:
                    return false;
                default: throw new NotImplementedException();
            }
        }
    }
}
