using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    public class NdState
    {
        public class ViewA : NdStateView
        {
            private NdState state;

            public ViewA(NdState state)
            {
                this.state = state;
            }
            public override int Position => state.PositionA;

            public override int Advances => state.AdvancesA;

            public override int[] Vars => state.VarAs;

            public override NdState AdvanceAndMoveOneForward(int offset) => state.AdvanceA(offset).MoveA(1);

            public override NdState Move(int offset) => state.MoveA(offset);

            public override NdState SetMaxBounds(int offset) => state.SetMaxBounds(offset);

            public override NdState SetMinBounds(int offset) => state.SetMinBounds(offset);

            public override NdState StoreAndMoveOneForward(ushort var) => state.StoreA(var).MoveA(1);

            public override NdState WithAdvancesAndMoveOneForward(int newValue) => state.WithAdvancesA(newValue).MoveA(1);

            public override NdState WithPosition(int position) => state.WithPositionA(position);
        }

        public class ViewB : NdStateView
        {
            private NdState state;

            public ViewB(NdState state)
            {
                this.state = state;
            }

            public override int Position => state.PositionB;

            public override int Advances => state.AdvancesB;

            public override int[] Vars => state.VarBs;

            public override NdState AdvanceAndMoveOneForward(int offset) => state.AdvanceB(offset).MoveB(1);

            public override NdState Move(int offset) => state.MoveB(offset);

            public override NdState SetMaxBounds(int offset) => state.SetMaxBounds(offset);

            public override NdState SetMinBounds(int offset) => state.SetMinBounds(offset);

            public override NdState StoreAndMoveOneForward(ushort var) => state.StoreB(var).MoveB(1);

            public override NdState WithAdvancesAndMoveOneForward(int newValue) => state.WithAdvancesB(newValue).MoveB(1);

            public override NdState WithPosition(int position) => state.WithPositionB(position);
        }

        public ushort Variable { get; }
        public bool[][] Vug { get; }
        public int PositionA { get; }
        public int PositionB { get; }

        public int AdvancesA { get; }
        public int AdvancesB { get; }
        
        public int MinBounds { get; }
        public int MaxBounds { get; }

        public int[] VarAs { get; }
        public int[] VarBs { get; }

        public CircularBuffer<List<Instruction>> MatchingCharacters { get; } = new CircularBuffer<List<Instruction>>();
        public CircularBuffer<List<Instruction>> FailingCharacters { get; } = new CircularBuffer<List<Instruction>>();

        public NdStateView A => new ViewA(this);

        public NdStateView B => new ViewB(this);

        public NdState(ushort variable, bool[][] variableUsageGraph, int numVariables, int posA, int posB)
        {
            Variable = variable;
            Vug = variableUsageGraph;
            PositionA = posA;
            PositionB = posB;
            VarAs = new int[numVariables];
            VarBs = new int[numVariables];
            MinBounds = -1;
            MaxBounds = int.MaxValue;

            for(var i = 0; i < numVariables; i++)
            {
                VarAs[i] = -1;
                VarBs[i] = -1;
            }
        }

        private NdState(ushort variable, bool[][] variableUsageGraph, int positionA, int positionB, int advancesA, int advancesB, int minBounds, int maxBounds, int[] varAs, int[] varBs, CircularBuffer<List<Instruction>> matching, CircularBuffer<List<Instruction>> failing)
        {
            Variable = variable;
            Vug = variableUsageGraph;
            PositionA = positionA;
            PositionB = positionB;
            MinBounds = minBounds;
            MaxBounds = maxBounds;
            VarAs = varAs;
            VarBs = varBs;
            AdvancesA = advancesA;
            AdvancesB = advancesB;
            MatchingCharacters = matching;
            FailingCharacters = failing;
        }

        public bool PracticallyEquivalent(NdState other, OptimizationContext context, Stack<NdState> stack)
        {
            var diff = AdvancesB - other.AdvancesB;
            if (PositionA == other.PositionA &&
                PositionB == other.PositionB &&
                AdvancesA - other.AdvancesA == AdvancesB - other.AdvancesB &&
                ((MinBounds == -1 && other.MinBounds == -1) || (MinBounds == other.MinBounds + diff)) &&
                ((MaxBounds == int.MaxValue && other.MaxBounds == int.MaxValue) || (MaxBounds == other.MaxBounds + diff)) &&
                MatchingCharacters.Count >= other.MatchingCharacters.Count &&
                FailingCharacters.Count >= other.FailingCharacters.Count)
            {
                for(var i = 0; i < VarAs.Length; i++)
                {
                    if (VarAs[i] != other.VarAs[i] && VarAs[i] != other.VarAs[i] + diff)
                    {
                        return false;
                    }
                }

                for (var i = 0; i < VarBs.Length; i++)
                {
                    if (VarBs[i] != other.VarBs[i] && VarBs[i] != other.VarBs[i] + diff)
                    {
                        return false;
                    }
                }

                var newState = new Lazy<NdState>(() => new NdState(other.Variable, other.Vug, other.PositionA, other.PositionB, other.AdvancesA, other.AdvancesB, other.MinBounds, other.MaxBounds, other.VarAs, other.VarBs, other.MatchingCharacters.Clone(), other.FailingCharacters.Clone()));
                var addAdditionalCondition = false;

                // TODO: Check that this has at least every match / fail that other has too.
                for (var i = 0; i < other.MatchingCharacters.Count; i++)
                {
                    for (var j = 0; j < other.MatchingCharacters[i].Count; j++)
                    {
                        var current = other.MatchingCharacters[i][j];
                        if (!MatchingCharacters[i].Any(item => item.Data1 == current.Data1 && item.Data2 == current.Data2))
                        {
                            if(diff == 0)
                            {
                                return false;
                            }

                            addAdditionalCondition = true;
                            newState.Value.MatchingCharacters[i] = newState.Value.MatchingCharacters[i].Where(item => item != current).ToList();
                        }
                    }
                }

                for (var i = 0; i < other.FailingCharacters.Count; i++)
                {
                    for (var j = 0; j < other.FailingCharacters[i].Count; j++)
                    {
                        var current = other.FailingCharacters[i][j];
                        if (!FailingCharacters[i].Any(item => item.Data1 == current.Data1 && item.Data2 == current.Data2))
                        {
                            if (diff == 0)
                            {
                                return false;
                            }

                            addAdditionalCondition = true;
                            newState.Value.FailingCharacters[i] = newState.Value.FailingCharacters[i].Where(item => item != current).ToList();
                        }
                    }
                }

                if(addAdditionalCondition)
                {
                    stack.Push(newState.Value);
                }

                return true;
            }

            return false;
        }

        public bool AAndBAreSameState()
        {
            // TODO: We probably need to check vars here too!
            return PositionA == PositionB && AdvancesA == AdvancesB;
        }

        public NdState AdvanceA(int offset)
        {
            return WithAdvancesA(AdvancesA + offset);
        }

        public NdState AdvanceB(int offset)
        {
            return WithAdvancesB(AdvancesB + offset);
        }

        public NdState WithPositionA(int position)
        {
            return new NdState(Variable, Vug, position, PositionB, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState WithPositionB(int position)
        {
            return new NdState(Variable, Vug, PositionA, position, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState WithAdvancesA(int newValue)
        {
            if(newValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newValue));
            }

            return new NdState(Variable, Vug, PositionA, PositionB, newValue, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }
        
        public NdState WithAdvancesB(int newValue)
        {
            if (newValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newValue));
            }

            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, newValue, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState MoveA(int offset)
        {
            return new NdState(Variable, Vug, PositionA + offset, PositionB, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState MoveB(int offset)
        {
            return WithPositionB(PositionB + offset);
        }

        public NdState MoveBoth(int offset)
        {
            return new NdState(Variable, Vug, PositionA + offset, PositionB + offset, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState SetMinBounds(int offset)
        {
            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, AdvancesB, Math.Max(MinBounds, offset), MaxBounds, VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState SetMaxBounds(int offset)
        {
            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, AdvancesB, MinBounds, Math.Min(offset, MaxBounds), VarAs, VarBs, MatchingCharacters, FailingCharacters);
        }
        
        public NdState MatchSuccess(OptimizationContext context, Instruction instruction, int offset)
        {
            var matching = MatchingCharacters.Clone();
            AddEntry(instruction, true, offset, matching);

            matching[offset].RemoveAll(existing => InstructionHelper.NonJumpMatchWillFail(context, existing, instruction));

            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, matching, FailingCharacters);
        }

        public bool IsInProperReturnState()
        {
            return AdvancesA == AdvancesB;
        }

        public NdState MatchFail(OptimizationContext context, Instruction instruction, int offset)
        {
            var failing = FailingCharacters.Clone();
            AddEntry(instruction, true, offset, failing);

            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, VarBs, MatchingCharacters, failing);
        }

        private static void AddEntry(Instruction instruction, bool cloneList, int offset, CircularBuffer<List<Instruction>> matching)
        {
            while (matching.Count <= offset)
            {
                matching.PushBack(new List<Instruction>());
            }

            var newList = cloneList ? matching[offset].ToList() : matching[offset];
            newList.Add(instruction);
            matching[offset] = newList;
        }

        public NdState StoreA(ushort var)
        {
            var newVars = VarAs.ToArray();
            newVars[var] = AdvancesA;
            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, AdvancesB, MinBounds, MaxBounds, newVars, VarBs, MatchingCharacters, FailingCharacters);
        }

        public NdState StoreB(ushort var)
        {
            var newVars = VarBs.ToArray();
            newVars[var] = AdvancesB;
            return new NdState(Variable, Vug, PositionA, PositionB, AdvancesA, AdvancesB, MinBounds, MaxBounds, VarAs, newVars, MatchingCharacters, FailingCharacters);
        }

        public EvaluationResult CheckChars(OptimizationContext context, Instruction instruction, int offset)
        {
            return InstructionHelper.CharCheckResult(context, instruction, FailingCharacters[offset], MatchingCharacters[offset]);
        }

        public EvaluationResult CheckBounds(int offset)
        {
            if(offset <= MinBounds)
            {
                return EvaluationResult.Success;
            }else if(offset >= MaxBounds)
            {
                return EvaluationResult.Fail;
            }

            return EvaluationResult.Inconclusive;
        }

        public override string ToString()
        {
            return $"({PositionA}, {AdvancesA}) ({PositionB}, {AdvancesB}); {MinBounds} <= B <= {MaxBounds}; Matching {string.Join(", ", MatchingCharacters.Select((item, i) => $"[{i}] = {string.Join("|", item.Select(r => $"{r.Data1}...{r.Data2}"))}"))}; Failing {string.Join(", ", FailingCharacters.Select((item, i) => $"[{i}] = {string.Join("|", item.Select(r => $"{r.Data1}...{r.Data2}"))}"))};";
        }
    }
}
