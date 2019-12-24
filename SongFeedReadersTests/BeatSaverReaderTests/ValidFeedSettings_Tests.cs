using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using WebUtilities;
using System.Threading.Tasks;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeatSaver;
using System.Threading;

namespace SongFeedReadersTests.BeatSaverReaderTests
{
    [TestClass]
    public class ValidFeedSettings_Tests
    {
        [TestMethod]
        public void NullSettings()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new BeatSaverFeed(null));
            Console.WriteLine(ex.Message);
        }

        [TestMethod]
        public void Downloads()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Downloads);
            var feed = new BeatSaverFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Hot()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Hot);
            var feed = new BeatSaverFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Plays()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Plays);
            var feed = new BeatSaverFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Latest()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Latest);
            var feed = new BeatSaverFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void UnusedQuery()
        {
            var querylessFeeds = new BeatSaverFeedName[] { BeatSaverFeedName.Downloads, BeatSaverFeedName.Hot, BeatSaverFeedName.Latest, BeatSaverFeedName.Plays };
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.all, "test");
            foreach (var feedType in querylessFeeds)
            {
                var settings = new BeatSaverFeedSettings(feedType);
                settings.SearchQuery = queryBuilder.GetQuery();
                var feed = new BeatSaverFeed(settings);
                feed.EnsureValidSettings();
                Assert.IsTrue(feed.HasValidSettings);
            }
        }

        [TestMethod]
        public void Search_HasQuery()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Search);
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.all, "test");
            settings.SearchQuery = queryBuilder.GetQuery();
            var feed = new BeatSaverFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Search_NullQuery()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Search);
            var feed = new BeatSaverFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Search_EmptyCriteria()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Search);
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.all, "");
            settings.SearchQuery = queryBuilder.GetQuery();
            var feed = new BeatSaverFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Author_HasQuery()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Author);
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.author, "test");
            settings.SearchQuery = queryBuilder.GetQuery();
            var feed = new BeatSaverFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Author_WrongSearchType()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Author);
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.all, "test");
            settings.SearchQuery = queryBuilder.GetQuery();
            var feed = new BeatSaverFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Author_EmptyCriteria()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Author);
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.author, "");
            settings.SearchQuery = queryBuilder.GetQuery();
            var feed = new BeatSaverFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Author_NullQuery()
        {
            var settings = new BeatSaverFeedSettings(BeatSaverFeedName.Author);
            var feed = new BeatSaverFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

    }
}
