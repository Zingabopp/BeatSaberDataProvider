﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SongFeedReaders.Services;
using SongFeedReaders;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using System.IO;
using SongFeedReaders.Data;

namespace SongFeedReadersTests.SongInfoProviderTests
{
    [TestClass]
    public class BeatSaverProvider_Tests
    {
        static BeatSaverProvider_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public async Task BeatSaverProviderTest()
        {
            
            var manager = new SongInfoManager();
            SongFeedReaders.Logging.LoggingController.DefaultLogger = new SongFeedReaders.Logging.FeedReaderLogger();
            var beatSaver = new BeatSaverSongInfoProvider();
            manager.AddProvider(beatSaver);
            var response = await manager.GetSongByKeyAsync("b");
            Assert.IsTrue(response.Success);
            ScrapedSong song = response.Song ?? throw new AssertFailedException("Song is null");
            Assert.AreEqual("B", song.Key);
            Assert.AreEqual("19F2879D11A91B51A5C090D63471C3E8D9B7AEE3", song.Hash);
            Assert.AreEqual("rustic", song.LevelAuthorName);
            Assert.AreEqual("Believer", song.Name);
        }
    }
}
