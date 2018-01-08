using SharpPeg.Operators;
using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.Grammar
{
    public class NamespacedPattern : UnresolvedPatternReference
    {
        public Pattern Child { get; }

        public Pattern FinalPattern => Child is NamespacedPattern p ? p.FinalPattern : Child;

        public string PatternName => Child is NamespacedPattern p ? p.PatternName : Child.Name;

        public string FileName => Child is NamespacedPattern p ? p.FileName : Name;

        public string NamespacePath => Child is NamespacedPattern p ? $"{Name}::{p.NamespacePath}" : Name;

        public NamespacedPattern(string name, Pattern child) : base(name)
        {
            Child = child;
        }

        public override string ToString()
        {
            return $"{Name}::{Child}";
        }
    }
}
