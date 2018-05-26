using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg.Common;
using SharpPeg.Compilation;
using SharpPeg.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPegTests
{
    [TestClass]
    public class LeftRecursionDetectorTests
    {
        [TestMethod]
        [ExpectedException(typeof(CompilationException))]
        public void DirectLeftRecursion()
        {
            var p = new Pattern("p");
            p.Data = new PrioritizedChoice(p, "abc");

            LeftRecursionDetector.Check(p);
        }
    }
}
