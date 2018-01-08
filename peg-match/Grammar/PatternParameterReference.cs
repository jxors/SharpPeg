using SharpPeg.Operators;
using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.Grammar
{
    public class PatternParameterReference : UnresolvedPatternReference
    {
        public PatternParameterReference(string name) : base(name)
        { }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new PatternParameterReference(Name);
    }
}
