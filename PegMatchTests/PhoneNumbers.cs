using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegGrepTests
{
    [TestClass]
    public class PhoneNumbers : PegTest
    {
        [TestMethod]
        public void NationalPhoneNumbers()
        {
            MustMatch("Phone::Phone", "0123456789");
            MustMatch("Phone::Phone", "012345 6789");
            MustMatch("Phone::Phone", "012 345 6789");
        }

        [TestMethod]
        public void InternationalPhoneNumbers()
        {
            MustMatch("Phone::Phone", "+31 012 345 6789");
            MustMatch("Phone::Phone", "+31 (012) 3456789");
            MustMatch("Phone::Phone", "+31 012 3456789");
            MustMatch("Phone::Phone", "+31 0123456789");
            MustMatch("Phone::Phone", "+310123456789");
            MustMatch("Phone::Phone", "+1 012 345 6789");
            MustMatch("Phone::Phone", "+1 012 3456789");
            MustMatch("Phone::Phone", "+1 0123456789");
            MustMatch("Phone::Phone", "+10123456789");
        }

        [TestMethod]
        public void InvalidPhoneNumbers()
        {
            MustNotMatch("Phone::Phone", "31012345935347895346789");
            MustNotMatch("Phone::Phone", "31");
            MustNotMatch("Phone::Phone", "31012");
            MustNotMatch("Phone::Phone", "31012345-9353478-95346789");
        }
    }
}
