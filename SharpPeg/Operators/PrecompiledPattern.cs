using SharpPeg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public class PrecompiledPattern : Pattern
    {
        public Method Method { get; }

        public IReadOnlyList<Pattern> PatternReferences { get; }

        public override IEnumerable<Operator> Children => PatternReferences;

        public PrecompiledPattern(Method method, IEnumerable<Pattern> patternReferences) : base(method.Name)
        {
            Method = method;
            PatternReferences = patternReferences.ToList();
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new Pattern(Name) { Data = Data?.Duplicate(mapping) };
    }
}
