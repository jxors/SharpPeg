using System.Collections.Generic;
using SharpPeg.Operators;
using System.Linq;
using System.Collections;
using SharpPeg.Common;
using SharpPeg.Optimizations.Default.Analyzers;
using System;

namespace SharpPeg.Compilation
{
    public class CompilerContext : IEnumerable<Instruction>
    {
        public ushort LabelAllocator { get; set; } = 0;
        public ushort VariableAllocator { get; set; } = 0;
        public int InsideLoop { get; set; } = 0;

        public IReadOnlyList<Instruction> Instructions => instructions;

        public int Count => instructions.Count;

        public short DelayedAdvance { get; set; } = 0;

        public List<CharRange> CharacterRanges { get; } = new List<CharRange>();

        private List<Instruction> instructions = new List<Instruction>();

        private int boundsCheckPosition = -1;

        private ushort boundsCheckFailLabel = 0;
        public IReadOnlyDictionary<Pattern, PatternInfo> PatternInfo { get; }
        public IReadOnlyDictionary<Pattern, int> PatternIndices { get;}
        public List<LabelMap> FailureLabelMap { get; } = new List<LabelMap>();

        public CompilerContext(Dictionary<Pattern, PatternInfo> patternInfo, Dictionary<Pattern, int> patternIndices)
        {
            PatternInfo = patternInfo;
            PatternIndices = patternIndices;
        }

        public void Add(Instruction instruction)
        {
            instructions.Add(instruction);
        }

        public void UpdateOrSetBoundsCheck(ushort failLabel, short offset)
        {
            if (boundsCheckPosition < 0 || boundsCheckFailLabel != failLabel)
            {
                boundsCheckPosition = Instructions.Count;
                Add(Instruction.BoundsCheck(failLabel, offset));
                boundsCheckFailLabel = failLabel;
            }
            else if(Instructions[boundsCheckPosition].Offset < offset)
            {
                instructions[boundsCheckPosition] = Instruction.BoundsCheck(failLabel, offset);
            }
        }

        public void Flush()
        {
            boundsCheckPosition = -1;
            if (DelayedAdvance > 0)
            {
                instructions.Add(Instruction.Advance(DelayedAdvance));
                DelayedAdvance = 0;
            }
        }

        public int GetCharRange(CharacterRange range)
        {
            for (var i = 0; i < CharacterRanges.Count; i++)
            {
                var exisingRange = CharacterRanges[i];
                if (exisingRange.Min == range.Min && exisingRange.Max == range.Max)
                {
                    return i;
                }
            }

            var result = CharacterRanges.Count;
            CharacterRanges.Add(new CharRange(range.Min, range.Max));

            return result;
        }

        public int GetCharRange(IReadOnlyList<CharacterRange> ranges)
        {
            for (var i = 0; i <= CharacterRanges.Count - ranges.Count; i++)
            {
                if(RangeMatchesRange(i, ranges))
                {
                    return i;
                }
            }

            var result = CharacterRanges.Count;
            CharacterRanges.AddRange(ranges.Select(range => new CharRange(range.Min, range.Max)));

            return result;
        }

        private bool RangeMatchesRange(int offset, IReadOnlyList<CharacterRange> ranges)
        {
            for (var j = 0; j < ranges.Count; j++)
            {
                var existingRange = CharacterRanges[offset + j];
                var newRange = ranges[j];
                if (existingRange.Min != newRange.Min || existingRange.Max != newRange.Max)
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerator<Instruction> GetEnumerator()
        {
            return ((IEnumerable<Instruction>)instructions).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Instruction>)instructions).GetEnumerator();
        }

        public ushort GetFailLabelMap(Dictionary<int, ushort> failLabelMap)
        {
            var result = FailureLabelMap.Count;
            FailureLabelMap.Add(new LabelMap(failLabelMap.Select(kvp => (kvp.Key, kvp.Value))));

            return (ushort)result;
        }
    }
}
