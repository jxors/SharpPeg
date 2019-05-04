using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Compilation;
using SharpPeg.Operators;
using SharpPeg.Optimizations;
using SharpPeg.Runner.ILRunner;
using SharpPeg.Runner.Interpreter;
using System.Linq;

namespace SharpPegTests
{
    [TestClass]
    public class CompilerTests
    {
        private Operator letters = new CharacterClass(new CharacterRange('a', 'z'));
        private Operator capitals = new CharacterClass(new CharacterRange('A', 'Z'));

        private Operator capitalsAndNonCapitals = new CharacterClass(new CharacterRange('a', 'z'), new CharacterRange('A', 'Z'));

        [TestMethod]
        public void Atoms()
        {
            var any = new Any();
            MustMatch(any, "a");
            MustMatch(any, "z");
            MustMatch(any, "!");
            MustMatch(any, " ");
            MustNotMatch(any, "ab");
            MustNotMatch(any, "");
            
            MustMatch(letters, "a");
            MustMatch(letters, "f");
            MustMatch(letters, "z");
            MustNotMatch(letters, " ");
            MustNotMatch(letters, "A");
            MustNotMatch(letters, "!");
            MustNotMatch(letters, "");

            MustMatch(capitalsAndNonCapitals, "A");
            MustMatch(capitalsAndNonCapitals, "a");
            MustMatch(capitalsAndNonCapitals, "F");
            MustMatch(capitalsAndNonCapitals, "f");
            MustMatch(capitalsAndNonCapitals, "Z");
            MustMatch(capitalsAndNonCapitals, "z");
        }
        
        [TestMethod]
        public void Concatenation()
        {
            var p = new Sequence(letters, capitals);
            MustMatch(p, "aB");
            MustMatch(p, "fF");
            MustMatch(p, "zA");
            MustNotMatch(p, "++");
            MustNotMatch(p, "Aa");
            MustNotMatch(p, "!A");
            MustNotMatch(p, "");
            MustNotMatch(p, "+");
        }

        [TestMethod]
        public void Choice()
        {
            var p = new PrioritizedChoice(letters, capitals);
            MustMatch(p, "a");
            MustMatch(p, "F");
            MustMatch(p, "z");
            MustNotMatch(p, "++");
            MustNotMatch(p, "Aa");
            MustNotMatch(p, "0");
            MustNotMatch(p, "");
            MustNotMatch(p, "+");
        }

        [TestMethod]
        public void ConcatenationInChoice()
        {
            var first = new Sequence(new Any(), letters);
            var second = new Sequence(new Any(), new CharacterClass(new CharacterRange('A', 'Z')));

            var p = new PrioritizedChoice(first, second);
            MustMatch(p, "xa");
            MustMatch(p, ")F");
            MustMatch(p, "6z");
            MustNotMatch(p, "+");
            MustNotMatch(p, "A");
            MustNotMatch(p, "+");
            MustNotMatch(p, "");
            MustNotMatch(p, "++++++");
        }

        [TestMethod]
        public void Repetition()
        {
            var p = new ZeroOrMore(letters);

            MustMatch(p, "xa");
            MustMatch(p, "");
            MustMatch(p, "aaaaaaaaaaaaaaaaaaaaaaaaaaa");
            MustMatch(p, "arsndhtaibtarsbt");
            MustNotMatch(p, "+");
            MustNotMatch(p, "+aaaaaa");
            MustNotMatch(p, "Baaaaa");
            MustMatchPartial(p, "abcdefNHNEISH", 6);
            MustNotMatch(p, "++++++");
        }

        [TestMethod]
        public void Not()
        {
            var p = new Not(letters);

            MustMatchPartial(p, "+", 0);
            MustMatchPartial(p, "", 0);
            MustMatchPartial(p, "Aaaaaaaaaaaaaaaa", 0);
            MustMatchPartial(p, "+++++", 0);
            MustMatchPartial(p, "Nest", 0);
            MustNotMatch(p, "a");
            MustNotMatch(p, "f");
            MustNotMatch(p, "z");
            MustNotMatch(p, "++++++");
        }

