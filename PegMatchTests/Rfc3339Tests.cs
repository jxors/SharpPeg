using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegGrepTests;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegMatchTests
{
    [TestClass]
    public class Rfc3339Tests : PegTest
    {
        [TestMethod]
        public void Duration()
        {
            MustMatch("DateTime::Rfc3339::Duration", "P2D");
            MustMatch("DateTime::Rfc3339::Duration", "P2Y5M3D");
            MustMatch("DateTime::Rfc3339::Duration", "PT2H5M3S");
            MustMatch("DateTime::Rfc3339::Duration", "P2Y5M3DT2H5M3S");
            MustMatch("DateTime::Rfc3339::Duration", "P2Y");
        }

        public void DateTime()
        {
            MustMatch("DateTime::Rfc3339::DateTime", "2017-11-22 15:23:10");
            MustMatch("DateTime::Rfc3339::DateTime", "2017-11-22 15:23:10Z");
            MustMatch("DateTime::Rfc3339::DateTime", "2017-11-22");
            MustMatch("DateTime::Rfc3339::Date", "2017-11-22");
            MustMatch("DateTime::Rfc3339::DateTime", "15:23:10");
            MustMatch("DateTime::Rfc3339::Time", "15:23:10");

            MustNotMatch("DateTime::Rfc3339::Time", "75:23:10");
            MustNotMatch("DateTime::Rfc3339::Time", "15:66:10");
            MustNotMatch("DateTime::Rfc3339::Time", "15:55:78");
        }

        public void Period()
        {
            MustMatch("DateTime::Rfc3339::Period", "2017-11-22 15:23:10/2017-11-22 16:00:00");
            MustMatch("DateTime::Rfc3339::Period", "P2Y5M3D/2017-11-22 15:23:10");
            MustMatch("DateTime::Rfc3339::Period", "2017-11-22 15:23:10/T2H5M3S");
        }
    }
}
