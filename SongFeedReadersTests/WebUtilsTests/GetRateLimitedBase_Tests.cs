using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static SongFeedReaders.WebUtils;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReadersTests.WebUtilsTests
{
    [TestClass]
    public class GetRateLimitedBase_Tests
    {
        static GetRateLimitedBase_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void AdvancedSearch()
        {
            var expectedBaseUrl = "https://beatsaver.com/api/search/advanced";
            var url = new Uri(@"https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND metadata.songName:*e*");
            var baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND metadata.songName:e");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/search/advanced?q=metadata.songName:bomb");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void TextSearch()
        {
            string expectedBaseUrl;
            Uri url;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/search/text";
            url = new Uri(@"https://beatsaver.com/api/search/text/0?q=ruckus");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void Latest()
        {
            string expectedBaseUrl;
            Uri url;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/maps/latest";
            url = new Uri(@"https://beatsaver.com/api/maps/latest/1414");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/maps/latest/0");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/maps/latest");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void Downloads()
        {
            string expectedBaseUrl;
            Uri url;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/maps/downloads";
            url = new Uri(@"https://beatsaver.com/api/maps/downloads/1414");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/maps/downloads/0");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/maps/downloads");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void Detail()
        {
            string expectedBaseUrl;
            Uri url;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/maps/detail/5317";
            url = new Uri(@"https://beatsaver.com/api/maps/detail/5317");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/maps/downloads/b54f");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreNotEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void Uploader()
        {
            string expectedBaseUrl;
            Uri uri;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/maps/uploader/5cff0b7398cc5a672c84f6cc";
            uri = new Uri(@"https://beatsaver.com/api/maps/uploader/5cff0b7398cc5a672c84f6cc/2");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            uri = new Uri(@"https://beatsaver.com/api/maps/uploader/5cff0b7398cc5a672c84f6cc");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            uri = new Uri(@"https://beatsaver.com/api/maps/uploader/5cff0b7398cc5a672cdfdc/2");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreNotEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void UsersFind()
        {
            string expectedBaseUrl;
            Uri uri;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/users/find";
            uri = new Uri(@"https://beatsaver.com/api/users/find/5cff0b7398cc5a672c84f6cc");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);

        }

        [TestMethod]
        public void UsersMe()
        {
            string expectedBaseUrl;
            Uri uri;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/users/me";
            uri = new Uri(@"https://beatsaver.com/api/users/me");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void Stats()
        {
            string expectedBaseUrl;
            Uri url;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/stats";
            url = new Uri(@"https://beatsaver.com/api/stats/by-hash/aa791c6e9180b34c7eea635c6972d4d03c548783");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
            url = new Uri(@"https://beatsaver.com/api/stats/key/15c3");
            baseUrl = GetRateLimitedBase(url);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void Vote()
        {
            string expectedBaseUrl;
            Uri uri;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/vote";
            uri = new Uri(@"https://beatsaver.com/api/vote/steam/348f");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void DownloadByKey()
        {
            string expectedBaseUrl;
            Uri uri;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/download/key/b";
            uri = new Uri(@"https://beatsaver.com/api/download/key/b");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void DownloadByHash()
        {
            string expectedBaseUrl;
            Uri uri;
            string baseUrl;
            expectedBaseUrl = "https://beatsaver.com/api/download/hash/da8d7f78da8f7db";
            uri = new Uri(@"https://beatsaver.com/api/download/hash/da8d7f78da8f7db");
            baseUrl = GetRateLimitedBase(uri);
            Assert.AreEqual(expectedBaseUrl, baseUrl);
        }

        [TestMethod]
        public void EmptyUrl()
        {
        
            var result = GetRateLimitedBase(string.Empty);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void NullUrl()
        {
            string nullStr = null;
            var result = GetRateLimitedBase(nullStr);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void NoMatchingRoute()
        {
            Uri uri = new Uri("https://beatsaver.com/api/nothinghere");
            var result = GetRateLimitedBase(uri);
            Assert.AreEqual(string.Empty, result);
        }

    }
}
