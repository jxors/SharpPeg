using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.SelfParser
{
    public enum CaptureType
    {
        Any,
        Empty,
        CharacterClass,
        Literal,
        Not,
        Pattern,
        Sequence,
        ZeroOrMore,
        OneOrMore,
        And,
        PrioritizedChoice,
        Definition,
        Identifier,
        CharacterClassRange,
        Optional,
        Character,
    }
}
