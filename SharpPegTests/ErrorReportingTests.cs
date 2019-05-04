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

        [TestMethod]
        public void FailureLabelIsCorrect()
        {
            var runner = PatternCompiler.Default.Compile(new Pattern()
            {
                Data = new Throw(5),
            });

            var result = runner.Run("abc");
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(5, result.FailureLabel);
        }

        [TestMethod]
        public void PrioritizedChoiceCatchesFailureLabels()
        {
            var runner = PatternCompiler.Default.Compile(new Pattern()
            {
                Data = new PrioritizedChoice(new Throw(5), CharacterClass.String("abc"), new[] { 5 }),
            });

            var result = runner.Run("abc");
            Assert.IsTrue(result.IsSuccessful);
            Assert.AreEqual(3, result.InputPosition);
        }

        [TestMethod]
        public void PrioritizedChoiceNotCatchingDefaultFailure()
        {
            var runner = PatternCompiler.Default.Compile(new Pattern()
            {
                Data = new PrioritizedChoice(CharacterClass.String("xyz"), CharacterClass.String("abc"), new[] { 5 }),
            });

            var result = runner.Run("abc");
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(1, result.FailureLabel);
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
