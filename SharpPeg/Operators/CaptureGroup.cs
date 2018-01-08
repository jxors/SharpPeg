using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPeg.Operators
{
    public class CaptureGroup : SingleChildOperator
    {
        public int Key { get; }

        public CaptureGroup(int key, Operator child) : base(child)
        {
            this.Key = key;
        }
        
        public override string ToString()
        {
            return $"{{{Key}:{Child}}}";
        }

        protected override Operator DuplicateInternal(Dictionary<Operator, Operator> mapping) => new CaptureGroup(Key, Child.Duplicate(mapping));
    }
}
