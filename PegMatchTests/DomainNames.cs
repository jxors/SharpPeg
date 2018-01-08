using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegGrepTests
{
    [TestClass]
    public class DomainNames : PegTest
    {
        [TestMethod]
        public void SimpleDomains()
        {
            MustMatch("Net::Rfc1035::Domain", "google");
            MustMatch("Net::Rfc1035::Domain", "google.com");
            MustMatch("Net::Rfc1035::Domain", "xn--google.com");

            // Labels may not start or end with a -
            MustNotMatch("Net::Rfc1035::Domain", "-google.com");
            MustNotMatch("Net::Rfc1035::Domain", "google-.com");

            // A label must be at least 1 character
            MustNotMatch("Net::Rfc1035::Domain", "..google.com");

            // Each label must be 63 characters or less
            MustMatch("Net::Rfc1035::Domain", new string('a', 63) + ".com");
            MustNotMatch("Net::Rfc1035::Domain", new string('a', 64) + ".com");
        }
    }
}
