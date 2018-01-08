using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Operators;
using SharpPeg.Runner;
using SharpPeg.SelfParser;

namespace SharpPegTests
{
    [TestClass]
    public class ErrorReportingTests
    {
        private PegGrammar grammar = new PegGrammar();

        [TestMethod]
        public void ErrorReportingDoesNotCrash()
        {
            const string InputData = "A <- 'a' | 'b'";

            var runner = BuildRunner();
            var result = runner.Run(InputData);
            Assert.IsFalse(result.IsSuccessful);
            runner.ExplainResult(result, InputData);
        }

        [TestMethod]
        public void ErrorReportingDoesNotCrash2()
        {
            const string InputData = "A <- 'a' / 'b' B <- [a-";

            var runner = BuildRunner();
            var result = runner.Run(InputData);
            Assert.IsFalse(result.IsSuccessful);
            runner.ExplainResult(result, InputData);
        }

        private IRunner BuildRunner()
        {
            grammar.EnsureGrammarBuilt();
            return PatternCompiler.Default.Compile(new Pattern()
            {
                Data = grammar.Grammar
            });
        }
    }
}
