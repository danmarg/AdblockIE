using af0.Adblock;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace af0.Adblock.UnitTests
{
    
    
    /// <summary>
    ///This is a test class for AdblockTest and is intended
    ///to contain all AdblockTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AdblockTest
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


        /// <summary>
        ///A test for ShouldBlock
        ///</summary>
        [TestMethod()]
        [DeploymentItem("Adblock.dll")]
        public void ShouldBlockTest()
        {
            AdblockEngine_Accessor target = new AdblockEngine_Accessor(new object());
            AdblockEngine_Accessor.Rule blockRule = new AdblockEngine_Accessor.Rule(new XElement("rule",
                                                                                      new XAttribute("action", "block"),
                                                                                      "*/adx/*"));
            AdblockEngine_Accessor.Rule passRule = new AdblockEngine_Accessor.Rule(new XElement("rule",
                                                                                      new XAttribute("site", "nytimes.com"),
                                                                                      new XAttribute("action", "pass"),
                                                                                      "*/adx/*"));
            target._allBlacklist = new AdblockEngine_Accessor.Rule[] { blockRule };
            target._allWhitelist = new AdblockEngine_Accessor.Rule[] { passRule };

            Assert.IsTrue(target.ShouldBlock("http://www.google.com", "http://nytimes.com/adx/banner", AdblockEngine_Accessor.ApplyTo.Image));
            Assert.IsFalse(target.ShouldBlock("http://global.nytimes.com", "http://google.com/adx/banner", AdblockEngine_Accessor.ApplyTo.Div));
            Assert.IsTrue(target.ShouldBlock("http://www.google.com/nytimes.com/", "http://anyone/adx/", AdblockEngine_Accessor.ApplyTo.Frame));
            // TODO: etc
            
        }
    }
}
