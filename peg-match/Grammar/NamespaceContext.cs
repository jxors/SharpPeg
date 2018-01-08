using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatch.Grammar
{
    public class NamespaceContext
    {
        private Dictionary<(string, int), Pattern> patternMap = new Dictionary<(string, int), Pattern>();

        public NamespaceContext()
        { }

        public void Add(Pattern pattern)
        {
            if (pattern is ParameterizedPattern pp)
            {
                patternMap[(pp.Name, pp.Parameters.Count)] = pp;
            }
            else
            {
                patternMap[(pattern.Name, 0)] = pattern;
            }
        }

        public Pattern GetPattern(Pattern pattern, bool allowInternal = false)
        {
            var key = (pattern.Name, pattern is ParameterizedPattern pp ? pp.Parameters.Count : 0);
            if (!patternMap.TryGetValue(key, out var output))
            {
                throw new KeyNotFoundException($"Pattern {pattern} could not be found");
            }

            if(!allowInternal && (!(output is ExportablePattern ep) || !ep.IsPublic))
            {
                throw new KeyNotFoundException($"Pattern {pattern} is not exported");
            }

            return output;
        }
    }
}
