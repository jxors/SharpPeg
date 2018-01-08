using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.SelfParser
{
    public class UnresolvedPatternReference : Pattern
    {
        public bool IsResolved => base.Data != null;

        public UnresolvedPatternReference(string name = null) : base(name)
        { }
    }
}
