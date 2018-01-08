using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegMatch.Grammar;
using System.Linq;

namespace PegGrepTests
{
    [TestClass]
    public class GrammarTests
    {
        [TestMethod]
        public void NamespacedIdentifiers()
        {
            Test("Test::A::Namespace");
        }

        private void Test(string data, string expected = null)
        {
            var grammar = new ExtendedPegGrammar();
            var result = grammar.ParseExpression(data);
            Assert.AreEqual(expected ?? data, result.ToString());
        }

        private void Test2(string data, string expectedPatternName, string expectedPatternData)
        {
            var grammar = new ExtendedPegGrammar();
            var result = grammar.ParseGrammar(data);
            Assert.AreEqual(expectedPatternName, result.First().ToString());
            Assert.AreEqual(expectedPatternData, result.First().Data.ToString());
        }

        [TestMethod]
        public void ParameterizedPatterns()
        {
            Test("Pattern");
            Test("Pattern<A>");
            Test("Pattern<A, B>");
            Test("Pattern<A, Namespace::B>");
        }

        [TestMethod]
        public void ParameterizedPatternDefinitions()
        {
            Test2("Pattern <- [a-z]", "Pattern", "[a-z]");
            Test2("Pattern<A> <- [a-z]", "Pattern<A>", "[a-z]");
            Test2("Pattern<A, B> <- [a-z]", "Pattern<A, B>", "[a-z]");
        }

        [TestMethod]
        public void InlinePatterns()
        {
            var grammar = new ExtendedPegGrammar();
            Test("Pattern<\\('a')>", "Pattern<Inline>");
        }
    }
}
