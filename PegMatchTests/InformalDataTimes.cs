using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegGrepTests;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatchTests
{
    [TestClass]
    public class InformalDataTimes : PegTest
    {
        [TestMethod]
        public void InformalDate()
        {
            MustMatch("DateTime::Informal::Date", "11 November 2017");
            MustMatch("DateTime::Informal::Date", "November 11th 2017");
            MustMatch("DateTime::Informal::Date", "1 January");
            MustMatch("DateTime::Informal::Date", "31 March 4902");
        }

        [TestMethod]
        public void InformalTime()
        {
            MustMatch("DateTime::Rfc3339::Time", "11:23:10");
            MustMatch("DateTime::Rfc3339::Time", "15:36:10");
            MustMatch("DateTime::Rfc3339::Time", "15:55:18");

            MustNotMatch("DateTime::Rfc3339::Time", "75:23:10");
            MustNotMatch("DateTime::Rfc3339::Time", "15:66:10");
            MustNotMatch("DateTime::Rfc3339::Time", "15:55:78");
        }
    }
}
