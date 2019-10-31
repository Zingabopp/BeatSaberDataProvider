using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using WebUtilities.WebWrapper;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace WebUtilitiesTests.IWebClientTests
{
    [TestClass]
    public class UserAgent_Tests
    {
        private static readonly string TestOutputPath = Path.GetFullPath(@"Output\IWebClientTests");

        [TestMethod]
        public void DefaultUserAgents()
        {
            var client1 = new WebClientWrapper();
            var client2 = new HttpClientWrapper();
            string expectedUserAgent = null;
            Assert.AreEqual(expectedUserAgent, client1.UserAgent);
            Assert.AreEqual(client1.UserAgent, client2.UserAgent);
            Console.WriteLine($"UserAgent: \"{client1.UserAgent}\"");
        }

        [TestMethod]
        public void SetOnce()
        {
            var client1 = new WebClientWrapper();
            var client2 = new HttpClientWrapper();
            string expectedUserAgent = "UserAgentTest/1.0";

            client1.SetUserAgent(expectedUserAgent);
            client2.SetUserAgent(expectedUserAgent);

            Assert.AreEqual(expectedUserAgent, client1.UserAgent);
            Assert.AreEqual(expectedUserAgent, client2.UserAgent);
            Console.WriteLine($"UserAgent: \"{client1.UserAgent}\"");
        }
        
        [TestMethod]
        public void SetTwice()
        {
            var client1 = new WebClientWrapper();
            var client2 = new HttpClientWrapper();
            string wrongUserAgent = "WrongAgent/1.0";
            string expectedUserAgent = "UserAgentTest/1.0";

            client1.SetUserAgent(wrongUserAgent);
            client2.SetUserAgent(wrongUserAgent);
            Assert.AreEqual(wrongUserAgent, client1.UserAgent);
            Assert.AreEqual(wrongUserAgent, client2.UserAgent);

            client1.SetUserAgent(expectedUserAgent);
            client2.SetUserAgent(expectedUserAgent);

            Assert.AreEqual(expectedUserAgent, client1.UserAgent);
            Assert.AreEqual(expectedUserAgent, client2.UserAgent);
            Console.WriteLine($"UserAgent: \"{client1.UserAgent}\"");
        }

    }
}
