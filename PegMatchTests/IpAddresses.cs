using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegGrepTests
{
    [TestClass]
    public class IpAddresses : PegTest
    {
        [TestMethod]
        public void Ipv4Addresses()
        {
            MustMatch("Net::Ipv4::Address", "0.1.5.9");
            MustMatch("Net::Ipv4::Address", "10.0.0.1");
            MustMatch("Net::Ipv4::Address", "255.255.255.255");
            MustMatch("Net::Ipv4::Address", "255.250.249.239");
            MustMatch("Net::Ipv4::Address", "001.030.003.05");
            MustNotMatch("Net::Ipv4::Address", "256.2456.0.255");
            MustNotMatch("Net::Ipv4::Address", "200.abc.0.255");
        }

        [TestMethod]
        public void Ipv6Addresses()
        {
            MustMatch("Net::Ipv6::Address", "::1");
            MustMatch("Net::Ipv6::AlternativeCollapsedAddress", "::f00d:10.0.0.1");
            MustMatch("Net::Ipv6::Address", "::f00d:10.0.0.1");
            MustMatch("Net::Ipv6::StandardFullAddress", "0:0:0:0:0:0:0:0");
            MustMatch("Net::Ipv6::FullAddress", "0:0:0:0:0:0:0:0");
            MustNotMatch("Net::Ipv6::CollapsedAddress", "0:0:0:0:0:0:0:0");
            MustMatch("Net::Ipv6::Address", "0:0:0:0:0:0:0:0");
            MustMatch("Net::Ipv6::StandardCollapsedAddress", "abcd:ef01:2345:6789::");
            MustMatch("Net::Ipv6::CollapsedAddress", "abcd:ef01:2345:6789::");
            MustMatch("Net::Ipv6::Address", "abcd:ef01:2345:6789::");
            MustMatch("Net::Ipv6::Address", "abcd:ef01:2345:6789:abcd:ef01:2345:6789");
            MustNotMatch("Net::Ipv6::Address", "abcd:ef01:2345:6789:abcd:ef01:2345:6789:0123");
            MustNotMatch("Net::Ipv6::Address", "abcd:ef01:2345:6789:abcd:1:1234:0.0.0.0");
            MustNotMatch("Net::Ipv6::Address", "abcd::1::1234");
            MustNotMatch("Net::Ipv6::Address", "0:0:0:0:0:0:0:0:0");
        }
    }
}
