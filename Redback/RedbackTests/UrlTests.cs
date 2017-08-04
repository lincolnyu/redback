using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redback.Helpers;

namespace RedbackTests
{
    [TestClass]
    public class UrlTests
    {
        [TestMethod]
        public void TestCombineUrls()
        {
            string url1 = "www.consoto.com/foo/bar/foo.html";
            string url2 = "./../../something.html";
            string expected = "http://www.consoto.com/something.html";
            string expectedb = "www.consoto.com/something.html";
            var abs1 = UrlHelper.GetAbsoluteUrl(url1, url2);
            var abs2 = UrlHelper.GetAbsoluteUrl2(url1, url2);
            var abs2b = UrlHelper.GetAbsoluteUrl2(url1, url2, false);
            Assert.AreEqual(expectedb, abs1);
            Assert.AreEqual(expected, abs2);
            Assert.AreEqual(expectedb, abs2b);
        }
    }
}
