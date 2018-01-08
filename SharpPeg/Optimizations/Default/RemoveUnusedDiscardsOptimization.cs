using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnusedDiscardsOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.DiscardCaptures:
                        if(!DiscardIsUseful(context, i))
                        {
                            context.RemoveAt(i);
                            changed = true;
                        }
                        break;
                    case InstructionType.RestorePosition:
                        if(i + 1 < context.Count && context[i + 1].Matches(InstructionType.DiscardCaptures))
                        {
                            if (!AnyCapturesSinceLastStore(context, i - 1, instruction.Data1))
                            {
                                context.RemoveAt(i + 1);
                            }
                        }
                        break;
                }
            }

            return changed;
        }

        private bool AnyCapturesSinceLastStore(OptimizationContext context, int pos, ushort variable)
        {
            var positionStack = new Stack<int>();
            var graph = new bool[context.Count];
            positionStack.Push(pos);

            while (positionStack.Count > 0)
            {
                var currentPos = positionStack.Pop();
                var instruction = context[currentPos];
                if (!graph[currentPos])
                {
                    graph[currentPos] = true;
                    switch (instruction.Type)
                    {
                        case InstructionType.Call:
                        case InstructionType.Capture:
                            return true;
                        case InstructionType.MarkLabel:
                            for (var i = 0; i < context.Count; i++)
                            {
                                switch (context[i].Type)
                                {
                                    case InstructionType.BoundsCheck:
                                    case InstructionType.Char:
                                    case InstructionType.Jump:
                                    case InstructionType.Call:
                                        if (context[i].Label == instruction.Label)
                                        {
                                            positionStack.Push(i);
                                        }
                                        break;
                                }
                            }
                            goto default;
                        case InstructionType.StorePosition when instruction.Data1 == variable:
                            break;
                        default:
                            if (currentPos > 0 && context[currentPos - 1].Type != InstructionType.Jump && context[currentPos - 1].Type != InstructionType.Return)
                            {
                                positionStack.Push(currentPos - 1);
                            }
                            break;
                    }
                }
            }

            return false;
        }

        private bool DiscardIsUseful(OptimizationContext context, int pos)
        {
            var positionStack = new Stack<int>();
            var found = false;
            var graph = new bool[context.Count];
            positionStack.Push(pos);

            while (positionStack.Count > 0)
            {
                var currentPos = positionStack.Pop();
                var instruction = context[currentPos];
                if (!graph[currentPos])
                {
                    graph[currentPos] = true;
                    switch (instruction.Type)
                    {
                        case InstructionType.Capture:
                            break;
                        case InstructionType.MarkLabel:
                            for (var i = 0; i < context.Count; i++)
                            {
                                switch (context[i].Type)
                                {
                                    case InstructionType.BoundsCheck:
                                    case InstructionType.Char:
                                    case InstructionType.Jump:
                                    case InstructionType.Call:
                                        if (context[i].Label == instruction.Label)
                                        {
                                            positionStack.Push(i);
                                        }
                                        break;
                                }
                            }
                            goto default;
                        case InstructionType.RestorePosition:
                        case InstructionType.Advance when instruction.Offset < 0:
                            found = true;
                            goto default;
                        default:
                            if (currentPos > 0 && context[currentPos - 1].Type != InstructionType.Jump && context[currentPos - 1].Type != InstructionType.Return)
                            {
                                positionStack.Push(currentPos - 1);
                            }
                            break;
                    }
                }
            }

            return found;
        }
    }
}
