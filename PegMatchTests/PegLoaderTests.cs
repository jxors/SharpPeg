using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegMatch.Grammar;
using SharpPeg;
using SharpPeg.Operators;
using SharpPeg.Runner;

namespace PegGrepTests
{
    [TestClass]
    public class PegLoaderTests
    {
        [TestMethod]
        public void BasicLoading()
        {
            var loader = new PegLoader(new[] { "." });
            var result = loader.Parse("Test::A");
        }

        [TestMethod]
        public void ParameterizedLoading()
        {
            MustMatch("Test2::OneOrMore<\\([a-z])>", "arstoe");
            MustMatch("Test2::OneOrMore<\\([a-z])>", "a");
            MustNotMatch("Test2::OneOrMore<\\([a-z])>", "");
            MustNotMatch("Test2::OneOrMore<\\([a-z])>", "XXXXar");
        }

        [TestMethod]
        public void NestedParameterizedLoading()
        {
            MustMatch("Test3::Record<\\(',')>", "a,b,c");
        }

        [TestMethod]
        public void NestedReferencingParameters()
        {
            MustMatch("Quotes::Quoted<\\([a-z]+)>", "\"hello\"");
        }

        [TestMethod]
        public void FixedRepeat()
        {
            MustMatch("[a-z]^3", "abc");
            MustMatch("[a-z]^3 '-' [a-z]^3", "abc-def");
            MustNotMatch("[a-z]^3", "ac");
            MustNotMatch("[a-z]^3 !.", "abcd");
        }

        [TestMethod]
        public void RangeRepeat()
        {
            MustMatch("[a-z]^1..3", "a");
            MustMatch("[a-z]^1..3", "ab");
            MustMatch("[a-z]^1..3", "abc");
            MustNotMatch("[a-z]^1..3", "");
            MustNotMatch("[a-z]^1..3 !.", "abcd");
            MustNotMatch("[a-z]^1..3", "#");
            MustMatch("[a-z]^3 '-' [a-z]^3", "abc-def");
            MustNotMatch("[a-z]^3", "ac");
            MustNotMatch("[a-z]^3 !.", "abcd");
        }

        [TestMethod]
        public void Empty()
        {
            MustMatch("e", "");
            MustNotMatch("e !.", "abcd");
        }

        [TestMethod]
        public void Slashes()
        {
            MustMatch("'//'", "//");
        }

        private void MustNotMatch(string pattern, string matchable)
        {
            var result = GetResult(pattern, matchable);

            Assert.IsFalse(result.IsSuccessful, $"Pattern {pattern} must not match {matchable}");
        }

        private void MustMatch(string pattern, string matchable)
        {
            var result = GetResult(pattern, matchable);

            Assert.IsTrue(result.IsSuccessful && result.InputPosition >= matchable.Length, $"Pattern {pattern} must match {matchable}");
        }

        private RunResult GetResult(string pattern, string matchable)
        {
            var loader = new PegLoader(new[] { "." });
            var loaded = loader.Parse(pattern);
            var runner = PatternCompiler.Default.Compile(new Pattern() { Data = loaded });
            var result = runner.Run(matchable);
            return result;
        }
    }
}
