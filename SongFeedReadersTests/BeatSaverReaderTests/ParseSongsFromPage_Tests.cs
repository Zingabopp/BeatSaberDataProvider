using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using SongFeedReaders.Readers.BeatSaver;

namespace SongFeedReadersTests.BeatSaverReaderTests
{
    [TestClass]
    public class ParseSongsFromPage_Tests
    {
        static ParseSongsFromPage_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void Success()
        {
            string path = Path.Combine("Data", "NewBeatSaver", "List", "Latest", "Latest_0.json");
            string pageText = File.ReadAllText(path);
            Uri uri = null;
            var songs = BeatSaverReader.ParseSongsFromPage(pageText, uri, true);
            Assert.AreEqual(20, songs.Count);
            foreach (var song in songs)
            {
                Assert.IsFalse(song.DownloadUri == null);
                Assert.IsFalse(string.IsNullOrEmpty(song.Hash));
                Assert.IsFalse(string.IsNullOrEmpty(song.LevelAuthorName));
                Assert.IsFalse(string.IsNullOrEmpty(song.RawData));
                Assert.IsFalse(string.IsNullOrEmpty(song.Name));
            }
            var firstSong = JObject.Parse(songs.First().RawData);
            string firstHash = firstSong["versions"].First()["hash"]?.Value<string>();
            Assert.AreEqual("e2512e6fbf85059d9fd9b429f62b2e618dd4d7e9", firstHash);
            string firstUploader = firstSong["uploader"]?["name"]?.Value<string>();
            Assert.AreEqual("itzrimuru", firstUploader);
        }
    }
}
