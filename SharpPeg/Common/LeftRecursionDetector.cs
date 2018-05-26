using SharpPeg.Compilation;
using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg.Common
{
    public class LeftRecursionDetector
    {
        public static void Check(Pattern startPattern)
        {
            var processed = new HashSet<Pattern>();
            var output = new Dictionary<Pattern, PatternInfo>();

            void Internal(Pattern currentElement)
            {
                void ProcessPattern(Pattern p)
                {
                    if (!processed.Contains(p))
                    {
                        if (currentElement == p)
                        {
                            throw new CompilationException("PEG is directly left-recursive in pattern: " + p);
                        }
                        else
                        {
                            throw new CompilationException("PEG is indirectly left-recursive in pattern: " + p);
                        }
                    } else { 
                        processed.Add(p);
                        Internal(p);
                        processed.Remove(p);
                    }
                }

                void ProcessOperator(Operator op)
                {
                    switch(op)
                    {
                        case PrecompiledPattern p:
                            break;
                        case Pattern p:
                            ProcessPattern(p);
                            break;
                        default:
                            ProcessOperator(op.Children.First());
                            break;
                    }
                }

                ProcessOperator(currentElement.Data);
            }

            Internal(startPattern);
        }
    }
}
