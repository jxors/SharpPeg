using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    // TODO: Unused!
    public class NdAnalyzer
    {
        private bool[] validPositionsForB;

        public ushort Variable { get; }
        public bool Result { get; }

        // TODO: Not used anywhere
        public NdAnalyzer(OptimizationContext context, int posA, int posB, ushort variable)
        {
            this.Variable = variable;
            var graphs = CreateVariableUsageGraphs(context);
            var initialState = new NdState(Variable, graphs, context.VariableAllocator, posA, posB);

            validPositionsForB = CalculatePositionsWhereBMightHaveBeen(context, posA, variable);

            var bounds = context.Backtracer.GetBoundsAt(posB, false);
            initialState = initialState
                .SetMinBounds(bounds.MinBounds)
                .SetMaxBounds(bounds.MaxBounds);

            Result = Analyze(context, initialState);
        }
        
        private bool[] CalculatePositionsWhereBMightHaveBeen(OptimizationContext context, int pos, ushort variable)
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
                        case InstructionType.Capture:
                            return null;
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

            return graph;
        }

        // Copied from RestorePositionOptimization
        private bool[][] CreateVariableUsageGraphs(OptimizationContext context)
        {
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

            return graphs;
        }
        
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
                                    case InstructionType.Call:
                                        if (context[i].Label == instruction.Label)
                                        {
                                            positionStack.Push(i);
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

        private bool Analyze(OptimizationContext context, NdState initialState)
        {
            var processed = new List<NdState>[context.Count];
            for(var i = 0; i < processed.Length; i++)
            {
                processed[i] = new List<NdState>();
            }

            var overflowProtection = 0;

            var stack = new Stack<NdState>();
            stack.Push(initialState);
            while(stack.Count > 0)
            {
                if(stack.Count > context.Count)
                {
                    // Bailout if stack starts to explode
                    return false;
                }else if (overflowProtection++ >= context.Count * 250)
                {
                    // Or if we're just taking too long
                    return false;
                }
                
                var current = stack.Pop();
                if (current.AAndBAreSameState() || !validPositionsForB[current.PositionB] || processed[current.PositionA].Any(item => current.PracticallyEquivalent(item, context, stack)))
                {
                    // Already processed this state.
                    continue;
                }

                processed[current.PositionA].Add(current);
                
                NdStateView view;
                if (current.AdvancesB < current.AdvancesA || context[current.PositionA].Matches(InstructionType.Return))
                {
                    view = current.B;
                }
                else
                {
                    view = current.A;
                    
                }

                if(!Step(context, stack, view, current))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Step(OptimizationContext context, Stack<NdState> stack, NdStateView view, NdState state)
        {
            var instruction = context[view.Position];
            switch (instruction.Type)
            {
                case InstructionType.BoundsCheck:
                    switch (state.CheckBounds(instruction.Offset + view.Advances))
                    {
                        case EvaluationResult.Success:
                            stack.Push(view.Move(1));
                            break;
                        case EvaluationResult.Fail:
                            stack.Push(view.WithPosition(context.GetLabelPosition(instruction.Label)));
                            break;
                        case EvaluationResult.Inconclusive:
                            stack.Push(view.Move(1).SetMinBounds(instruction.Offset + view.Advances));
                            stack.Push(view.WithPosition(context.GetLabelPosition(instruction.Label)).SetMaxBounds(instruction.Offset + view.Advances));
                            break;
                    }
                    break;
                case InstructionType.Char:
                    switch (state.CheckChars(context, instruction, instruction.Offset + view.Advances))
                    {
                        case EvaluationResult.Success:
                            stack.Push(view.Move(1));
                            break;
                        case EvaluationResult.Fail:
                            stack.Push(view.WithPosition(context.GetLabelPosition(instruction.Label)));
                            break;
                        case EvaluationResult.Inconclusive:
                            stack.Push(view.Move(1).MatchSuccess(context, instruction, instruction.Offset + view.Advances));
                            stack.Push(view.WithPosition(context.GetLabelPosition(instruction.Label)).MatchFail(context, instruction, instruction.Offset + view.Advances));
                            break;
                    }
                    break;
                case InstructionType.Jump:
                    stack.Push(view.WithPosition(context.GetLabelPosition(instruction.Label)));
                    break;
                case InstructionType.Call:
                    return false;
                case InstructionType.Advance:
                    if (instruction.Offset < 0)
                    {
                        return false;
                    }

                    stack.Push(view.AdvanceAndMoveOneForward(instruction.Offset));
                    break;
                case InstructionType.StorePosition:
                    stack.Push(view.StoreAndMoveOneForward(instruction.Data1));
                    break;
                case InstructionType.RestorePosition:
                    if (view.Vars[instruction.Data1] == -1)
                    {
                        return false;
                    }

                    stack.Push(view.WithAdvancesAndMoveOneForward(view.Vars[instruction.Data1]));
                    break;
                case InstructionType.MarkLabel:
                case InstructionType.DiscardCaptures:
                    stack.Push(view.Move(1));
                    break;
                case InstructionType.Capture:
                    if (instruction.Data2 != Variable)
                    {
                        return false;
                    }
                    break;
                case InstructionType.Return:
                    stack.Push(state);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }
    }
}
