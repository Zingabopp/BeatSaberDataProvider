using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using WebUtilities;
using System.Threading.Tasks;
using SongFeedReaders.Readers;

namespace SongFeedReadersTests
{
    [TestClass]
    public class BeastSaberReaderTests
    {
        static BeastSaberReaderTests()
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
        public void LargeFile()
        {
            var uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            //WebUtils.WebClient.Timeout = 500;
            try
            {
                try
                {
                    var task = GetResponseSafeAsync(uri).Result;
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }

            }
            catch (WebClientException ex)
            {
                Console.WriteLine($"WebClientException\n{ex}");
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception\n{ex}");
            }
        }

        [TestMethod]
        public void SingleRequest()
        {
            var uri = new Uri("https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=2&count=50");
            //WebUtils.WebClient.Timeout = 500;
            try
            {
                try
                {
                    var task = GetResponseSafeAsync(uri).Result;
                }catch(AggregateException ex)
                {
                    throw ex.InnerException;
                }
                
            }
            catch (WebClientException ex)
            {
                Console.WriteLine($"WebClientException\n{ex}");
            }
            catch(AssertFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception\n{ex}");
            }
        }

        [TestMethod]
        public void GetSongsFromPage_XML_Test()
        {

            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            var text = File.ReadAllText("Data\\BeastSaberXMLPage.xml");
            Uri uri = null;
            var songList = reader.GetSongsFromPageText(text, uri, BeastSaberReader.ContentType.XML);
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
        public void GetSongsFromPage_JSON_Test()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            var text = File.ReadAllText("Data\\BeastSaberJsonPage.json");
            Uri uri = null;
            var songList = reader.GetSongsFromPageText(text, uri, BeastSaberReader.ContentType.JSON);
            Assert.IsTrue(songList.Count == 20);
            var firstHash = "a3bbbe2d6f64dfe8324c7098d5c35281d21fd20f".ToUpper();
            var firstUrl = "https://beatsaver.com/api/download/key/5679";
            Assert.IsTrue(songList.First().Hash == firstHash);
            Assert.IsTrue(songList.First().DownloadUri.ToString() == firstUrl);
            var lastHash = "20b9326bd71db4454aba08df06b035ea536322a9".ToUpper();
            var lastUrl = "https://beatsaver.com/api/download/key/55d1";
            Assert.IsTrue(songList.Last().Hash == lastHash);
            Assert.IsTrue(songList.Last().DownloadUri.ToString() == lastUrl);
            Assert.IsFalse(songList.Any(s => string.IsNullOrEmpty(s.Hash)));
            Assert.IsFalse(songList.Any(s => s.DownloadUri == null));
        }

        [TestMethod]
        public void GetSongsFromFeed_Bookmarks()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Bookmarks) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeed_Followings_SinglePage()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 150;
            int maxPages = 1;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Following)
            {
                MaxSongs = maxSongs,
                MaxPages = maxPages
            };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count > 0);
            Assert.IsTrue(songList.Count <= 100);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeed_Followings_SinglePage_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 20;
            int maxPages = 1;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Following)
            {
                MaxSongs = maxSongs,
                MaxPages = maxPages
            };
            var songList = reader.GetSongsFromFeed(settings);
            var pagesChecked = songList.Songs.Values.GroupBy(s => s.SourceUri);
            Assert.IsTrue(pagesChecked.Count() == 1);
            Assert.IsTrue(songList.Count > 0);
            Assert.IsTrue(songList.Count <= 20);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeed_Followings()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Following) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }



        [TestMethod]
        public void GetSongsFromFeedAsync_Bookmarks_UnlimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Bookmarks) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Bookmarks_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Bookmarks) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Followings_UnlimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Following) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count != 0);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_Followings_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.Following) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_CuratorRecommended_UnlimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.CuratorRecommended) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count != 0);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Songs.Any(s => s.Value.DownloadUri == null));
            var firstSong = songList.Songs.First().Value;
            var firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["hash"]?.Value<string>().ToUpper() == firstSong.Hash);
        }

        [TestMethod]
        public void GetSongsFromFeedAsync_CuratorRecommended_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.CuratorRecommended) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Songs.Any(s => s.Value.DownloadUri == null));
            var firstSong = songList.Songs.First().Value;
            var firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["hash"]?.Value<string>().ToUpper() == firstSong.Hash);
        }

        [TestMethod]
        public void GetSongsFromFeed_CuratorRecommended_LimitedPages()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 30;
            int maxPages = 2;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeed.CuratorRecommended) { MaxPages = maxPages, MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(songList.Songs.Any(s => s.Value.DownloadUri == null));
        }
    }
}
