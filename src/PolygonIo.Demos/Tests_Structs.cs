using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace PolygonIo.Demos
{
    [TestClass]
    public class Tests_Structs
    {
        [TestMethod]
        public void Test_StringStructMax12_New()
        {
            _ = new StringStructMax12();
            
            // Assert.AreEqual(16, sizeof(StringStructMax12)); // Check option "Allow Unsafe Code" for the sizeof operator to work on this struct
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_StringStructMax12_New_ArgumentNullException()
        {
            _ = new StringStructMax12(null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Test_StringStructMax12_New_ArgumentOutOfRangeException()
        {
            _ = new StringStructMax12(new string('*', 13));
        }
        [TestMethod]
        public void Test_StringStructMax12_New_fromString()
        {
            _ = new StringStructMax12(string.Empty);
            _ = new StringStructMax12("antondelsink");

            for (int len = 1; len <= 12; len++)
            {
                _ = new StringStructMax12(new string('*', len));
            }
        }
        [TestMethod]
        public void Test_StringStructMax12_Casts()
        {
            _ = (StringStructMax12)string.Empty;
            _ = (StringStructMax12)"antondelsink";

            for (int len = 1; len <= 12; len++)
            {
                _ = (StringStructMax12)string.Empty.PadLeft(len, '*');
            }
        }
        [TestMethod]
        [DataRow("*", 0)]
        [DataRow("**", 1)]
        [DataRow("***", 2)]
        [DataRow("****", 3)]
        [DataRow("*****", 4)]
        [DataRow("******", 5)]
        [DataRow("*******", 6)]
        [DataRow("********", 7)]
        [DataRow("*********", 8)]
        [DataRow("**********", 9)]
        [DataRow("***********", 10)]
        [DataRow("************", 11)]
        public void Test_StringStructMax12_Indexer(string validString12, int indexMax)
        {
            _ = new StringStructMax12(validString12)[0];
            _ = new StringStructMax12(validString12)[indexMax];
        }

        [TestMethod]
        [DataRow("*", 1)]
        [DataRow("**", 2)]
        [DataRow("***", 3)]
        [DataRow("****", 4)]
        [DataRow("*****", 5)]
        [DataRow("******", 6)]
        [DataRow("*******", 7)]
        [DataRow("********", 8)]
        [DataRow("*********", 9)]
        [DataRow("**********", 10)]
        [DataRow("***********", 11)]
        [DataRow("************", 12)]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Test_StringStructMax12_IndexOutOfRangeException(string validString12, int outOfRangeIndex)
        {
            _ = new StringStructMax12(validString12)[outOfRangeIndex];
        }

        [TestMethod]
        [DataRow("*", 1)]
        [DataRow("**", 2)]
        [DataRow("***", 3)]
        [DataRow("****", 4)]
        [DataRow("*****", 5)]
        [DataRow("******", 6)]
        [DataRow("*******", 7)]
        [DataRow("********", 8)]
        [DataRow("*********", 9)]
        [DataRow("**********", 10)]
        [DataRow("***********", 11)]
        [DataRow("************", 12)]
        public void Test_StringStructMax12_Length(string validString12, int length)
        {
            Assert.AreEqual(validString12.Length, length);
        }

        [TestMethod]
        public void Test_StringStructMax12_SymbolsFromPolygonDataFile()
        {
            var filename = @"C:\PolygonData\polygon.txt";
            Assert.IsTrue(File.Exists(filename));

            foreach (var line in File.ReadLines(filename))
            {
                string symbol = GetSymbol(line);

                _ = new StringStructMax12(symbol);
            }
        }

        private static string GetSymbol(string line)
        {
            var token = "sym";
            var indexOfValue = line.IndexOf(token) + token.Length + 3;
            var lengthOfValue = line.IndexOf('"', indexOfValue) - indexOfValue;
            var symbol = line.Substring(indexOfValue, lengthOfValue);
            return symbol;
        }

        [TestMethod]
        public void Test_StringStructMax12_SymbolsFromFile()
        {
            //Assert.AreEqual(56, sizeof(QuoteV2));

            var filename = @"C:\PolygonData\symbols.txt";
            Assert.IsTrue(File.Exists(filename));

            foreach (var line in File.ReadLines(filename))
            {
                var ssm12 = new StringStructMax12(line);
                Assert.AreEqual(line.Length, ssm12.Length);
            }
        }

        //[TestMethod]
        //public unsafe void Test_QuoteV2_SizeOf()
        //{
        //    Assert.AreEqual(64, sizeof(QuoteV2));
        //}
    }
}