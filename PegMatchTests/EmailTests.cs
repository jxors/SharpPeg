using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegGrepTests;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatchTests
{
    [TestClass]
    public class EmailTests : PegTest
    {
        [TestMethod]
        public void EmailAddress()
        {
            MustMatch("Net::Email::Address", "name@gmail.com");
            MustMatch("Net::Email::Address", "name.name+filter@abc.def.nl");
            MustMatch("Net::Email::Address", "John Doe <jdoe@machine.example>");
        }

        [TestMethod]
        public void EmailAddressList()
        {
            MustMatch("Net::Email::AddressList", "name@gmail.com");
            MustMatch("Net::Email::AddressList", "name@gmail.com, aesnht@sthdo.n");
            MustMatch("Net::Email::AddressList", "<boss@nil.test>, \"Giant; \\\"Big\\\" Box\" <sysservices@example.net>");
            MustMatch("Net::Email::AddressList", "Mary Smith <mary@x.test>, jdoe@example.org, Who? <one@y.test>");
        }
    }
}
