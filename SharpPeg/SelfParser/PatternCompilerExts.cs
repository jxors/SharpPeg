using SharpPeg.Operators;
using SharpPeg.Runner;
using System.Linq;

namespace SharpPeg.SelfParser
{
    public static class PatternCompilerExts
    {
        private static PegGrammar grammar = new PegGrammar();

        public static IRunner CompileExpression(this PatternCompiler patternCompiler, string expression)
        {
            return patternCompiler.Compile(new Pattern("Expression")
            {
                Data = grammar.ParseExpression(expression)
            });
        }

        public static IRunner CompileGrammar(this PatternCompiler patternCompiler, string grammarStr)
        {
            return patternCompiler.Compile(grammar.ParseGrammar(grammarStr).Last());
        }
    }
}