        [TestMethod]
        public void NestedChoice()
        {
            var first = new PrioritizedChoice(new CharacterClass('S'), letters);
            var second = new PrioritizedChoice(new CharacterClass('T'), capitals, new CharacterClass('.'));

            var p = new PrioritizedChoice(first, second);

            MustMatch(p, "S");
            MustMatch(p, "a");
            MustMatch(p, "T");
            MustMatch(p, ".");
            MustNotMatch(p, "");
            MustNotMatch(p, "ff");
            MustNotMatch(p, "zzzzzzzz");
            MustNotMatch(p, "AbTs");
            MustNotMatch(p, "++++");
        }

        [TestMethod]
        public void RegexChoice()
        {
            var first = new Sequence(new Any(), new Any(), new CharacterClass('S'));
            var second = new Sequence(new Any(), new Any(), new CharacterClass('T'));

            var p = new PrioritizedChoice(first, second);

            MustMatch(p, "abS");
            MustMatch(p, "zyT");
            MustMatch(p, "ccS");
            MustNotMatch(p, "a");
            MustNotMatch(p, "ff");
            MustNotMatch(p, "zzzzzzzz");
            MustNotMatch(p, "AbTs");
            MustNotMatch(p, "++++");
        }

        [TestMethod]
        public void FailingNonTerminal()
        {
            // Note: additional non-terminals to prevent inlining.
            var any = new Pattern("any") { Data = new Any() };
            var lettersNT = new Pattern("lettersNT") { Data = letters };
            var first = new Pattern("first") { Data = new Sequence(any, any, new CharacterClass('S')) };
            var second = new Pattern("second") { Data = new PrioritizedChoice(letters, lettersNT) };

            var p = new Sequence(new Any(), new PrioritizedChoice(new Sequence(first, letters), new Sequence(second, letters)));

            MustMatch(p, "-aaSa");
            MustMatch(p, "-bc");
            MustMatch(p, "---Sa");
            MustNotMatch(p, "ab");
            MustNotMatch(p, "fX");
            MustNotMatch(p, "zzzzzzzz");
            MustNotMatch(p, "AbTs");
            MustNotMatch(p, "++++");
        }

        private void MustMatch(Operator p, string data)
        {
            var result = Match(p, data);
            Assert.AreEqual(data.Length, result, $"{p} must match '{data}'");
        }

        private void MustMatchPartial(Operator p, string data, int matchLength)
        {
            var result = Match(p, data);
            Assert.AreEqual(matchLength, result, $"{p} must match the first {matchLength} chars of '{data}'");
        }

        private void MustNotMatch(Operator p, string data)
        {
            var result = Match(p, data);
            Assert.AreNotEqual(data.Length, result, $"{p} must not match '{data}'");
        }

        private PatternCompiler[] compilers = new PatternCompiler[]
        {
            // First compiler is the one we're comparing to, so it's best to use the one with the least optimizations & complex CIL codegen involved
            new PatternCompiler(new Compiler(), new DefaultOptimizer
            {
                Optimizations = new SharpPeg.Optimizations.Default.OptimizationBase[0],
                RareOptimizations = new SharpPeg.Optimizations.Default.OptimizationBase[0],
            }, new InterpreterJitter()),
            PatternCompiler.Default,
            new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter() { EnableMemoization = true, EnableCaptureMemoization = false }),
            new PatternCompiler(new Compiler(), new DefaultOptimizer(), new ILJitter() { EnableMemoization = true, EnableCaptureMemoization = true }),
            new PatternCompiler(new Compiler(), new DefaultOptimizer(), new InterpreterJitter()),
        };

        private int Match(Operator p, string data)
        {
            var results = compilers.Select(compiler => compiler.Compile(new Pattern() { Data = p }).Run(data)).ToList();
            var first = results.First();

            foreach (var result in results)
            {
                Assert.AreEqual(first.InputPosition, result.InputPosition);
                Assert.AreEqual(first.IsSuccessful, result.IsSuccessful);
            }

            var matchSuccesful = first.IsSuccessful;
            return matchSuccesful ? first.InputPosition : -1;
        }
    }
}
