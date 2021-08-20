using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using WebUtilities;
using System.Threading.Tasks;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;
using System.Threading;

namespace SongFeedReadersTests.BeastSaberReaderTests
{
    [TestClass]
    public class ValidFeedSettings_Tests
    {
        [TestMethod]
        public void NullSettings()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new BeastSaberFeed(null));
            Console.WriteLine(ex.Message);
        }

        [TestMethod]
        public void CuratorRecommended_NoUsername()
        {
            var settings = new BeastSaberFeedSettings(BeastSaberFeedName.CuratorRecommended);
            var feed = new BeastSaberFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void CuratorRecommended_Username()
        {
            var settings = new BeastSaberFeedSettings(BeastSaberFeedName.CuratorRecommended, "TestUser");
            var feed = new BeastSaberFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Bookmarks_NoUsername()
        {
            var settings = new BeastSaberFeedSettings(BeastSaberFeedName.Bookmarks);
            var feed = new BeastSaberFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Bookmarks_Username()
        {
            var settings = new BeastSaberFeedSettings(BeastSaberFeedName.Bookmarks, "TestUser");
            var feed = new BeastSaberFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }

        [TestMethod]
        public void Following_NoUsername()
        {
            var settings = new BeastSaberFeedSettings(BeastSaberFeedName.Following);
            var feed = new BeastSaberFeed(settings);
            var ex = Assert.ThrowsException<InvalidFeedSettingsException>(() => feed.EnsureValidSettings());
            Console.WriteLine(ex.Message);
            Assert.IsFalse(feed.HasValidSettings);
        }

        [TestMethod]
        public void Following_Username()
        {
            var settings = new BeastSaberFeedSettings(BeastSaberFeedName.Following, "TestUser");
            var feed = new BeastSaberFeed(settings);
            feed.EnsureValidSettings();
            Assert.IsTrue(feed.HasValidSettings);
        }
    }
}
