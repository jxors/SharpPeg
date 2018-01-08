using SharpPeg.Compilation;
using SharpPeg.Runner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Common
{
    public class Method
    {
        public IReadOnlyList<Instruction> Instructions { get; }
        public IReadOnlyList<CharRange> CharacterRanges { get; }

        public int VariableCount { get; }
        public int LabelCount { get; }

        public string Name { get; }

        public int NumPlaceholderPatterns { get; } = 0;

        public Method(string name, IReadOnlyList<Instruction> instructions, IReadOnlyList<CharRange> characterRanges, int variableCount, int labelCount)
        {
            Instructions = instructions;
            CharacterRanges = characterRanges;
            VariableCount = variableCount;
            LabelCount = labelCount;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
