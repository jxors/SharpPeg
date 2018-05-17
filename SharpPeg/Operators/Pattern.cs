using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Operators
{
    public class Pattern : Operator
    {
        public virtual Operator Data { get; set; }

        public string Name { get; }

        public override IEnumerable<Operator> Children => Enumerable.Empty<Operator>();

        public Pattern(string name = null)
        {
            Name = name;
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new Pattern(Name) { Data = Data?.Duplicate(mapping) };

        public override string ToString()
        {
            return $"{Name ?? "[NonTerminal]"}";
        }
    }
}
