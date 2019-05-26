using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
                                ushort newLabelId;
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
                if (instruction.Matches(InstructionType.RestorePosition, out _, out _, out var v, out _))
                {
                     //RemoveRestorePositionIfPossible(context, i, v);
                }

                {
                    if (instruction.Matches(InstructionType.RestorePosition, out _, out _, out var variable, out _)
                        && context[i + 1].Matches(InstructionType.BoundsCheck)
                        && context[i + 2].Matches(InstructionType.Advance)
                        && context[i + 3].Matches(InstructionType.Jump, out var jumpTarget)
                        && context[context.GetLabelPosition(jumpTarget) + 1].Matches(InstructionType.StorePosition, out _, 0, variable)
                        && context.GetLabelPosition(jumpTarget) < i)
                    {
                        var nd = new NdAnalyzer(context, i + 2, i + 3, variable);
                        if (nd.Result)
                        {
                            context.RemoveAt(i);
                            changed = true;
                        }
                    }
                }
            }

            return changed;
        }

        private void RemoveRestorePositionIfPossible(OptimizationContext context, int i, ushort v)
        {
            // TODO: Also check without skipping the first advance
            var prefixInstructions = FindPrefixInstructions(context, i + 1, v, true);
            if (prefixInstructions == null)
            {
                return;
            }

            var splitPos = prefixInstructions.FindIndex(item => item.instr.Type == InstructionType.Advance);
            if (splitPos < 0)
            {
                return;
            }

            var advanceInstr = prefixInstructions[splitPos];

            var first = prefixInstructions.Take(splitPos).ToList();
            var last = prefixInstructions.Skip(splitPos).ToList();
            if (!prefixInstructions.Any(item => item.instr.Type == InstructionType.BoundsCheck))
            {
                return;
            }

            // TODO: Do we need to ensure that the jump is part of a loop rather than a linear program?
            if(!first.All(item => item.instr.Type == InstructionType.BoundsCheck || item.instr.Type == InstructionType.MarkLabel || item.instr.Type == InstructionType.Jump))
            {
                return;
            }

            var (instr, pos) = prefixInstructions.Last();
            if(!context.Instructions[pos + 1].Matches(InstructionType.Advance, out var _, out var offset) || offset != advanceInstr.instr.Offset)
            {
                return;
            }

            var nd = new NdAnalyzer(context, advanceInstr.pos, advanceInstr.pos + 1, v);
            if (nd.Result)
            {
                Console.WriteLine("Can remove RestorePosition if Advance is also removed!");
            }
        }

        private List<(Instruction instr, int pos)> FindPrefixInstructions(OptimizationContext context, int pos, ushort variable, bool skipAdvance)
        {
            var backtracer = new BacktracerView(context.Backtracer, pos - 2, true);
            var prefixInstructions = new List<(Instruction instr, int pos)>();
            var advanceOffset = 0;
            for (var i = 0; i < 32; i++)
            {
                var instr = context.Instructions[pos];
                prefixInstructions.Add((instr, pos));
                switch (instr.Type)
                {
                    case InstructionType.Jump:
                        pos = context.GetLabelPosition(instr.Label);
                        break;
                    case InstructionType.StorePosition when instr.Data1 == variable:
                        return prefixInstructions;
                    case InstructionType.Advance:
                        if (skipAdvance)
                        {
                            skipAdvance = false;
                        }
                        else
                        {
                            advanceOffset += instr.Offset;
                        }

                        pos++;
                        break;
                    case InstructionType.MarkLabel:
                        pos++;
                        break;
                    case InstructionType.Char:
                        switch (backtracer.CheckChars(instr, instr.Offset + advanceOffset))
                        {
                            case EvaluationResult.Success:
                                pos++;
                                break;
                            case EvaluationResult.Fail:
                                pos = context.GetLabelPosition(instr.Label);
                                break;
                            default: return null;
                        }
                        break;
                    case InstructionType.BoundsCheck:
                        switch(backtracer.CheckBounds(instr.Offset + advanceOffset))
                        {
                            case EvaluationResult.Success:
                                pos++;
                                break;
                            case EvaluationResult.Fail:
                                pos = context.GetLabelPosition(instr.Label);
                                break;
                            default: return null;
                        }
                        break;
                    default:
                        return null;
                }
            }

            return null;
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
