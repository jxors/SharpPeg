using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Compilation;
using SharpPeg.Operators;
using SharpPeg.Runner.ILRunner;
using RegexToPeg;

namespace RegexToPegTests
{
    [TestClass]
    public class RegexGrammarTests
    {
        private RegexGrammar grammar = new RegexGrammar(new PatternCompiler(new Compiler(), null, new ILJitter()));

        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsTrue(grammar.ParseExpression(@"Tom.{10,25}river|river.{10,25}Tom") != null);
            Assert.IsTrue(grammar.ParseExpression("a") != null);
            Assert.IsTrue(grammar.ParseExpression(@"\w+") != null);
            Assert.IsTrue(grammar.ParseExpression(".*") != null);
            Assert.IsTrue(grammar.ParseExpression("th[a-z]+") != null);
            Assert.IsTrue(grammar.ParseExpression(@"\b(ME|YOU)\b") != null);
            Assert.IsTrue(grammar.ParseExpression(@"a{3,5}") != null);

            Matches(grammar, @"Tom.{10,25}river|river.{10,25}Tom", "river; and when Tom");
            Matches(grammar, @"river.{10,25}Tom", "river, either. Tom");

            Matches(grammar, "[a-z]{3,5}", "abcdz");
            Matches(grammar, "[a-z]{3,5}", "abcd");
            Matches(grammar, "[a-z]{3,5}", "abc");
            Matches(grammar, "[a-z]shing", "ashing");
            Matches(grammar, "this\\s[a-z]+", "this that");
            Matches(grammar, "Huck[a-zA-Z]+|Saw[a-zA-Z]+", "Sawyer");
            //Matches(grammar, "\\b\\w+nn\\b", "Lynn");
            Matches(grammar, "Tom|Sawyer|Huckleberry|Finn", "Huckleberry");
            Matches(grammar, ".{0,2}(Tom|Sawyer|Huckleberry|Finn)", "abHuckleberry");
            Matches(grammar, ".{2,4}(Tom|Sawyer|Huckleberry|Finn)", "abHuckleberry");
        }

        [TestMethod]
        public void TestRepeat()
        {
            Matches(grammar, "[a-z]+ing", "ingingiing");
            Matches(grammar, "[a-z]+ing", "ingingingining");
        }

        // TODO: Broken by DelayBoundsCheckOptimization.
        [TestMethod]
        public void VerifyBoundsOptimization()
        {
            Matches(grammar, "..(T|S)", "abS");
            Matches(grammar, "..(T|SS|HHH|FFFF)", "abHHH");
        }

        private void Matches(RegexGrammar g, string regex, string s)
        {
            var converter = new RegexConverter();
            var c = converter.Convert(g.ParseExpression(regex));
            
            var runner = PatternCompiler.Default.Compile(new Pattern() { Data = c });
            var result = runner.Run(s);

            Assert.IsTrue(result.IsSuccessful && result.InputPosition >= s.Length, $"PEG from regex {regex} must match {s}. Matched {result.InputPosition} characters. Success: {result.IsSuccessful}");
        }
    }
}
