using SharpPeg.Common;
using System.Collections.Generic;
using System.Linq;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    public class InstructionHelper
    {
        public static IEnumerable<Instruction> DuplicateLabels(OptimizationContext context, List<Instruction> instructions, ushort? ignoreLabel = null)
        {
            var labelMap = new ushort?[context.LabelAllocator];
            if(ignoreLabel != null)
            {
                labelMap[ignoreLabel.Value] = ignoreLabel.Value;
            }

            return DuplicateLabels(context, instructions, labelMap).ToList();
        }

        public static IEnumerable<Instruction> DuplicateLabels(OptimizationContext context, List<Instruction> instructions, ushort?[] labelMap)
        {
            for (var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                if (instruction.HasLabel)
                {
                    var newLabel = labelMap[instruction.Label];
                    if (newLabel == null)
                    {
                        newLabel = context.LabelAllocator++;
                        labelMap[instruction.Label] = newLabel;
                    }

                    yield return instruction.WithLabel(newLabel.Value);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        public static IEnumerable<int> GetLabelReferences(List<Instruction> instructions, ushort label)
        {
            for(var i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                if(instruction.Label == label && instruction.CanJumpToLabel)
                {
                    yield return i;
                }
            }
        }

        public static int FindJumpTarget(OptimizationContext context, BacktracerView backtracer, int position, bool doRecursion) => FindJumpTargetEx(context, backtracer, position, doRecursion, false).Position;

        public static ForwardTraceResult FindJumpTargetEx(OptimizationContext context, BacktracerView backtracer, int position, bool doRecursion, bool allowStores)
        {
            var memo = new Dictionary<int, ForwardTraceResult>();
            return FindLastPossiblePositionInternal(context, memo, backtracer, position, 0, doRecursion, allowStores);
        }

        private static ForwardTraceResult FindLastPossiblePositionInternal(OptimizationContext context, Dictionary<int, ForwardTraceResult> memo, BacktracerView backtracer, int startPosition, int depth, bool doRecursion, bool allowStores)
        {
            if (memo.TryGetValue(startPosition, out var result))
            {
                return result;
            }

            if (depth > 10) return new ForwardTraceResult(startPosition);

            var traceResult = TracePath(context, doRecursion ? backtracer : null, startPosition);
            var instruction = context[traceResult];
            if (instruction.Matches(InstructionType.RestorePosition))
            {
                // TODO: Backtracer will return incorrect result, is there any way to make it return correct things to improve
                // the tracing here?
                var newStartPos = traceResult + 1;
                if(context[traceResult + 1].Matches(InstructionType.DiscardCaptures))
                {
                    newStartPos++;
                }

                var traceResult2 = TracePath(context, null, traceResult + 1);
                if (context[traceResult2].Matches(InstructionType.RestorePosition, out var _, 0))
                {
                    return memo[startPosition] = new ForwardTraceResult(traceResult2, null);
                }
            }
            else if (doRecursion)
            {
                if (instruction.Type == InstructionType.BoundsCheck || instruction.Type == InstructionType.Char)
                {
                    var normal = FindLastPossiblePositionInternal(context, memo, backtracer, traceResult + 1, depth + 1, doRecursion, allowStores);
                    if (normal == result)
                    {
                        return memo[startPosition] = normal;
                    }

                    var taken = FindLastPossiblePositionInternal(context, memo, backtracer, context.GetLabelPosition(instruction.Label), depth + 1, doRecursion, allowStores);
                    if (taken == normal)
                    {
                        return memo[startPosition] = taken;
                    }
                }
            }
            
            return memo[startPosition] = new ForwardTraceResult(traceResult); ;
        }
        
        public static int TracePath(OptimizationContext context, BacktracerView backtracer, int position)
        {
            while (true)
            {
                var instruction = context[position];
                switch (instruction.Type)
                {
                    case InstructionType.Jump:
                        position = context.GetLabelPosition(instruction.Label);
                        break;
                    case InstructionType.MarkLabel:
                        position++;
                        break;
                    case InstructionType.Char:
                    case InstructionType.BoundsCheck:
                        if(backtracer == null)
                        {
                            return position;
                        }

                        var (newPosition, result) = GetJumpResult(context, backtracer, position, instruction.Label);
                        if (result == EvaluationResult.Fail)
                        {
                            position = context.GetLabelPosition(instruction.Label);
                            break;
                        }
                        else if (result == EvaluationResult.Success)
                        {
                            position = newPosition + 1;
                            break;
                        }

                        return position;
                    default: return position;
                }
            }
        }

        private static (int position, EvaluationResult result) GetJumpResult(OptimizationContext context, BacktracerView backtracer, int startPosition, ushort label)
        {
            var position = startPosition;
            for (; context[position].IsCharOrBoundsCheck && context[position].Label == label; position++)
            {
                var instruction = context[position];
                var result = EvaluationResult.Inconclusive;
                if (instruction.Type == InstructionType.Char)
                {
                    result = backtracer.CheckChars(instruction, instruction.Offset);
                }
                else
                {
                    result = backtracer.CheckBounds(instruction.Offset);
                }

                if (result == EvaluationResult.Success && position == startPosition)
                {
                    return (position, result);
                }else if(result == EvaluationResult.Fail)
                {
                    return (position, result);
                }
            }

            return (startPosition, EvaluationResult.Inconclusive);
        }

        public static EvaluationResult CharCheckResult(OptimizationContext context, Instruction oneOfTheseMustSucceed, List<Instruction> failedChars, List<Instruction> matchedChars)
        {
            if ((failedChars?.Count ?? 0) > 0 && failedChars.Any(item => InstructionHelper.JumpMatchWillFail(context, oneOfTheseMustSucceed, item)))
            {
                return EvaluationResult.Fail;
            }
            else if ((matchedChars?.Count ?? 0) > 0 && matchedChars.All(item => InstructionHelper.NonJumpMatchWillFail(context, oneOfTheseMustSucceed, item)))
            {
                return EvaluationResult.Fail;
            }
            else if ((matchedChars?.Count ?? 0) > 0 && matchedChars.Any(item => InstructionHelper.NonJumpMatchWillSucceed(context, oneOfTheseMustSucceed, item)))
            {
                return EvaluationResult.Success;
            }
            else
            {
                return EvaluationResult.Inconclusive;
            }
        }

        public static bool JumpMatchWillFail(OptimizationContext context, Instruction oneOfTheseMustSucceed, Instruction theseFailedToMatch)
        {
            // Each of the items that must succeed is part of all of the ranges that failed to match
            foreach (var range in oneOfTheseMustSucceed.GetCharacterRanges(context.CharacterRanges))
            {
                if(!theseFailedToMatch.GetCharacterRanges(context.CharacterRanges).Any(innerRange => range.Min >= innerRange.Min && range.Max <= innerRange.Max))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool NonJumpMatchWillFail(OptimizationContext context, Instruction oneOfTheseMustSucceed, Instruction theseMatched)
        {
            // Each of the items that must succeed is not part of any of the ranges that matched
            foreach (var range in oneOfTheseMustSucceed.GetCharacterRanges(context.CharacterRanges))
            { 
                // If we know a matched range partially overlaps, the match might succeed
                if(theseMatched.GetCharacterRanges(context.CharacterRanges).Any(innerRange => range.Min <= innerRange.Max && innerRange.Min <= range.Max))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool NonJumpMatchWillSucceed(OptimizationContext context, Instruction oneOfTheseMustSucceed, Instruction theseMatched)
        {
            // At least one of the items that must succeed is one of ranges that matched
            foreach (var range in oneOfTheseMustSucceed.GetCharacterRanges(context.CharacterRanges))
            {
                foreach (var innerRange in theseMatched.GetCharacterRanges(context.CharacterRanges))
                {
                    if (innerRange.Min >= range.Min && innerRange.Max <= range.Max)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
