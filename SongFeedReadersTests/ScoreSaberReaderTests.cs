using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using SongFeedReaders.Readers;

namespace SongFeedReadersTests
{
    [TestClass]
    public class ScoreSaberReaderTests
    {
        static ScoreSaberReaderTests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void GetSongsFromFeed_Trending()
        {
            var reader = new ScoreSaberReader();
            int maxSongs = 100;
            var settings = new ScoreSaberFeedSettings((int)ScoreSaberFeed.Trending) { MaxSongs = maxSongs, SongsPerPage = 40, RankedOnly = true };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Songs.Keys.Any(k => string.IsNullOrEmpty(k)));
        }

        [TestMethod]
        public void GetSongsFromFeed_LatestRanked()
        {
            var reader = new ScoreSaberReader();
            int maxSongs = 50;
            var settings = new ScoreSaberFeedSettings((int)ScoreSaberFeed.LatestRanked) { MaxSongs = maxSongs, SongsPerPage = 40, RankedOnly = true };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Songs.Keys.Any(k => string.IsNullOrEmpty(k)));
        }

        [TestMethod]
        public void GetSongsFromFeed_TopRanked()
        {
            var reader = new ScoreSaberReader();
            int maxSongs = 0;
            var settings = new ScoreSaberFeedSettings((int)ScoreSaberFeed.TopRanked) { MaxSongs = maxSongs, SongsPerPage = 40, RankedOnly = true };
            var songList = reader.GetSongsFromFeed(settings);
            Console.WriteLine($"{songList.Count} songs.");
            Assert.IsTrue(songList.Count >= 367);
            Assert.IsFalse(songList.Songs.Keys.Any(k => string.IsNullOrEmpty(k)));
        }

        [TestMethod]
        public void GetSongsFromFeed_TopPlayed()
        {
            var reader = new ScoreSaberReader();
            int maxSongs = 30;
            var settings = new ScoreSaberFeedSettings((int)ScoreSaberFeed.TopPlayed) { MaxSongs = maxSongs, SongsPerPage = 40, RankedOnly = false };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == maxSongs);
            Assert.IsFalse(songList.Songs.Keys.Any(k => string.IsNullOrEmpty(k)));
        }

        [TestMethod]
        public void GetSongsFromFeed_Search()
        {
            var reader = new ScoreSaberReader();
            int maxSongs = 40;
            var settings = new ScoreSaberFeedSettings((int)ScoreSaberFeed.Search)
            { MaxSongs = maxSongs, SongsPerPage = 40, RankedOnly = true, SearchQuery = "Believer" };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Songs.Values.Any(s => s.SongName.ToLower().Contains("believer")));
            Assert.IsFalse(songList.Songs.Keys.Any(k => string.IsNullOrEmpty(k)));
        }

        [TestMethod]
        public void GetSongsFromPageText()
        {
            var reader = new ScoreSaberReader() { StoreRawData = true };
            var pageText = File.ReadAllText("Data\\ScoreSaberPage.json");
            Uri sourceUri = null;
            var songList = reader.GetSongsFromPageText(pageText, sourceUri);
            Assert.IsTrue(songList.Count == 50);
            var firstHash = "0597F8F7D8E396EBFEF511DC9EC98B69635CE532";
            Assert.IsTrue(songList.Songs.First().Hash == firstHash);
            var firstRawData = JToken.Parse(songList.Songs.First().RawData);
            Assert.IsTrue(firstRawData["uid"]?.Value<int>() == 143199);
            var lastHash = "F369747C6B54914DEAA163AAE85816BA5A8C1845";
            Assert.IsTrue(songList.Songs.Last().Hash == lastHash);
        }

        [TestMethod]
        public void GetSongsFromPageText_Url()
        {
            var reader = new ScoreSaberReader() { StoreRawData = true };
            var pageText = File.ReadAllText("Data\\ScoreSaberPage.json");
            string url = Path.GetFullPath("Data\\ScoreSaberPage.json");
            var songList = reader.GetSongsFromPageText(pageText, url);
            Assert.IsTrue(songList.Count == 50);
            var firstHash = "0597F8F7D8E396EBFEF511DC9EC98B69635CE532";
            Assert.IsTrue(songList.Songs.First().Hash == firstHash);
            var firstRawData = JToken.Parse(songList.Songs.First().RawData);
            Assert.IsTrue(firstRawData["uid"]?.Value<int>() == 143199);
            var lastHash = "F369747C6B54914DEAA163AAE85816BA5A8C1845";
            Assert.IsTrue(songList.Songs.Last().Hash == lastHash);
        }

    }
}
