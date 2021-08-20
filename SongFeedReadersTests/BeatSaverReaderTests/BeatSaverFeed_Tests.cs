using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeatSaver;
using System;
using System.Collections.Generic;
using System.Text;

namespace SongFeedReadersTests.BeatSaverReaderTests
{
    [TestClass]
    public class BeatSaverFeed_Tests
    {
        [TestMethod]
        public void GetUriForDate_Before()
        {
            string dateStr = "2021-08-02T20:36:22.804Z";
            DateTime.Parse(dateStr);
            DateTime dateTime = DateTime.Parse(dateStr);
            FeedDate feedDate = new FeedDate(dateTime, DateDirection.Before);
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings(BeatSaverFeedName.Latest);
            BeatSaverFeed feed = new BeatSaverFeed(settings);
            string expectedUrl = $"https://api.beatsaver.com/maps/latest?automapper=false&before={dateStr}";

            string actualUrl = feed.GetUriForDate(feedDate).ToString();

            Assert.AreEqual(expectedUrl, actualUrl);
        }
    }
}
