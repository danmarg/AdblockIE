using af0.Adblock;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace af0.Adblock.UnitTests
{
    /// <summary>
    ///This is a test class for GlobTest and is intended
    ///to contain all GlobTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GlobTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        public void LiteralTest()
        {
            Glob target = new Glob("hamandeggs");
            Assert.IsTrue(target.IsMatch("hamandeggs")); // a literal should be matched
            Assert.IsTrue(target.IsMatch("HAMANDEGGS")); // as should be capitals
            Assert.IsTrue(target.IsMatch("HaMaNDeggS")); // or mixed case
            Assert.IsFalse(target.IsMatch("spamhamandeggs")); // without a star, this shouldn't match
            Assert.IsFalse(target.IsMatch("hamandeggsandspam"));
            Assert.IsFalse(target.IsMatch("spamhamandeggsandbacon")); // nor these two
            Assert.IsFalse(target.IsMatch("")); // just to be on the safe side
            Assert.IsFalse(target.IsMatch("and")); // substrings are never matches
        }

        [TestMethod()]
        public void StarTest()
        {
            Glob target = new Glob("*");
            Assert.IsTrue(target.IsMatch(""));
            Assert.IsTrue(target.IsMatch("*"));
            Assert.IsTrue(target.IsMatch("hamANDeggs"));
            target = new Glob("ham*");
            Assert.IsTrue(target.IsMatch("ham"));
            Assert.IsTrue(target.IsMatch("hamandeggs"));
            Assert.IsFalse(target.IsMatch("eggshamandeggs"));
            target = new Glob("*eggs");
            Assert.IsTrue(target.IsMatch("hamandeggs"));
            Assert.IsTrue(target.IsMatch("eggs"));
            Assert.IsFalse(target.IsMatch("hamandeggsandbacon"));
            target = new Glob("ham*eggs");
            Assert.IsTrue(target.IsMatch("hamandeggs"));
            Assert.IsTrue(target.IsMatch("hameggs"));
            Assert.IsFalse(target.IsMatch("ham"));
            Assert.IsFalse(target.IsMatch("eggs"));
        }

        [TestMethod()]
        public void QmarkTest()
        {
            Glob target = new Glob("?");
            Assert.IsTrue(target.IsMatch("h"));
            Assert.IsFalse(target.IsMatch("ham"));
            Assert.IsFalse(target.IsMatch(""));
            target = new Glob("ham?");
            Assert.IsTrue(target.IsMatch("hama"));
            Assert.IsFalse(target.IsMatch("hamand"));
        }

        [TestMethod()]
        public void ComboTest()
        {
            Glob target = new Glob("*?eggs");
            Assert.IsTrue(target.IsMatch("hamandeggs"));
            Assert.IsFalse(target.IsMatch("eggs"));
            target = new Glob("ham?*");
            Assert.IsTrue(target.IsMatch("hamandeggs"));
            Assert.IsFalse(target.IsMatch("ham"));
        }

        [TestMethod()]
        public void EscapeTest()
        {
            Glob target = new Glob(@"ham\?");
            Assert.IsTrue(target.IsMatch("ham?"));
            Assert.IsFalse(target.IsMatch("ham"));
            Assert.IsFalse(target.IsMatch("hama"));
            target = new Glob(@"eggs\*");
            Assert.IsTrue(target.IsMatch("eggs*"));
            Assert.IsFalse(target.IsMatch("eggs"));
            Assert.IsFalse(target.IsMatch("eggsa"));
            target = new Glob(@"eggs\\");
            Assert.IsTrue(target.IsMatch(@"eggs\"));
            Assert.IsFalse(target.IsMatch(@"eggsa"));
            target = new Glob(@"eggs\\?");
            Assert.IsTrue(target.IsMatch(@"eggs\a"));
            Assert.IsFalse(target.IsMatch(@"eggs\"));
            target = new Glob(@"eggs\\\?");
            Assert.IsTrue(target.IsMatch(@"eggs\?"));
            Assert.IsFalse(target.IsMatch(@"eggs\a"));
            target = new Glob(@"eggs\andham");
            Assert.IsTrue(target.IsMatch(@"eggs\andham"));
        }
    }
}
