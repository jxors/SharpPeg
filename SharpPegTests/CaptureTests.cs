using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Operators;
using SharpPeg.Runner;
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

        private static List<string> Match(Operator p, string data)
        {
            var runner = PatternCompiler.Default.Compile(new Pattern() { Data = p });
            var captures = new List<Capture>();
            var result = runner.Run(data, captures);

            Assert.IsTrue(result.IsSuccessful);
            return captures.Select(item => data.Substring(item.StartPosition, item.EndPosition - item.StartPosition)).ToList();
        }
    }
}
