using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpPeg.SelfParser;

namespace SharpPegTests
{
    [TestClass]
    public class PegGrammarTests
    {
        [TestMethod]
        public void TestGrammar()
        {
            var grammar = new PegGrammar();

            Assert.AreEqual("'a'*", grammar.ParseExpression("'a'*").ToString());

            Assert.AreEqual("[a-z]", grammar.ParseExpression("[\\141-\\172]").ToString());
            //Assert.AreEqual("('a' / 'b' / 'c') / e", grammar.ParseExpression("('a' / 'b' / 'c')?").ToString());
            //Assert.AreEqual("'c' / ('a'* 'b') / e", grammar.ParseExpression("'c' / ('a'* 'b')?").ToString());
        }
    }
}
