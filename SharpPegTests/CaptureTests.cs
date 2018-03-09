using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Compilation;
using SharpPeg.Operators;
using SharpPeg.Optimizations;
using SharpPeg.Runner;
using SharpPeg.Runner.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace SharpPegTests
{
    [TestClass]
    public class CaptureTests
    {
        private CharacterClass letters = new CharacterClass(new CharacterRange('a', 'z'));

        [TestMethod]
        public void BasicCapture()
        {
            var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, letters), new Any()));

            var captures = Match(p, "...a...b...c");

            Assert.AreEqual(3, captures.Count);

            Assert.IsTrue(captures.Contains("a"));
            Assert.IsTrue(captures.Contains("b"));
            Assert.IsTrue(captures.Contains("c"));

            Assert.IsFalse(captures.Contains(""));
            Assert.IsFalse(captures.Contains("."));
            Assert.IsFalse(captures.Any(item => item.Length != 1));
        }

        [TestMethod]
        public void EmptyCaptureKeep()
        {
            var whitespace = new Pattern() { Data = new CaptureGroup(0, Operator.Optional(Operator.OneOrMore(new Pattern { Data = ' ' }))) };
            var other = new Pattern { Data = new CaptureGroup(1, new PrioritizedChoice(new Pattern { Data = new CaptureGroup(100, "alongstring") }, new Pattern { Data = new Pattern { Data = new CaptureGroup(2, "y_alsoverylong") } })) };
            var p = new Sequence("if", whitespace, other);

            var captures = Match(p, "ify_alsoverylong");

            Assert.AreEqual(3, captures.Count);

            Assert.IsTrue(captures.Contains(""));
            Assert.IsTrue(captures.Contains("y_alsoverylong"));

            Assert.IsFalse(captures.Contains("a"));
            Assert.IsFalse(captures.Contains("x"));
            Assert.IsFalse(captures.Contains("ax"));
        }

        [TestMethod]
        public void EmptyCaptureDiscard()
        {
            var whitespace = new Pattern() { Data = new CaptureGroup(0, Operator.Optional(Operator.OneOrMore(new Pattern { Data = ' ' }))) };
            var other = new Pattern { Data = new CaptureGroup(1, new PrioritizedChoice(new Pattern { Data = new CaptureGroup(100, "alongstring") }, new Pattern { Data = new Pattern { Data = new CaptureGroup(2, "y_alsoverylong") } })) };
            var p = new Sequence("if", new PrioritizedChoice(new Sequence(whitespace, other), 'x'));

            var captures = Match(p, "ifx");

            Assert.AreEqual(0, captures.Count);
        }

        [TestMethod]
        public void EmptyCaptureOrder()
        {
            var whitespace = new Pattern() { Data = new CaptureGroup(0, Operator.Optional(Operator.OneOrMore(new Pattern { Data = ' ' }))) };
            var name = new Pattern { Data = new CaptureGroup(1, Operator.OneOrMore(new CharacterClass('a', 'z'))) };
            var var = new Pattern { Data = new CaptureGroup(2, new Sequence(whitespace, name)) };
            var p = new Sequence("if", var);

            var captures = Match(p, "ifabc");

            Assert.AreEqual(3, captures.Count);
            Assert.AreEqual("a", captures[0]);
            Assert.AreEqual("", captures[1]);
            Assert.AreEqual("a", captures[2]);
        }

        [TestMethod]
        public void WordCapture()
        {
            var p = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, Operator.OneOrMore(letters)), new Any()));

            var captures = Match(p, "hello world test ABC ...");

            Assert.AreEqual(3, captures.Count);

            Assert.IsTrue(captures.Contains("hello"));
            Assert.IsTrue(captures.Contains("world"));
            Assert.IsTrue(captures.Contains("test"));

            Assert.IsFalse(captures.Contains("ABC"));
            Assert.IsFalse(captures.Contains("."));
            Assert.IsFalse(captures.Contains("..."));
        }

        private static List<string> Match(Operator p, string data, bool optimize = true)
        {
            var runner = new PatternCompiler(new Compiler(), optimize ? new DefaultOptimizer() : null, new InterpreterJitter()).Compile(new Pattern() { Data = p });
            var captures = new List<Capture>();
            var result = runner.Run(data, captures);

            Assert.IsTrue(result.IsSuccessful);
            return captures.Select(item => data.Substring(item.StartPosition, item.EndPosition - item.StartPosition)).ToList();
        }
    }
}
