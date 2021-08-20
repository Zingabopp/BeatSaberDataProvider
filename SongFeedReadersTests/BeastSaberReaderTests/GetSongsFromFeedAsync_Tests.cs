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

namespace SongFeedReadersTests.BeastSaberReaderTests
{
    [TestClass]
    public class GetSongsFromFeedAsync_Tests
    {
        static GetSongsFromFeedAsync_Tests()
        {
            TestSetup.Initialize();
        }

        private int DefaultMaxConcurrency = 2;

        [TestMethod]
        public void Bookmarks_UnlimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Bookmarks) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void Bookmarks_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency);
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Bookmarks) { MaxSongs = maxSongs };
            var songList = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(songList.Count > 0);
            Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(songList.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void Followings_UnlimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Following) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(result.Count != 0);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(result.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void Followings_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.Following) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(result.Count == maxSongs);
            //Assert.IsFalse(songList.Count > maxSongs);
            Assert.IsFalse(result.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
        }

        [TestMethod]
        public void CuratorRecommended_UnlimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 0;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.CuratorRecommended) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(result.Count != 0);
            Assert.IsFalse(result.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(result.Songs.Any(s => s.Value.DownloadUri == null));
            Assert.IsTrue(result.PagesChecked >= 26);
            var firstSong = result.Songs.First().Value;
            var firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["hash"]?.Value<string>().ToUpper() == firstSong.Hash);
        }

        [TestMethod]
        public void CuratorRecommended_LimitedSongs()
        {
            var reader = new BeastSaberReader("Zingabopp", DefaultMaxConcurrency) { StoreRawData = true };
            int maxSongs = 60;
            var settings = new BeastSaberFeedSettings((int)BeastSaberFeedName.CuratorRecommended) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeedAsync(settings).Result;
            Assert.IsTrue(result.Count == maxSongs);
            Assert.IsFalse(result.Songs.Any(s => string.IsNullOrEmpty(s.Key)));
            Assert.IsFalse(result.Songs.Any(s => s.Value.DownloadUri == null));
            try
            {
                Assert.AreEqual(2, result.PagesChecked);
            }
            catch (AssertFailedException)
            {
                for(int i = 0; i < result.PageResults.Count(); i++)
                {
                    var page = result.PageResults[i];
                    Console.WriteLine($"Page {i + 1}: {page.Count} songs. {page.Uri}");
                }
                Assert.AreEqual(3, result.PagesChecked);
            }
            var firstSong = result.Songs.First().Value;
            Assert.IsFalse(string.IsNullOrEmpty(firstSong.RawData));
            var firstRawData = JToken.Parse(firstSong.RawData);
            Assert.IsTrue(firstRawData["hash"]?.Value<string>().ToUpper() == firstSong.Hash);
        }
    }
}
