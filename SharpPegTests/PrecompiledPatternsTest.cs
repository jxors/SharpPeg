using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Common;
using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPegTests
{
    [TestClass]
    public class PrecompiledPatternsTest
    {
        [TestMethod]
        public void SimplePrecompiled()
        {
            var p = new PrecompiledPattern(new Method("Precompiled", new List<Instruction>()
            {
                Instruction.Char(0, 0, 0, 1),
                Instruction.Char(0, 1, 1, 2),
                Instruction.Advance(2),
                Instruction.Return(0),
                Instruction.MarkLabel(0),
                Instruction.Return(1),
            }, new List<CharRange>()
            {
                new CharRange('a', 'a'),
                new CharRange('b', 'b'),
            }, new List<LabelMap>(), 0, 1), new List<Pattern>());

            MustMatch(p, "ab");
            MustNotMatch(p, "bb");
            MustNotMatch(p, "ac");
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

        private static int Match(Operator p, string data)
        {
            var runner = PatternCompiler.Default.Compile(new Pattern() { Data = p });
            var result = runner.Run(data);
            var matchSuccesful = result.IsSuccessful;
            return matchSuccesful ? result.InputPosition : -1;
        }
    }
}
