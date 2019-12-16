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
using SongFeedReaders.Readers.BeastSaber;
using SongFeedReaders.Readers.BeatSaver;
using System.Threading;

namespace SongFeedReadersTests.BeastSaberReaderTests
{
    [TestClass]
    public class GetSongsFromFeed_Tests
    {
        static GetSongsFromFeed_Tests()
        {
            TestSetup.Initialize();
        }

        private int DefaultMaxConcurrency = 2;

        private IFeedReader GetDefaultReader() { return new BeastSaberReader("Zingabopp", DefaultMaxConcurrency); }

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
        public void Bookmarks_LimitedSongs()
        {
            var reader = GetDefaultReader();
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Bookmarks) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void Followings_SinglePage()
        {
            var reader = GetDefaultReader();
            int maxSongs = 150;
            int maxPages = 1;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Following)
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
        public void Followings_SinglePage_LimitedSongs()
        {
            var reader = GetDefaultReader();
            reader.StoreRawData = true;
            int maxSongs = 20;
            int maxPages = 1;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Following)
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
        public void Followings_LimitedSongs()
        {
            var reader = GetDefaultReader();
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Following) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void Followings_Exception_WrongSettingsType()
        {
            var reader = GetDefaultReader();
            int maxSongs = 60;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Author) { MaxSongs = maxSongs };
            Assert.ThrowsException<InvalidCastException>(() => reader.GetSongsFromFeed(settings));
        }

        [TestMethod]
        public void Followings_Exception_NullSettings()
        {
            var reader = GetDefaultReader();
            BeastSaberFeedSettings settings = null;
            Assert.ThrowsException<ArgumentNullException>(() => reader.GetSongsFromFeed(settings));
        }

        [TestMethod]
        public void Followings_OperationCanceled()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            IFeedReader reader = GetDefaultReader();
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Following) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeed(settings, cts.Token);
            Assert.IsFalse(result.Successful);
            Assert.AreEqual(FeedResultError.Cancelled, result.ErrorCode);
            cts.Dispose();
        }

        [TestMethod]
        public void CuratorRecommended_LimitedPages()
        {
            var reader = GetDefaultReader();
            int maxSongs = 55;
            int maxPages = 2;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.CuratorRecommended) { MaxPages = maxPages, MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count == maxSongs);
            Assert.IsFalse(result.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(result.Songs.Any(s => s.Value.DownloadUri == null));
            Assert.AreEqual(2, result.PagesChecked);
        }
    }
}
