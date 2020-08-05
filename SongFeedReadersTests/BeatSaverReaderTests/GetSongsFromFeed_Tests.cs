using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeatSaver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            string[] authorList = new string[] { "BlackBlazon", "greatyazer", "joetastic" };
            Dictionary<string, ScrapedSong> songList = new Dictionary<string, ScrapedSong>();
            int maxSongs = 59;
            int maxPages = 10;
            SearchQueryBuilder queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.author, null);
            foreach (string author in authorList)
            {
                queryBuilder.Criteria = author;
                BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Author) { SearchQuery = queryBuilder.GetQuery(), MaxSongs = maxSongs, MaxPages = maxPages };
                FeedResult songsByAuthor = reader.GetSongsFromFeed(settings);
                Assert.IsTrue(songsByAuthor.Count > 0);
                Assert.IsTrue(songsByAuthor.Count <= maxSongs);
                int expectedPages = ExpectedPagesForSongs(songsByAuthor.Count);
                Assert.IsTrue(expectedPages <= songsByAuthor.PagesChecked);
                foreach (KeyValuePair<string, ScrapedSong> song in songsByAuthor.Songs)
                {
                    songList.TryAdd(song.Key, song.Value);
                }
            }
            IEnumerable<string> detectedAuthors = songList.Values.Select(s => s.LevelAuthorName.ToLower()).Distinct();
            foreach (KeyValuePair<string, ScrapedSong> song in songList)
            {
                Assert.IsTrue(song.Value.DownloadUri != null);
                Assert.IsTrue(authorList.Any(a => a.ToLower() == song.Value.LevelAuthorName.ToLower()));
            }
            foreach (string author in authorList)
            {
                Assert.IsTrue(songList.Any(s => s.Value.LevelAuthorName.ToLower() == author.ToLower()));
            }

            // BlackBlazon check
            string blazonHash = "58de2d709a45b68fdb1dbbfefb187f59f629bfc5".ToUpper();
            ScrapedSong blazonSong = songList[blazonHash];
            Assert.IsTrue(blazonSong != null);
            Assert.IsTrue(blazonSong.DownloadUri != null);
            // GreatYazer check
            string songHash = "bf8c016dc6b9832ece3030f05277bbbe67db790d".ToUpper();
            ScrapedSong yazerSong = songList[songHash];
            Assert.IsTrue(yazerSong != null);
            Assert.IsTrue(yazerSong.DownloadUri != null);
            var checkedPages = songList.Values.Select(s => s.SourceUri.OriginalString).Distinct().ToList();
            checkedPages.ForEach(p => Console.WriteLine(p));
        }

        [TestMethod]
        public void Success_Newest()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 55;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Latest) { MaxSongs = maxSongs };
            FeedResult result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(settings.MaxSongs, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Success_Filtered360()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 20;
            int maxPages = 30;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Latest) { MaxSongs = maxSongs, MaxPages = maxPages };
            Progress<ReaderProgress> progress = new Progress<ReaderProgress>(p =>
            {
                if (p.SongCount > 0)
                    Console.WriteLine($"Progress: Page {p.CurrentPage} found {p.SongCount} songs.");
                else
                    Console.WriteLine($"Progress: Page {p.CurrentPage} did not have any songs.");
            });
            bool stopAfter(ScrapedSong song)
            {
                DateTime uploadDate = song.JsonData["uploaded"].Value<DateTime>();
                bool shouldStop = uploadDate < (DateTime.Now - TimeSpan.FromDays(5));
                if (shouldStop)
                    Console.WriteLine($"StopWhenAny reached with {song.Key} ({uploadDate.ToString()})");
                return shouldStop;
            }
            Func<ScrapedSong, bool> filter = SongFeedReaders.Filtering.BuiltInFilters.ThreeSixtyDegree;
            settings.Filter = filter;
            settings.StopWhenAny = stopAfter;
            FeedResult result = reader.GetSongsFromFeedAsync(settings, progress, CancellationToken.None).Result;
            Assert.IsTrue(result.Count > 0 && result.Count < maxPages * settings.SongsPerPage);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            Console.WriteLine($"----------------");
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Success_Hot()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 53;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Hot) { MaxSongs = maxSongs };
            Progress<ReaderProgress> progress = new Progress<ReaderProgress>(p =>
            {
                if (p.SongCount > 0)
                    Console.WriteLine($"Progress: Page {p.CurrentPage} found {p.SongCount} songs.");
                else
                    Console.WriteLine($"Progress: Page {p.CurrentPage} did not have any songs.");
            });
            FeedResult result = reader.GetSongsFromFeedAsync(settings, progress, CancellationToken.None).Result;
            Assert.IsTrue(result.Count == settings.MaxSongs);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            Console.WriteLine($"----------------");
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Assert.IsFalse(string.IsNullOrEmpty(song.Key));
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void CancelledImmediate()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true }; 
            var cts = new CancellationTokenSource();
            cts.Cancel();
            int maxSongs = 50;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Plays) { MaxSongs = maxSongs };
            FeedResult result = reader.GetSongsFromFeed(settings, cts.Token);
            Assert.IsFalse(result.Successful);
            Assert.AreEqual(FeedResultError.Cancelled, result.ErrorCode);
            cts.Dispose();
        }

        [TestMethod]
        public void CancelledInProgress()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            var cts = new CancellationTokenSource(500);
            int maxSongs = 50;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Plays) { MaxSongs = maxSongs };
            FeedResult result = reader.GetSongsFromFeed(settings, cts.Token);
            Assert.IsFalse(result.Successful);
            Assert.IsTrue(result.Count > 0);
            Assert.AreEqual(FeedResultError.Cancelled, result.ErrorCode);
            cts.Dispose();
        }

        [TestMethod]
        public void Success_Plays()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 50;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Plays) { MaxSongs = maxSongs };
            FeedResult result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count == settings.MaxSongs);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.IsTrue(expectedPages <= result.PagesChecked);
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }

        private int ExpectedPagesForSongs(int songs)
        {
            return (int)Math.Ceiling((double)songs / BeatSaverReader.SongsPerPage);
        }

        [TestMethod]
        public async Task Downloads_PageLimit()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxPages = 3;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Downloads) { MaxPages = maxPages };
            Progress<ReaderProgress> progress = new Progress<ReaderProgress>(p =>
            {
                if (p.SongCount > 0)
                    Console.WriteLine($"Progress: Page {p.CurrentPage} found {p.SongCount} songs.");
                else
                    Console.WriteLine($"Progress: Page {p.CurrentPage} did not have any songs.");
            });
            CancellationTokenSource cts = new CancellationTokenSource();
            FeedResult result = await reader.GetSongsFromFeedAsync(settings, progress, cts.Token).ConfigureAwait(false);
            Assert.AreEqual(settings.MaxPages * BeatSaverFeed.GlobalSongsPerPage, result.Songs.Count);
            int expectedPages = maxPages;
            Assert.AreEqual(expectedPages, result.PagesChecked);
            await Task.Delay(100).ConfigureAwait(false);
            //foreach (ScrapedSong song in result.Songs.Values)
            //{
            //    Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            //}
        }

        [TestMethod]
        public async Task Downloads_PageLimitStartingPage()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxPages = 3;
            int startingPage = 3;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Downloads) 
            { MaxPages = maxPages, StartingPage = startingPage };
            Progress<ReaderProgress> progress = new Progress<ReaderProgress>(p =>
            {
                if (p.SongCount > 0)
                    Console.WriteLine($"Progress: Page {p.CurrentPage} found {p.SongCount} songs.");
                else
                    Console.WriteLine($"Progress: Page {p.CurrentPage} did not have any songs.");
            });
            CancellationTokenSource cts = new CancellationTokenSource();
            FeedResult result = await reader.GetSongsFromFeedAsync(settings, progress, cts.Token).ConfigureAwait(false);
            Assert.AreEqual(settings.MaxPages * BeatSaverFeed.GlobalSongsPerPage, result.Songs.Count);
            int expectedPages = maxPages;
            Assert.AreEqual(expectedPages, result.PagesChecked);
            await Task.Delay(100).ConfigureAwait(false);
            //foreach (ScrapedSong song in result.Songs.Values)
            //{
            //    Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            //}
        }

        [TestMethod]
        public void Downloads_SongLimit()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 45;
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Downloads) { MaxSongs = maxSongs };
            FeedResult result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(maxSongs, result.Songs.Count);
            //int expectedPages = ExpectedPagesForSongs(maxSongs);
            //Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
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
        //        Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
        //    }
        //}

        [TestMethod]
        public void Search_Default_LimitedSongs()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 10;
            BeatSaverSearchType searchType = BeatSaverSearchType.name;
            string criteria = "Believer";
            BeatSaverSearchQuery query = new SearchQueryBuilder(searchType, criteria).GetQuery();
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Search) { MaxSongs = maxSongs, SearchQuery = query };
            FeedResult result = reader.GetSongsFromFeed(settings);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Count <= 10);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Search_Hash()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 10;
            string criteria = "19F2879D11A91B51A5C090D63471C3E8D9B7AEE3";
            BeatSaverSearchType searchType = BeatSaverSearchType.hash;
            BeatSaverSearchQuery query = new SearchQueryBuilder(searchType, criteria).GetQuery();
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Search) { MaxSongs = maxSongs, SearchQuery = query };
            FeedResult result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(1, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }

        [TestMethod]
        public void Search_User()
        {
            BeatSaverReader reader = new BeatSaverReader() { StoreRawData = true };
            int maxSongs = 10;
            string criteria = "19F2879D11A91B51A5C090D63471C3E8D9B7AEE3";
            BeatSaverSearchType searchType = BeatSaverSearchType.hash;
            BeatSaverSearchQuery query = new SearchQueryBuilder(searchType, criteria).GetQuery();
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings((int)BeatSaverFeedName.Search) { MaxSongs = maxSongs, SearchQuery = query };
            FeedResult result = reader.GetSongsFromFeed(settings);
            Assert.AreEqual(1, result.Count);
            int expectedPages = ExpectedPagesForSongs(result.Count);
            Assert.AreEqual(expectedPages, result.PagesChecked);
            foreach (ScrapedSong song in result.Songs.Values)
            {
                Console.WriteLine($"{song.Name} by {song.LevelAuthorName}, {song.Hash}");
            }
        }
        #endregion
    }
}
