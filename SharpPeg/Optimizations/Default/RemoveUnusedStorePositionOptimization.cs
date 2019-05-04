using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations.Default
{
    public class RemoveUnusedStorePositionOptimization : OptimizationBase
    {
        public override bool Optimize(OptimizationContext context)
        {
            var changed = false;
            var graphs = new bool[context.VariableAllocator][];
            for (var i = 0; i < graphs.Length; i++)
            {
                graphs[i] = new bool[context.Count];
            }

            for (var i = 0; i < context.Count; i++)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.RestorePosition:
                    case InstructionType.Capture:
                        CreateUsageGraph(graphs[instruction.Data1], instruction.Data1, context, i);
                        break;
                }
            }

            for (var i = context.Count - 1; i >= 0; i--)
            {
                var instruction = context[i];
                switch (instruction.Type)
                {
                    case InstructionType.StorePosition:
                        if (!graphs[instruction.Data1][i])
                        {
                            context.RemoveAt(i);
                        }
                        break;
                }
            }

            return changed;
        }

        // Use this for definition in thesis
        //private bool StoreIsUseful(CompilerContext context, ushort variable, int pos)
        //{
        //    var visited = new bool[context.Count];
        //    var positionStack = new Stack<int>();
        //    positionStack.Push(pos);

        //    while (positionStack.Count > 0)
        //    {
        //        var currentPos = positionStack.Pop();

        //        if (currentPos < context.Count)
        //        {
        //            var instruction = context[currentPos];
        //            if (!visited[currentPos])
        //            {
        //                visited[currentPos] = true;
        //                switch (instruction.Type)
        //                {
        //                    case InstructionType.StorePosition when instruction.Data1 == variable:
        //                        break;
        //                    case InstructionType.BoundsCheck:
        //                    case InstructionType.Char:
        //                    case InstructionType.Call:
        //                        positionStack.Push(context.GetLabelPosition(instruction.Label));
        //                        positionStack.Push(currentPos + 1);
        //                        break;
        //                    case InstructionType.Jump:
        //                        positionStack.Push(context.GetLabelPosition(instruction.Label));
        //                        break;
        //                    case InstructionType.RestorePosition when instruction.Data1 == variable:
        //                    case InstructionType.Capture when instruction.Data1 == variable:
        //                        return true;
        //                    default:
        //                        positionStack.Push(currentPos + 1);
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        private bool[] CreateUsageGraph(bool[] graph, ushort variable, OptimizationContext context, int pos)
        {
            var positionStack = new Stack<int>();
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
                        case InstructionType.StorePosition when instruction.Data1 == variable:
                            break;
                        case InstructionType.MarkLabel:
                            for (var i = 0; i < context.Count; i++)
                            {
                                switch (context[i].Type)
                                {
                                    case InstructionType.BoundsCheck:
                                    case InstructionType.Char:
                                    case InstructionType.Jump:
                                        if (context[i].Label == instruction.Label)
                                        {
                                            positionStack.Push(i);
                                        }
                                        break;
                                    case InstructionType.Call:
                                        foreach(var (_, jumpTarget) in context.FailureLabelMap[context[i].Data2].Mapping)
                                        {
                                            if (jumpTarget == instruction.Label)
                                            {
                                                positionStack.Push(i);
                                            }
                                        }
                                        break;
                                }
                            }
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

            return graph;
        }
    }
}
