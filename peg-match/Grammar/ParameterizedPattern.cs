using SharpPeg.Operators;
using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PegMatch.Grammar
{
    public class ParameterizedPattern : ExportablePattern
    {
        public IList<Pattern> Parameters { get; }
        
        public ParameterizedPattern(string name, IEnumerable<Pattern> parameters, bool isPublic = false) : base(name)
        {
            Parameters = parameters.ToList();
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => 
            new ParameterizedPattern(Name, Parameters.Select(item => (Pattern)item.Duplicate(mapping)), IsPublic)
            {
                Data = Data?.Duplicate(mapping)
            };
        
        public override string ToString()
        {
            return $"{Name}<{string.Join(", ", Parameters)}>";
        }
    }
}
