﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using SongFeedReaders.Readers.BeatSaver;
using System.Threading;

namespace SongFeedReadersTests.BeatSaverReaderTests
{
    [TestClass]
    public class GetSongsFromFeed_Tests
    {
        static GetSongsFromFeed_Tests()
        {
            TestSetup.Initialize();
        }
        #region Web
        [TestMethod]
        public void Success_Authors_LimitedSongs()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            var authorList = new string[] { "BlackBlazon", "greatyazer", "joetastic" };
            var songList = new Dictionary<string, ScrapedSong>();
            int maxSongs = 59;
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.author, null);
            foreach (var author in authorList)
            {
                queryBuilder.Criteria = author;
                var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Author) { SearchQuery = queryBuilder.GetQuery(), MaxSongs = maxSongs };
                var songsByAuthor = reader.GetSongsFromFeed(settings);
                Assert.IsTrue(songsByAuthor.Count > 0);
                Assert.IsTrue(songsByAuthor.Count <= maxSongs);
                int expectedPages = ExpectedPagesForSongs(songsByAuthor.Count);
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
        public void Success_Newest()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 55;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Latest) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(settings.MaxSongs, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Success_Filtered360()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 5;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Latest) { MaxSongs = maxSongs };
            Func<ScrapedSong, bool> filter = song =>
            {
                var meta = song.JsonData["metadata"];
                var chara = meta["characteristics"];
                var list = chara.Children().ToList();
                return chara.Any(t => t["name"].Value<string>() == "360Degree");
            };
            filter = SongFeedReaders.Filtering.BuiltInFilters.OneSaber;
            settings.Filter = filter;
            var result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(settings.MaxSongs, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Success_Hot()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 50;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Hot) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count == settings.MaxSongs);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        [TestMethod]
        public void Success_Plays()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 50;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Plays) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count == settings.MaxSongs);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        private int ExpectedPagesForSongs(int songs)
        {
            return (int)Math.Ceiling((double)songs / BeatSaverReader.SongsPerPage);
        }

        [TestMethod]
        public void Downloads_PageLimit()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxPages = 5;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Downloads) { MaxPages = maxPages };
            var result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(settings.MaxPages * BeatSaverFeed.GlobalSongsPerPage, result.Songs.Count);
            int expectedPages = maxPages;
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Downloads_SongLimit()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 45;
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Downloads) { MaxSongs = maxSongs };
            var result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(maxSongs, result.Songs.Count);
            //int expectedPages = ExpectedPagesForSongs(maxSongs);
            //Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        //[TestMethod]
        //public void Downloads_SongLimit()
        //{
        //    var reader = new BeatSaverReader() { StoreRawData = true };
        //    int maxSongs = 45;
        //    var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Downloads) { MaxSongs = maxSongs };
        //    var result = reader.GetSongsFromFeed(settings);
        //    Assert.AreEqual(maxSongs, result.Songs.Count);
        //    int expectedPages = ExpectedPagesForSongs(maxSongs);
        //    Assert.AreEqual(expectedPages, result.PagesChecked);
        //    foreach (var song in result.Songs.Values)
        //    {
        //        Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
        //    }
        //}

        [TestMethod]
        public void Search_Default_LimitedSongs()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 10;
            var searchType = BeatSaverSearchType.name;
            var criteria = "Believer";
            var query = new SearchQueryBuilder(searchType, criteria).GetQuery();
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Search) { MaxSongs = maxSongs, SearchQuery = query };
            var result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Count <= 10);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Search_Hash()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 10;
            string criteria = "19F2879D11A91B51A5C090D63471C3E8D9B7AEE3";
            var searchType = BeatSaverSearchType.hash;
            var query = new SearchQueryBuilder(searchType, criteria).GetQuery();
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Search) { MaxSongs = maxSongs, SearchQuery = query };
            var result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(1, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Search_User()
        {
            var reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 10;
            string criteria = "19F2879D11A91B51A5C090D63471C3E8D9B7AEE3";
            var searchType = BeatSaverSearchType.hash;
            var query = new SearchQueryBuilder(searchType, criteria).GetQuery();
            var settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Search) { MaxSongs = maxSongs, SearchQuery = query };
            var result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(1, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (var song in result.Songs.Values)
            {
                Console.WriteLine($"{song.SongName} by {song.MapperName}, {song.Hash}");
            }
        }
        #endregion
    }
}
