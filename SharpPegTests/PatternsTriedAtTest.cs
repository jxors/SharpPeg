using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Compilation;
using SharpPeg.Optimizations;
using SharpPeg.Runner.ILRunner;
using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPegTests
{
    [TestClass]
    public class PatternsTriedAtTest
    {
        private const string StringData = "A <- '.'+ [a-z0-9]";

        [TestMethod]
        public void TestPegGrammar()
        {
            var grammar = new PegGrammar();
            grammar.EnsureGrammarBuilt();
            var runner = new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter
            {
                EnableMemoization = true,
                EnableCaptureMemoization = true,
            }).Compile(grammar.Grammar);
            var result = runner.Run(StringData);
            var patternNames = runner.GetPatternsTriedAt(StringData.Length).ToList();
            Assert.AreEqual(true, result.IsSuccessful);
            Assert.IsTrue(patternNames.Count > 0);
        }
    }
}
