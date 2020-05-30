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
            string pageText = File.ReadAllText(@"Data\BeatSaverListPage.json");
            Uri uri = null;
            var songs = BeatSaverReader.ParseSongsFromPage(pageText, uri, true);
            Assert.IsTrue(songs.Count == 10);
            foreach (var song in songs)
            {
                Assert.IsFalse(song.DownloadUri == null);
                Assert.IsFalse(string.IsNullOrEmpty(song.Hash));
                Assert.IsFalse(string.IsNullOrEmpty(song.LevelAuthorName));
                Assert.IsFalse(string.IsNullOrEmpty(song.RawData));
                Assert.IsFalse(string.IsNullOrEmpty(song.Name));
            }
            var firstSong = JObject.Parse(songs.First().RawData);
            string firstHash = firstSong["hash"]?.Value<string>();
            Assert.IsTrue(firstHash == "25170877f7b500369be0c2d1ffbdc8c6d1ad4227");
            string firstUploader = firstSong["uploader"]?["username"]?.Value<string>();
            Assert.IsTrue(firstUploader == "z-anesaber");
        }
    }
}
