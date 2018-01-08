using SharpPeg.Common;
using SharpPeg.Operators;
using SharpPeg.Optimizations.Default.Analyzers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Optimizations
{
    public class OptimizationContext : IEnumerable<Instruction>
    {
        public ushort LabelAllocator { get; set; } = 0;
        public ushort VariableAllocator { get; set; } = 0;

        public IReadOnlyList<Instruction> Instructions => instructions;

        public int Count => instructions.Count;

        public short DelayedAdvance { get; set; } = 0;

        public IReadOnlyList<CharRange> CharacterRanges => Method.CharacterRanges;

        private List<Instruction> instructions = new List<Instruction>();

        private int[] labelCache = null;

        private Backtracer backtracer = null;

        public Backtracer Backtracer
        {
            get
            {
                if (backtracer == null)
                {
                    backtracer = new Backtracer(this);
                }

                return backtracer;
            }
        }

        public Method Method { get; }

        public void Flush()
        {
            if (DelayedAdvance > 0)
            {
                instructions.Add(Instruction.Advance(DelayedAdvance));
                DelayedAdvance = 0;
            }
        }

        public Instruction this[int index]
        {
            get
            {
                return instructions[index];
            }
            set
            {
                backtracer = null;
                labelCache = null;
                instructions[index] = value;
            }
        }

        public void NonDestructiveUpdate(int index, Instruction newInstruction)
        {
            labelCache = null;
            instructions[index] = newInstruction;
        }

        public void Add(Instruction instruction)
        {
            backtracer = null;
            labelCache = null;
            instructions.Add(instruction);
        }

        public void RemoveAt(int position, bool nonDestructive = false)
        {
            if (!nonDestructive)
            {
                backtracer = null;
            }
            else if (backtracer != null)
            {
                backtracer.NotifyRemoval(position);
            }

            labelCache = null;
            instructions.RemoveAt(position);
        }

        public void RemoveRange(int start, int count)
        {
            backtracer = null;
            labelCache = null;
            instructions.RemoveRange(start, count);
        }

        public void Insert(int position, Instruction instruction, bool nonDestructive = false)
        {
            if (!nonDestructive)
            {
                backtracer = null;
            }
            else if (backtracer != null)
            {
                backtracer.NotifyInsert(position);
            }

            labelCache = null;
            instructions.Insert(position, instruction);
        }

        public void InsertRange(int position, IEnumerable<Instruction> newInstructions)
        {
            backtracer = null;
            labelCache = null;
            instructions.InsertRange(position, newInstructions);
        }

        public int GetLabelPosition(ushort label)
        {
            if (labelCache == null)
            {
                labelCache = new int[LabelAllocator];
            }

            if (labelCache[label] != 0)
            {
                return labelCache[label] - 1;
            }

            for (var i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Matches(InstructionType.MarkLabel, label))
                {
                    labelCache[label] = i + 1;
                    return i;
                }
            }

            labelCache[label] = -1;
            return -1;
        }

        public OptimizationContext(Method method)
        {
            Method = method;
            instructions = new List<Instruction>(method.Instructions);
            LabelAllocator = (ushort)method.LabelCount;
            VariableAllocator = (ushort)method.VariableCount;
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
    }
}
