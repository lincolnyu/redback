using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redback.Helpers;

namespace RedbackTests
{
    [TestClass]
    public class UrlTests
    {
        [TestMethod]
        public void TestCombineUrls1()
        {
            string url1 = "www.consoto.com/foo/bar/foo.html";
            string url2 = "./../../something.html";
            string expected = "http://www.consoto.com/something.html";
            string expectedb = "www.consoto.com/something.html";
            var abs1 = UrlHelper.GetAbsoluteUrl(url1, url2);
            var abs1b = UrlHelper.GetAbsoluteUrl(url1, url2, false);
            var abs2 = UrlHelper.GetAbsoluteUrl2(url1, url2);
            var abs2b = UrlHelper.GetAbsoluteUrl2(url1, url2, false);
            Assert.AreEqual(expected, abs1);
            Assert.AreEqual(expectedb, abs1b);
            Assert.AreEqual(expected, abs2);
            Assert.AreEqual(expectedb, abs2b);
        }

        [TestMethod]
        public void TestCombineUrls2()
        {
            string url1 = "http://www.consoto.com/foo/bar/foo.html";
            string url2 = "./../../something.html";
            string expected = "http://www.consoto.com/something.html";
            var abs1 = UrlHelper.GetAbsoluteUrl(url1, url2);
            var abs1b = UrlHelper.GetAbsoluteUrl(url1, url2, false);
            var abs2 = UrlHelper.GetAbsoluteUrl2(url1, url2);
            var abs2b = UrlHelper.GetAbsoluteUrl2(url1, url2, false);
            Assert.AreEqual(expected, abs1);
            Assert.AreEqual(expected, abs1b);
            Assert.AreEqual(expected, abs2);
            Assert.AreEqual(expected, abs2b);
        }

        [TestMethod]
        public void TestRemoveRedundantSlashes()
        {
            var dirty = "http://www.contoso.com//something/foo///bar.html/";
            string expected = "http://www.contoso.com/something/foo/bar.html/";
            var cleaned = dirty.RemoveRedundantSlashesInUrl();
            Assert.AreEqual(expected, cleaned);
        }
    }
}

