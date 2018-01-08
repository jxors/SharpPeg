using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.Grammar
{
    public class ExportablePattern : Pattern
    {
        public bool IsPublic { get; internal set; }

        public ExportablePattern(string name, bool isPublic = false) : base(name)
        {
            IsPublic = isPublic;
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new ExportablePattern(Name, IsPublic) { Data = Data?.Duplicate(mapping) };
    }
}
