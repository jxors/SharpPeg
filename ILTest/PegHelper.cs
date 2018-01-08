using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpPeg.Operators;
using SharpPeg.Runner;
using SharpPeg.SelfParser;
using SharpPeg;

namespace ILTest
{
    class PegHelper : PegGrammar
    {
        public PegHelper(PatternCompiler patternCompiler) : base(patternCompiler)
        {
        }

        public Pattern GetExpressionPattern()
        {
            return Expression;
        }
    }
}
