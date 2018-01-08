using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg;
using SharpPeg.Operators;
using SharpPeg.Runner;
using RegexToPeg;
using System.Collections.Generic;

namespace SharpPegTests
{
    [TestClass]
    public class CaptureCountTests
    {
        [TestMethod]
        public void One()
        {
            Assert.AreEqual(4, GetNumCaptures(".{2,4}(Tom|Sawyer|Huckleberry|Finn)", "Tom..Huckleberry  Finn         Tom  Tom  Huck\nFinn,")); 
            Assert.AreEqual(0, GetNumCaptures("river.{10,25}Tom|Tom.{10,25}river", "A Good Man--A Sermon from the Tom"));
            Assert.AreEqual(0, GetNumCaptures("river.{10,25}Tom|Tom.{10,25}river", "f\nit?\"\n\n\"Cure him!  No!  When Tom"));
        }

        [TestMethod]
        public void GreedyEndsWith()
        {
            Assert.AreEqual(1, GetNumCaptures("([a-z][A-Z])+ing", "aBcDing"));
            Assert.AreEqual(1, GetNumCaptures("([a-z][A-Z])+ing", "aaBcDing"));
            Assert.AreEqual(1, GetNumCaptures("([a-z][A-Z])+iNg", "aBcDiNg"));
            Assert.AreEqual(1, GetNumCaptures("([a-z][A-Z])+iNg", "aaBcDiNg"));
        }

        private static int GetNumCaptures(string regex, string strData)
        {
            var data = strData.ToCharArray();
            var g = new RegexGrammar(PatternCompiler.Default);

            var converter = new RegexConverter();
            var c = converter.Convert(g.ParseExpression(regex));

            var matchPattern = new ZeroOrMore(new PrioritizedChoice(new CaptureGroup(0, c), new Any()));

            var p = new Pattern(null)
            {
                Data = matchPattern
            };

            var runner = PatternCompiler.Default.Compile(p);
            var captures = new List<Capture>();
            var result = runner.Run(data, 0, data.Length, captures);

            return result.IsSuccessful ? captures.Count : -1;
        }
    }
}
