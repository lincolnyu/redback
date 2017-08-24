using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net;

namespace RedbackTests
{
    [TestClass]
    public class DownloadTest
    {
        [TestMethod]
        public void DownloadHttpsUsingHttpWeb()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://w3schools.com/html/");
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705)";
            var response = (HttpWebResponse)request.GetResponse();
            var str = new MemoryStream();
            var sr1 = new StreamReader(response.GetResponseStream());

            var request2 = (HttpWebRequest)WebRequest.Create("http://w3schools.com/html/");
            request2.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705)";
            var response2 = (HttpWebResponse)request.GetResponse();
            response2.GetResponseStream();
            var sr2 = new StreamReader(response.GetResponseStream());

            var s1 = sr1.ReadToEnd();
            var s2 = sr2.ReadToEnd();

            Assert.IsTrue(s1.Length > 3000);
            Assert.AreEqual("", s2);
        }
    }
}
