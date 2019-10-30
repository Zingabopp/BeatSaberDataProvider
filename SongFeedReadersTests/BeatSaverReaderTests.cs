using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using SongFeedReaders.Readers;

namespace SongFeedReadersTests
{
    [TestClass]
    public class BeatSaverReaderTests
    {
        static BeatSaverReaderTests()
        {
            TestSetup.Initialize();
        }
        #region Web
        //[TestMethod]
        //public void GetSongsFromFeed_ObsoleteAuthors_Test()
        //{
        //    var maxSongs = 26;
        //    var reader = new BeatSaverReader() { StoreRawData = true };
        //    var authorList = new string[] { "BlackBlazon", "greatyazer" };
        //    var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Author) { Authors = authorList, MaxSongs = maxSongs };
        //    var songsByAuthor = reader.GetSongsFromFeed(settings);
        //    var detectedAuthors = songsByAuthor.Songs.Values.Select(s => s.MapperName.ToLower()).Distinct();
        //    foreach (var song in songsByAuthor.Songs)
        //    {
        //        Assert.IsTrue(song.Value.DownloadUri != null);
        //        Assert.IsTrue(authorList.Any(a => a.ToLower() == song.Value.MapperName.ToLower()));
        //    }
        //    foreach (var author in authorList)
        //    {
        //        Assert.IsTrue(songsByAuthor.Songs.Any(s => s.Value.MapperName.ToLower() == author.ToLower()));
        //    }

        //    var authorGroups = songsByAuthor.Songs.Values.GroupBy(s => s.MapperName);
        //    foreach(var author in authorGroups)
        //    {
        //        Assert.IsTrue(author.Count() <= maxSongs);
        //        Assert.IsTrue(author.Count() > 0);
        //    }


        //    // BlackBlazon check
        //    var blazonHash = "58de2d709a45b68fdb1dbbfefb187f59f629bfc5".ToUpper();
        //    var blazonSong = songsByAuthor.Songs[blazonHash];
        //    Assert.IsTrue(blazonSong != null);
        //    Assert.IsTrue(blazonSong.DownloadUri != null);
        //    // GreatYazer check
        //    var songHash = "bf8c016dc6b9832ece3030f05277bbbe67db790d".ToUpper();
        //    var yazerSong = songsByAuthor.Songs[songHash];
        //    Assert.IsTrue(yazerSong != null);
        //    Assert.IsTrue(yazerSong.DownloadUri != null);
        //}

        [TestMethod]
        public void GetSongsFromFeed_Authors_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var authorList = new string[] { "BlackBlazon", "greatyazer", "joetastic" };
            var songList = new Dictionary<string, ScrapedSong>();
            foreach (var author in authorList)
            {
                var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Author) { Criteria = author, MaxSongs = 59 };
                var songsByAuthor = reader.GetSongsFromFeed(settings);
                Assert.IsTrue(songsByAuthor.Count > 0);
                Assert.IsTrue(songsByAuthor.Count <= 59);
                var expectedPages = (int)Math.Ceiling((double)songsByAuthor.Count / BeatSaverReader.SongsPerPage);
                Assert.IsTrue(expectedPages <= songsByAuthor.PagesChecked);
                foreach (var song in songsByAuthor.Songs)
                {
                    songList.TryAdd(song.Key, song.Value);
                }
            }
            var detectedAuthors = songList.Values.Select(s => s.MapperName.ToLower()).Distinct();
            foreach (var song in songList)
            {
                Assert.IsTrue(song.Value.DownloadUri != null);
                Assert.IsTrue(authorList.Any(a => a.ToLower() == song.Value.MapperName.ToLower()));
            }
            foreach (var author in authorList)
            {
                Assert.IsTrue(songList.Any(s => s.Value.MapperName.ToLower() == author.ToLower()));
            }

            // BlackBlazon check
            var blazonHash = "58de2d709a45b68fdb1dbbfefb187f59f629bfc5".ToUpper();
            var blazonSong = songList[blazonHash];
            Assert.IsTrue(blazonSong != null);
            Assert.IsTrue(blazonSong.DownloadUri != null);
            // GreatYazer check
            var songHash = "bf8c016dc6b9832ece3030f05277bbbe67db790d".ToUpper();
            var yazerSong = songList[songHash];
            Assert.IsTrue(yazerSong != null);
            Assert.IsTrue(yazerSong.DownloadUri != null);
        }

        [TestMethod]
        public void GetSongsFromFeed_Newest_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Latest) { MaxSongs = 55 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(settings.MaxSongs, songList.Count);
            foreach (var song in songList.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void GetSongsFromFeed_Hot_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Hot) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == settings.MaxSongs);
            foreach (var song in songList.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        [TestMethod]
        public void GetSongsFromFeed_Plays_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Plays) { MaxSongs = 50 };
            var songList = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(songList.Count == settings.MaxSongs);
            foreach (var song in songList.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        [TestMethod]
        public void GetSongsFromFeed_Downloads_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Downloads) { MaxSongs = 50 };
            var result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count == settings.MaxSongs);
            Assert.AreEqual(5, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void GetSongsFromFeed_Search_Test()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeed.Search) { MaxSongs = 50, Criteria = "Believer" };
            var result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count > 0);
            Assert.AreEqual(0, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        #endregion

        [TestMethod]
        public void ParseSongsFromPage_Test()
        {
            string pageText = File.ReadAllText(@"Data\BeatSaverListPage.json");
            Uri uri = null;
            var songs = BeatSaverReader.ParseSongsFromPage(pageText, uri);
            Assert.IsTrue(songs.Count == 10);
            foreach (var song in songs)
            {
                Assert.IsFalse(song.DownloadUri == null);
                Assert.IsFalse(string.IsNullOrEmpty(song.Hash));
                Assert.IsFalse(string.IsNullOrEmpty(song.MapperName));
                Assert.IsFalse(string.IsNullOrEmpty(song.RawData));
                Assert.IsFalse(string.IsNullOrEmpty(song.SongName));
            }
            var firstSong = JObject.Parse(songs.First().RawData);
            string firstHash = firstSong["hash"]?.Value<string>();
            Assert.IsTrue(firstHash == "27639680f92a9588b7cce843fc7aaa0f5dc720f8");
            string firstUploader = firstSong["uploader"]?["username"]?.Value<string>();
            Assert.IsTrue(firstUploader == "latte");
        }
    }
}
