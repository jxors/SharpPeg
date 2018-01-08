using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegGrepTests
{
    [TestClass]
    public class Uris : PegTest
    {
        [TestMethod]
        public void Scheme()
        {
            MustMatch("Net::Uri::Scheme", "http");
        }

        [TestMethod]
        public void Host()
        {
            MustMatch("Net::Uri::Host", "google.com");
            MustMatch("Net::Uri::Host", "10.0.0.1");
            MustMatch("Net::Uri::Host", "[::1]");
        }

        [TestMethod]
        public void WebAddresses()
        {
            MustMatch("Net::Uri::Authority", "google.com");
            MustMatch("Net::Uri::AbsoluteUri", "http://google.com");
            MustMatch("Net::Uri::Uri", "http://google.com");
        }
    }
}
