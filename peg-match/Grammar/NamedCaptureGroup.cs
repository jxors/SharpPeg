using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.Grammar
{
    public class NamedCaptureGroup : CaptureGroup
    {
        public string Name { get; }
        
        public NamedCaptureGroup(string name, int key, Operator child) : base(key, child)
        {
            Name = name;
        }
        
        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new NamedCaptureGroup(Name, Key, Child.Duplicate(mapping));
    }
}
