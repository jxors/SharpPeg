using SharpPeg.Common;
using SharpPeg.Compilation;
using System;
using System.Collections.Generic;

namespace SharpPeg.Common
{
    public struct Instruction
    {
        public InstructionType Type { get; }

        public ushort Label { get; }

        public short Offset { get; }

        public ushort Data1 { get; }

        public ushort Data2 { get; }

        public Instruction(InstructionType type, ushort label, short offset, ushort data1, ushort data2)
        {
            Type = type;
            Label = label;
            Offset = offset;
            Data1 = data1;
            Data2 = data2;
        }

        public static Instruction BoundsCheck(ushort failLabel, short offset) =>                new Instruction(InstructionType.BoundsCheck, failLabel, offset, 0, 0);
        public static Instruction Char(ushort failLabel, short offset, ushort index, ushort length) =>   new Instruction(InstructionType.Char, failLabel, offset, index, length);
        public static Instruction Advance(short offset) =>                                      new Instruction(InstructionType.Advance, 0, offset, 0, 0);
        public static Instruction Call(ushort failLabelMap, ushort patternIndex) =>             new Instruction(InstructionType.Call, 0, 0, patternIndex, failLabelMap);
        public static Instruction Jump(ushort label) =>                                         new Instruction(InstructionType.Jump, label, 0, 0, 0);
        public static Instruction MarkLabel(ushort label) =>                                    new Instruction(InstructionType.MarkLabel, label, 0, 0, 0);
        public static Instruction StorePosition(ushort variable) =>                             new Instruction(InstructionType.StorePosition, 0, 0, variable, 0);
        public static Instruction RestorePosition(short offset, ushort variable) =>             new Instruction(InstructionType.RestorePosition, 0, offset, variable, 0);
        public static Instruction Return(ushort value) =>                                       new Instruction(InstructionType.Return, 0, 0, value, 0);
        public static Instruction Capture(ushort variable, ushort key) =>                       new Instruction(InstructionType.Capture, 0, 0, variable, key);

        public bool HasLabel => Type <= InstructionType.MarkLabel;

        public bool IsCharOrBoundsCheck => Type <= InstructionType.Char;

        public bool CanJumpToLabel => Type <= InstructionType.Jump; // Call also jumps, but has a mapping instead of a single target
        
        public bool IsEnding
        {
            get
            {
                switch (Type)
                {
                    case InstructionType.Return:
                    case InstructionType.Jump:
                        return true;
                }

                return false;
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case InstructionType.BoundsCheck:
                    return $"  {InstructionType.BoundsCheck} L{Label} +{Offset}";
                case InstructionType.Char:
                    return $"  {InstructionType.Char} L{Label} +{Offset} Chars[{Data1}...{Data2}]";
                case InstructionType.Advance:
                    return $"  {InstructionType.Advance} +{Offset}";
                case InstructionType.Call:
                    return $"  {InstructionType.Call} P{Data1} M[{Data2}]";
                case InstructionType.Return:
                    return $"  {InstructionType.Return} {Data1}";
                case InstructionType.Jump:
                    return $"  {InstructionType.Jump} L{Label}";
                case InstructionType.StorePosition:
                    return $"  {InstructionType.StorePosition} V{Data1}";
                case InstructionType.RestorePosition:
                    return $"  {InstructionType.RestorePosition} +{Offset} V{Data1}";
                case InstructionType.MarkLabel:
                    return $"L{Label}:";
                case InstructionType.Capture:
                    return $"  {InstructionType.Capture} V{Data1} K{Data2}";
            }

            throw new NotImplementedException();
        }

        public IEnumerable<CharRange> GetCharacterRanges(IReadOnlyList<CharRange> ranges)
        {
            for(var i = Data1; i < Data2; i++)
            {
                yield return ranges[i];
            }
        }

        public bool Matches(InstructionType type)
        {
            return Type == type;
        }

        public bool Matches(InstructionType type, ushort label)
        {
            return Type == type && Label == label;
        }

        public bool Matches(InstructionType type, out ushort label)
        {
            label = Label;
            return Type == type;
        }

        public bool Matches(InstructionType type, out ushort label, out short offset)
        {
            label = Label;
            offset = Offset;
            return Type == type;
        }

        public bool Matches(InstructionType type, out ushort label, short offset)
        {
            label = Label;
            return Type == type && offset == Offset;
        }

        public bool Matches(InstructionType type, out ushort label, short offset, ushort data1)
        {
            label = Label;
            data1 = Data1;
            return Type == type && offset == Offset && Data1 == data1;
        }

        public bool Matches(InstructionType type, out ushort label, short offset, out ushort data1, out ushort data2)
        {
            label = Label;
            data1 = Data1;
            data2 = Data2;
            return Type == type && offset == Offset;
        }

        public bool Matches(InstructionType type, out ushort label, out short offset, out ushort data1, out ushort data2)
        {
            label = Label;
            offset = Offset;
            data1 = Data1;
            data2 = Data2;
            return Type == type;
        }

        public Instruction WithLabel(ushort newLabel)
        {
            return new Instruction(Type, newLabel, Offset, Data1, Data2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Instruction))
            {
                return false;
            }

            var instruction = (Instruction)obj;
            return Type == instruction.Type &&
                   Label == instruction.Label &&
                   Offset == instruction.Offset &&
                   Data1 == instruction.Data1 &&
                   Data2 == instruction.Data2;
        }

        public override int GetHashCode()
        {
            var hashCode = 1603895772;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + Label.GetHashCode();
            hashCode = hashCode * -1521134295 + Offset.GetHashCode();
            hashCode = hashCode * -1521134295 + Data1.GetHashCode();
            hashCode = hashCode * -1521134295 + Data2.GetHashCode();
            return hashCode;
        }

        internal Instruction WithData1(ushort newData1)
        {
            return new Instruction(Type, Label, Offset, newData1, Data2);
        }

        internal Instruction WithData2(ushort newData2)
        {
            return new Instruction(Type, Label, Offset, Data1, newData2);
        }

        public static bool operator==(Instruction a, Instruction b)
        {
            return a.Type == b.Type 
                && a.Label == b.Label 
                && a.Offset == b.Offset
                && a.Data1 == b.Data1
                && a.Data2 == b.Data2;
        }

        public static bool operator !=(Instruction a, Instruction b)
        {
            return !(a == b);
        }
    }
}
