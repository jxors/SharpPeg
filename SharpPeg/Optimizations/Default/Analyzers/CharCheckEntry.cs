using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Optimizations.Default.Analyzers
{
    /// <summary>
    /// TODO: Is this a good idea (performance-wise?)
    /// </summary>
    public class CharCheckEntry
    {
        public CharCheckEntry(CircularBuffer<List<Instruction>> matching, CircularBuffer<List<Instruction>> failing)
        {
            this.MatchingCharacters = matching;
            this.FailingCharacters = failing;
        }

        public CircularBuffer<List<Instruction>> MatchingCharacters { get; } = new CircularBuffer<List<Instruction>>();
        public CircularBuffer<List<Instruction>> FailingCharacters { get; } = new CircularBuffer<List<Instruction>>();

        public bool CanChange => MatchingCharacters.Select(list => list.Count).Sum() > 0 || FailingCharacters.Select(list => list.Count).Sum() > 0;

        public static CharCheckEntry Default => new CharCheckEntry(new CircularBuffer<List<Instruction>>(), new CircularBuffer<List<Instruction>>());

        public CharCheckEntry Advance(int offset)
        {
            var matching = MatchingCharacters.Clone();
            var failing = FailingCharacters.Clone();

            for (var i = 0; i < offset; i++)
            {
                if (matching.Count > 0)
                {
                    matching.PopFront();
                }

                if (failing.Count > 0)
                {
                    failing.PopFront();
                }
            }

            return new CharCheckEntry(matching, failing);
        }

        public CharCheckEntry MatchSuccess(OptimizationContext context, Instruction instruction)
        {
            var matching = MatchingCharacters.Clone();
            AddEntry(instruction, true, instruction.Offset, matching);

            matching[instruction.Offset].RemoveAll(existing => InstructionHelper.NonJumpMatchWillFail(context, existing, instruction));

            return new CharCheckEntry(matching, FailingCharacters);
        }

        public CharCheckEntry MatchFail(OptimizationContext context, Instruction instruction)
        {
            var failing = FailingCharacters.Clone();
            AddEntry(instruction, true, instruction.Offset, failing);
            
            return new CharCheckEntry(MatchingCharacters, failing);
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
        
        public CharCheckEntry UnionWith(CharCheckEntry other)
        {
            if(other == null)
            {
                return this;
            }

            var matching = new CircularBuffer<List<Instruction>>();
            var failing = new CircularBuffer<List<Instruction>>();

            // TODO: Check if this is correct!
            for(var i = 0; i < MatchingCharacters.Count && i < other.MatchingCharacters.Count; i++)
            {
                foreach(var item in MatchingCharacters[i])
                {
                    if(other.MatchingCharacters[i].Any(range => range.Data1 == item.Data1 && range.Data2 == item.Data2))
                    {
                        AddEntry(item, false, i, matching);
                    }
                }
            }

            for (var i = 0; i < FailingCharacters.Count && i < other.FailingCharacters.Count; i++)
            {
                foreach (var item in FailingCharacters[i])
                {
                    if (other.FailingCharacters[i].Any(range => range.Data1 == item.Data1 && range.Data2 == item.Data2))
                    {
                        AddEntry(item, false, i, failing);
                    }
                }
            }

            return new CharCheckEntry(matching, failing);
        }

        public override string ToString()
        {
            return "Matching:" 
                + string.Join(", ", MatchingCharacters.Select((item, i) => $"[{i}] = {string.Join("|", item.Select(r => $"{r.Data1}...{r.Data2}"))}"))
                + " Failing: "
                + string.Join(", ", FailingCharacters.Select((item, i) => $"[{i}] = {string.Join("|", item.Select(r => $"{r.Data1}...{r.Data2}"))}"));
        }
    }
}
