using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redback.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        [TestMethod]
        public void TestUrlToFile()
        {
            var url = "https://accounts.google.com/ServiceLogin?service=mail&passive=true&rm=false&continue=https://mail.google.com/mail/?tab%3Dwm&scc=1&ltmpl=default&ltmplcache=2&emr=1&osid=1#";
            const string expectedir = "accounts.google.com";
            const string filePart = "ServiceLogin?service=mail&passive=true&rm=false&continue=https://mail.google.com/mail/?tab%3Dwm&scc=1&ltmpl=default&ltmplcache=2&emr=1&osid=1#";
            string expectedFile = UrlHelper.ValidateFileName(filePart).ToLower();
            url.UrlToFilePath(out string dir, out string file, UrlHelper.ValidateFileName);
            Assert.AreEqual(expectedir, dir);
            Assert.AreEqual(expectedFile, file);
        }

        [TestMethod]
        public void TestUrlToFile2()
        {
            var url = "http://www.google.com.au/intl/en/about/";
            const string expectedir = @"www.google.com.au\intl\en\about";
            const string expectedFile = "";
            const string expectedFileDefault = "index.html";

            url.UrlToFilePath(out string dir, out string file, null);
            Assert.AreEqual(expectedir, dir);
            Assert.AreEqual(expectedFile, file);

            url.UrlToFilePath(out dir, out file, UrlHelper.ValidateFileName);
            Assert.AreEqual(expectedir, dir);
            Assert.AreEqual(expectedFileDefault, file);
        }

        private int SkipWhiteSpace(string s, int start)
        {
            for (; start < s.Length; start++)
            {
                if (!char.IsWhiteSpace(s[start]))
                {
                    break;
                }
            }
            if (start >= s.Length) start = -1;
            return start;
        }

        [TestMethod]
        public void TestWeirdPage()
        {
            using (var sr = new StreamReader(@"Samples\sample1.html"))
            {
                var page = sr.ReadToEnd();
                var lastIndex = 0;

                var list = new List<Tuple<int, string>>();
                var pos = 0;
                for (; ; )
                {
                    pos = page.IndexOf("href", pos);
                    if (pos < 0) break;
                    pos += "href".Length;
                    pos = SkipWhiteSpace(page, pos);
                    if (pos < 0) break;
                    if (page[pos] != '=')
                    {
                        continue;
                    }
                    pos++;
                    list.Add(new Tuple<int, string>(pos, "href"));
                }
                pos = 0;
                for (; ; )
                {
                    pos = page.IndexOf("src", pos);
                    if (pos < 0) break;
                    pos += "src".Length;
                    pos = SkipWhiteSpace(page, pos);
                    if (pos < 0) break;
                    if (page[pos] != '=')
                    {
                        continue;
                    }
                    pos++;
                    list.Add(new Tuple<int, string>(pos, "src"));
                }
                list.Sort((a,b)=>a.Item1.CompareTo(b.Item1));

                var p = 0;
                page.FindAnyParameterAsync(new[] { "href", "src" }, 0, 
                    async (index, parameter)=>
                    {
                        Assert.IsTrue(p < list.Count);
                        Assert.AreEqual(list[p].Item1, index);
                        Assert.AreEqual(list[p].Item2, parameter);
                        p++;

                        var linkEnd = page.GetLink(index, out string link); // one character after closing double quotation mark 

                        if (linkEnd < 0 || link == null)
                        {
                            lastIndex = index;
                            return lastIndex;
                        }


                        Assert.IsTrue(linkEnd >= index);
                        lastIndex = linkEnd;
                        return linkEnd;
                    });
                Assert.AreEqual(list.Count, p);

            }
        }
    }
}

