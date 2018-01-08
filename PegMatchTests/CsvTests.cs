using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegGrepTests
{
    [TestClass]
    public class CsvTests : PegTest
    {
        [TestMethod]
        public void CsvRecord()
        {
            MustMatch(@"File::Csv::Record<\(',')>", "Item, Item, Item");
            MustMatch(@"File::Csv::Record<\(',')>", "Item, Item");
            MustMatch(@"File::Csv::Record<\(',')>", "Item, \"Item\"");
            MustMatch("File::Csv::Record<\\(\",\")>", "Item, Item, Item");
        }
        
    }
}
