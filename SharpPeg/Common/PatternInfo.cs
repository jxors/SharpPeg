using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpPeg.Operators;

namespace SharpPeg.Common
{
    public class PatternInfo
    {
        public bool IsRecursive { get; private set; } = false;
        public bool ContainsCaptures { get; private set; } = false;
        public int NumNodes { get; private set; } = 0;

        public List<Pattern> PatternCalls { get; private set; } = new List<Pattern>();

        private PatternInfo() { }

        public static Dictionary<Pattern, PatternInfo> Build(Pattern startPattern)
        {
            var processed = new HashSet<Pattern>();
            var output = new Dictionary<Pattern, PatternInfo>();

            void Internal(Pattern currentElement)
            {
                void ProcessPattern(Pattern p)
                {
                    if (processed.Contains(p))
                    {
                        output[p].IsRecursive = true;
                    }
                    else
                    {
                        processed.Add(p);
                        Internal(p);
                        processed.Remove(p);
                    }
                }

                if (!output.TryGetValue(currentElement, out var info))
                {
                    info = output[currentElement] = new PatternInfo();

                    void ProcessOperator(Operator op)
                    {
                        info.NumNodes += 1;

                        if (op is Pattern p)
                        {
                            ProcessPattern(p);
                            info.PatternCalls.Add(p);
                            info.ContainsCaptures |= output[p].ContainsCaptures;
                        }
                        else
                        {
                            if (op is CaptureGroup)
                            {
                                info.ContainsCaptures = true;
                            }

                            foreach (var child in op.Children)
                            {
                                ProcessOperator(child);
                            }
                        }
                    }

                    if (currentElement is PrecompiledPattern pp)
                    {
                        foreach(var child in pp.Children)
                        {
                            ProcessOperator(child);
                        }
                    }
                    else
                    {
                        ProcessOperator(currentElement.Data);
                    }
                }
            }

            Internal(startPattern);

            return output;
        }
    }
}
