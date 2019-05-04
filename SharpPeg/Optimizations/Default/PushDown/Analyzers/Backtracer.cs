using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    public enum EvaluationResult
    {
        Inconclusive,
        Fail,
        Success,
    }

    public class Backtracer
    {
        public OptimizationContext Context { get; }

        private List<BoundsCheckEntry> boundsCheckMemo;
        private List<CharCheckEntry> charCheckMemo;

        public Backtracer(OptimizationContext context)
        {
            Context = context;
            boundsCheckMemo = new List<BoundsCheckEntry>();
            charCheckMemo = new List<CharCheckEntry>();

            while(boundsCheckMemo.Count < context.Instructions.Count)
            {
                boundsCheckMemo.Add(null);
                charCheckMemo.Add(null);
            }
        }

        public void NotifyRemoval(int position)
        {
            boundsCheckMemo.RemoveAt(position);
            charCheckMemo.RemoveAt(position);
        }

        public void NotifyInsert(int position)
        {
            boundsCheckMemo.Insert(position, null);
            charCheckMemo.Insert(position, null);
        }

        public EvaluationResult CheckBounds(int pos, bool firstInstructionWasJump, int offset)
        {
            if (pos < 0)
            {
                return EvaluationResult.Inconclusive;
            }
            var result = GetBoundsAt(pos, firstInstructionWasJump);
            if (offset <= result.MinBounds)
            {
                return EvaluationResult.Success;
            }
            else if (offset >= result.MaxBounds)
            {
                return EvaluationResult.Fail;
            }
            else
            {
                return EvaluationResult.Inconclusive;
            }
        }

        public BoundsCheckEntry GetBoundsAt(int pos, bool firstInstructionWasJump)
        {
            return CheckBoundsInternalHelper(firstInstructionWasJump, pos, new bool[Context.Count]);
        }

        public EvaluationResult CheckChars(int pos, bool firstInstructionWasJump, Instruction oneOfTheseMustSucceed, int offset)
        {
            if (pos <= 0)
            {
                return EvaluationResult.Inconclusive;
            }

            var result = CheckCharsInternalHelper(firstInstructionWasJump, pos, new bool[Context.Count]);
            var failedChars = result.FailingCharacters[offset];
            var matchedChars = result.MatchingCharacters[offset];
            return InstructionHelper.CharCheckResult(Context, oneOfTheseMustSucceed, failedChars, matchedChars);
        }

        private CharCheckEntry CheckCharsInternalHelper(bool isJump, int pos, bool[] hasHitLabel)
        {
            if (pos >= 0)
            {
                var instruction = Context[pos];
                switch (instruction.Type)
                {
                    case InstructionType.Char:
                        var result = CheckCharsInternal(pos, hasHitLabel);
                        if (isJump)
                        {
                            return result.MatchFail(Context, instruction);
                        }
                        else
                        {
                            return result.MatchSuccess(Context, instruction);
                        }
                }
            }

            return CheckCharsInternal(pos, hasHitLabel);
        }

        private CharCheckEntry CheckCharsInternal(int pos, bool[] hasHitLabel)
        {
            if (pos >= 0)
            {
                CharCheckEntry WithoutMemoization()
                {
                    var instruction = Context[pos];
                    switch (instruction.Type)
                    {
                        case InstructionType.MarkLabel:
                            {
                                if (!hasHitLabel[instruction.Label])
                                {
                                    hasHitLabel[instruction.Label] = true;

                                    var result = pos > 0 && Context[pos - 1].IsEnding ? null : CheckCharsInternalHelper(false, pos - 1, hasHitLabel);
                                    for (var i = 0; i < Context.Count; i++)
                                    {
                                        var jumpingInstruction = Context[i];
                                        if ((jumpingInstruction.CanJumpToLabel && jumpingInstruction.Label == instruction.Label)
                                            ||
                                            (jumpingInstruction.Type == InstructionType.Call && Context.FailureLabelMap[jumpingInstruction.Data2].Mapping.Any(item => item.jumpTarget == instruction.Label)))
                                        {
                                            result = CheckCharsInternalHelper(true, i, hasHitLabel).UnionWith(result);
                                            if (!result.CanChange)
                                            {
                                                return result;
                                            }
                                        }
                                    }
                                
                                    hasHitLabel[instruction.Label] = false;

                                    return result ?? CharCheckEntry.Default;
                                }

                                return CharCheckEntry.Default;
                            }
                        case InstructionType.Advance:
                            return CheckCharsInternalHelper(false, pos - 1, hasHitLabel).Advance(instruction.Offset);
                        case InstructionType.Char:
                        case InstructionType.StorePosition:
                        case InstructionType.Capture:
                        case InstructionType.BoundsCheck:
                            return CheckCharsInternalHelper(false, pos - 1, hasHitLabel);
                        case InstructionType.RestorePosition:
                            {
                                var result = (CharCheckEntry)null;
                                foreach (var storePosition in FindStorePositionsInPath(pos, instruction.Data1))
                                {
                                    result = CheckCharsInternalHelper(false, storePosition, hasHitLabel).UnionWith(result);
                                    if (!result.CanChange)
                                    {
                                        return result;
                                    }
                                }

                                return result ?? CharCheckEntry.Default;
                            }
                        case InstructionType.Jump:
                        case InstructionType.Return:
                        case InstructionType.Call:
                        default:
                            return CharCheckEntry.Default;
                    }
                }

                if (charCheckMemo[pos] != null)
                {
                    return charCheckMemo[pos];
                }

                return charCheckMemo[pos] = WithoutMemoization();
            }

            return CharCheckEntry.Default;
        }


        private BoundsCheckEntry CheckBoundsInternalHelper(bool isJump, int pos, bool[] hasHitLabel)
        {
            if (pos >= 0)
            {
                var instruction = Context[pos];
                switch (instruction.Type)
                {
                    case InstructionType.BoundsCheck:
                        var result = CheckBoundsInternal(pos, hasHitLabel);
                        if (isJump)
                        {
                            return result.SetMax(instruction.Offset);
                        }
                        else
                        {
                            return result.SetMin(instruction.Offset);
                        }
                }
            }

            return CheckBoundsInternal(pos, hasHitLabel);
        }

        private BoundsCheckEntry CheckBoundsInternal(int pos, bool[] hasHitLabel)
        {
            if (pos >= 0)
            {
                BoundsCheckEntry WithoutMemoization()
                {
                    var instruction = Context[pos];
                    switch (instruction.Type)
                    {
                        case InstructionType.MarkLabel:
                            {
                                if (!hasHitLabel[instruction.Label])
                                {
                                    hasHitLabel[instruction.Label] = true;

                                    var result = pos > 0 && Context[pos - 1].IsEnding ? null : CheckBoundsInternalHelper(false, pos - 1, hasHitLabel);
                                    for (var i = 0; i < Context.Count; i++)
                                    {
                                        var jumpingInstruction = Context[i];
                                        if ((jumpingInstruction.CanJumpToLabel && jumpingInstruction.Label == instruction.Label)
                                            ||
                                            (jumpingInstruction.Type == InstructionType.Call && Context.FailureLabelMap[jumpingInstruction.Data2].Mapping.Any(item => item.jumpTarget == instruction.Label)))
                                        {
                                            result = CheckBoundsInternalHelper(true, i, hasHitLabel).UnionWith(result);
                                            if (!result.CanChange)
                                            {
                                                return result;
                                            }
                                        }
                                    }

                                    hasHitLabel[instruction.Label] = false;

                                    return result ?? BoundsCheckEntry.Default;
                                }

                                return BoundsCheckEntry.Default;
                            }
                        case InstructionType.Advance:
                            return CheckBoundsInternalHelper(false, pos - 1, hasHitLabel).Advance(instruction.Offset);
                        case InstructionType.Char:
                        case InstructionType.StorePosition:
                        case InstructionType.Capture:
                        case InstructionType.BoundsCheck:
                            return CheckBoundsInternalHelper(false, pos - 1, hasHitLabel);
                        case InstructionType.RestorePosition:
                            {
                                var result = (BoundsCheckEntry)null;
                                foreach (var storePosition in FindStorePositionsInPath(pos, instruction.Data1))
                                {
                                    result = CheckBoundsInternalHelper(false, storePosition, hasHitLabel).UnionWith(result);
                                    if (!result.CanChange)
                                    {
                                        return result;
                                    }
                                }

                                return result ?? BoundsCheckEntry.Default;
                            }
                        case InstructionType.Jump:
                        case InstructionType.Return:
                        case InstructionType.Call:
                        default:
                            return BoundsCheckEntry.Default;
                    }
                }

                if (boundsCheckMemo[pos] != null)
                {
                    return boundsCheckMemo[pos];
                }
                
                return boundsCheckMemo[pos] = WithoutMemoization();
            }

            return BoundsCheckEntry.Default;
        }

        private IEnumerable<int> FindStorePositionsInPath(int startPos, ushort variable)
        {
            var positionStack = new Stack<int>();
            positionStack.Push(startPos);
            var processed = new bool[Context.Count];

            while (positionStack.Count > 0)
            {
                var pos = positionStack.Pop();
                if (pos >= 0 && !processed[pos])
                {
                    processed[pos] = true;

                    var instruction = Context[pos];
                    switch (instruction.Type)
                    {
                        case InstructionType.StorePosition:
                            if (instruction.Data1 == variable)
                            {
                                yield return pos;
                            }
                            break;
                        case InstructionType.MarkLabel:
                            for (var i = 0; i < Context.Count; i++)
                            {
                                if ((Context[i].CanJumpToLabel && Context[i].Label == instruction.Label)
                                    ||
                                    (Context[i].Type == InstructionType.Call && Context.FailureLabelMap[Context[i].Data2].Mapping.Any(item => item.jumpTarget == instruction.Label)))
                                {
                                    positionStack.Push(i - (Context[i].Type == InstructionType.Jump ? 1 : 0));
                                }
                                else if (Context[i].Matches(InstructionType.Call, out var _1, out var _2, out var _3, out var labelMapping))
                                {
                                    foreach(var (_, jumpTarget) in Context.FailureLabelMap[Context[i].Data2].Mapping)
                                    {
                                        if (jumpTarget == instruction.Label)
                                        {
                                            positionStack.Push(i);
                                        }
                                    }
                                }
                            }
                            goto previous;
                        case InstructionType.Advance:
                        case InstructionType.BoundsCheck:
                        case InstructionType.Char:
                        case InstructionType.Capture:
                        case InstructionType.RestorePosition:
                            previous:
                            positionStack.Push(pos - 1);
                            break;
                        case InstructionType.Jump:
                        case InstructionType.Return:
                        case InstructionType.Call:
                            break;
                        default: throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
