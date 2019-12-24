using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using WebUtilities;
using System.Threading.Tasks;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.ScoreSaber;
using System.Threading;

namespace SongFeedReadersTests.ScoreSaberReaderTests
{
    [TestClass]
    public class ValidFeedSettings_Tests
    {
        [TestMethod]
        public void NullSettings()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new ScoreSaberFeed(null));
            Console.WriteLine(ex.Message);
        }

        [TestMethod]
        public void QuerylessFeeds()
        {
            var querylessFeeds = new ScoreSaberFeedName[] { ScoreSaberFeedName.LatestRanked, ScoreSaberFeedName.TopPlayed, ScoreSaberFeedName.TopRanked, ScoreSaberFeedName.Trending };
            foreach (var feedType in querylessFeeds)
            {
                var settings = new ScoreSaberFeedSettings(feedType);
                var feed = new ScoreSaberFeed(settings);
                feed.EnsureValidSettings();
                Assert.IsTrue(feed.HasValidSettings);
            }
        }

        [TestMethod]
        public void UnusedQuery()
        {
            var querylessFeeds = new ScoreSaberFeedName[] { ScoreSaberFeedName.LatestRanked, ScoreSaberFeedName.TopPlayed, ScoreSaberFeedName.TopRanked, ScoreSaberFeedName.Trending };
            foreach (var feedType in querylessFeeds)
            {
                var settings = new ScoreSaberFeedSettings(feedType);
                settings.SearchQuery = "test";
                var feed = new ScoreSaberFeed(settings);
                feed.EnsureValidSettings();
                Assert.IsTrue(feed.HasValidSettings);
            }
        }

        [TestMethod]
        public void Search_HasQuery()
        {
            var settings = new ScoreSaberFeedSettings(ScoreSaberFeedName.Search);
            settings.SearchQuery = "test";
            var feed = new ScoreSaberFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Search_NullQuery()
        {
            var settings = new ScoreSaberFeedSettings(ScoreSaberFeedName.Search);
            var feed = new ScoreSaberFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Search_EmptyCriteria()
        {
            var settings = new ScoreSaberFeedSettings(ScoreSaberFeedName.Search);
            settings.SearchQuery = "";
            var feed = new ScoreSaberFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }
    }
}
