using System;
using System.Collections.Generic;
using System.Text;

namespace RegexToPeg
{
    enum CaptureType
    {
        Union,
        NegativeSet,
        PositiveSet,
        Char,
        Eos,
        Any,
        Group,
        Plus,
        Star,
        Concatenation,
        MetaCharacter,
        Range,
        Repeat,
        RepeatRange,
    }
}
