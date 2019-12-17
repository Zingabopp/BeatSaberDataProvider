using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using WebUtilities;
using System.Threading.Tasks;
using SongFeedReaders.Readers.BeastSaber;

namespace SongFeedReadersTests.BeastSaberReaderTests
{
    [TestClass]
    public class GetSongsFromPage_Tests
    {
        static GetSongsFromPage_Tests()
        {
            TestSetup.Initialize();
        }

        private int DefaultMaxConcurrency = 2;

        private async Task<IWebResponseMessage> GetResponseSafeAsync(Uri uri)
        {
            try
            {
                using (var response = await WebUtils.WebClient.GetAsync(uri, 0).ConfigureAwait(false))
                {
                    await Task.Delay(10000);
                    return response;
                }
            }
            catch (WebClientException ex)
            {
                Console.WriteLine($"WebClientException\n{ex}");
                Assert.AreEqual(408, ex.Response?.StatusCode ?? 0);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception\n{ex}");
                Assert.Fail("Wrong exception");
                return null;
            }
        }

        [TestMethod]
        public void Success_XML()
        {
            //var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            var feedSettings = new BeastSaberFeedSettings(BeastSaberFeedName.Following, "Zingabopp");
            var feed = new BeastSaberFeed(feedSettings);
            var text = File.ReadAllText("Data\\BeastSaberXMLPage.xml");
            Uri uri = null;
            var songList = BeastSaberFeed.GetSongsFromPageText(text, uri, ContentType.XML, true);
            Assert.IsTrue(songList.Count == 50);
            var firstHash = "74575254ae759f3f836eb521b4b80093ca52cd3d".ToUpper();
            var firstKey = "56ff";
            var firstLevelAuthor = "Rustic";
            var firstTitle = "Xilent – Code Blood";
            var firstDownloadUrl = "https://beatsaver.com/api/download/key/56ff";
            var firstUrl = "https://beatsaver.com/api/download/key/56ff";
            var firstSong = songList.First();
            Assert.IsTrue(firstSong.Hash == firstHash);
            Assert.IsTrue(firstSong.DownloadUri.ToString() == firstUrl);
            // Raw Data test
            JToken firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["Hash"]?.Value<string>().ToUpper() == firstHash);
            Assert.IsTrue(firstRawData["SongKey"]?.Value<string>() == firstKey);
            Assert.IsTrue(firstRawData["LevelAuthorName"]?.Value<string>() == firstLevelAuthor);
            Assert.IsTrue(firstRawData["SongTitle"]?.Value<string>() == firstTitle);
            Assert.IsTrue(firstRawData["DownloadURL"]?.Value<string>() == firstDownloadUrl);



            var lastHash = "e3487474b70d969927e459a1590e93b7ad25a436".ToUpper();
            var lastUrl = "https://beatsaver.com/api/download/key/5585";
            Assert.IsTrue(songList.Last().Hash == lastHash);
            Assert.IsTrue(songList.Last().DownloadUri.ToString() == lastUrl);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Hash)));
            Assert.IsFalse(songList.Any(s => s.DownloadUri == null));
        }

        [TestMethod]
        public void Success_JSON()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            var text = File.ReadAllText("Data\\BeastSaberJsonPage.json");
            Uri uri = null;
            var songList = BeastSaberFeed.GetSongsFromPageText(text, uri, ContentType.JSON, true);
            Assert.IsTrue(songList.Count == 20);
            var firstHash = "a3bbbe2d6f64dfe8324c7098d5c35281d21fd20f".ToUpper();
            var firstUrl = "http://beatsaver.com/api/download/hash/a3bbbe2d6f64dfe8324c7098d5c35281d21fd20f";
            Assert.IsTrue(songList.First().Hash == firstHash);
            Assert.AreEqual(firstUrl, songList.First().DownloadUri.ToString());
            var lastHash = "20b9326bd71db4454aba08df06b035ea536322a9".ToUpper();
            var lastUrl = "http://beatsaver.com/api/download/hash/20b9326bd71db4454aba08df06b035ea536322a9";
            Assert.IsTrue(songList.Last().Hash == lastHash);
            Assert.IsTrue(songList.Last().DownloadUri.ToString() == lastUrl);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Hash)));
            Assert.IsFalse(songList.Any(s => s.DownloadUri == null));
        }
    }
}
