﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Logging;
using SongFeedReaders.Data;
using Newtonsoft.Json;
using WebUtilities;

namespace SongFeedReaders.Readers.ScoreSaber
{
    public class ScoreSaberReader : FeedReaderBase
    {
        /// API Examples:
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=50&page=1&ranked=1 // Sorted by PP
        /// https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit=10&page=1&search=honesty&ranked=1
        /// cat options:
        /// 0 = trending
        /// 1 = date ranked
        /// 2 = scores set
        /// 3 = star rating
        /// 4 = author
        #region Constants
        public static string NameKey => "ScoreSaberReader";
        public static string SourceKey => "ScoreSaber";

        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a ScoreSaberFeedSettings.";
        #endregion
        private static FeedReaderLoggerBase? _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public override string Name { get { return NameKey; } }
        public override string Source { get { return SourceKey; } }

        public static readonly Uri ReaderRootUri = new Uri("https://scoresaber.com/");

        public override Uri RootUri => ReaderRootUri;
        public override bool Ready { get; protected set; }

        public override void PrepareReader()
        {
            if (!Ready)
            {
                Ready = true;
            }
        }

        public override string GetFeedName(IFeedSettings settings)
        {
            if (!(settings is ScoreSaberFeedSettings ssSettings))
                throw new ArgumentException("Settings is not ScoreSaberFeedSettings", nameof(settings));
            return ScoreSaberFeed.Feeds[ssSettings.Feed].DisplayName;
        }

        public static bool IsValidSearchQuery(string query)
        {
            var valid = false;
            if (!string.IsNullOrEmpty(query))
                valid = true;
            return valid;
        }

        #region Web Requests

        #region Async
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_settings"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException">Throw when the provided settings object isn't a BeatSaverFeedSettings</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="_settings"/> is null.</exception>
        public async Task<FeedResult> GetSongsFromScoreSaberAsync(ScoreSaberFeedSettings _settings, IProgress<ReaderProgress> progress, CancellationToken cancellationToken)
        {
            if (_settings == null)
                throw new ArgumentNullException(nameof(_settings), "settings cannot be null for ScoreSaberReader.GetSongsFromScoreSaberAsync");
            if (!(_settings is ScoreSaberFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            // "https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}"
            int songsPerPage = settings.SongsPerPage;
            if (songsPerPage == 0)
                songsPerPage = 100;
            int pageNum = settings.StartingPage;
            //int maxPages = (int)Math.Ceiling(settings.MaxSongs / ((float)songsPerPage));
            int maxPages = settings.MaxPages;
            int pagesChecked = 0;
            if (pageNum > 1 && maxPages != 0)
                maxPages = maxPages + pageNum - 1;
            //if (settings.MaxPages > 0)
            //    maxPages = maxPages < settings.MaxPages ? maxPages : settings.MaxPages; // Take the lower limit.
            var feed = new ScoreSaberFeed(settings);
            try
            {
                feed.EnsureValidSettings();
            }
            catch (InvalidFeedSettingsException ex)
            {
                return new FeedResult(null, null, ex, FeedResultError.Error);
            }
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            
            var pageResults = new List<PageReadResult>();
            Uri uri = feed.GetUriForPage(1);
            PageReadResult result = await feed.GetSongsAsync(uri, cancellationToken).ConfigureAwait(false);
            pageResults.Add(result);
            foreach (var song in result.Songs)
            {
                if (!songs.ContainsKey(song.Hash) && (songs.Count < settings.MaxSongs || settings.MaxSongs == 0))
                {
                    songs.Add(song.Hash, song);
                }
            }
            pagesChecked++;
            progress?.Report(new ReaderProgress(pagesChecked, songs.Count));
            bool continueLooping = true;

            do
            {
                pageNum++;
                //int diffCount = 0;
                if ((maxPages > 0 && pageNum > maxPages) || (settings.MaxSongs > 0 && songs.Count >= settings.MaxSongs))
                    break;
                if (Utilities.IsPaused)
                    await Utilities.WaitUntil(() => !Utilities.IsPaused, 500).ConfigureAwait(false);

                // TODO: Handle PageReadResult here
                uri = feed.GetUriForPage(pageNum);
                var pageResult = await feed.GetSongsAsync(uri, cancellationToken).ConfigureAwait(false);
                pageResults.Add(pageResult);
                if(pageResult.PageError == PageErrorType.Cancelled)
                {
                    return FeedResult.GetCancelledResult(songs, pageResults);
                }
                int uniqueSongCount = 0;
                foreach (var song in pageResult.Songs)
                {
                    //diffCount++;
                    if (!songs.ContainsKey(song.Hash) && (songs.Count < settings.MaxSongs || settings.MaxSongs == 0))
                    {
                        songs.Add(song.Hash, song);
                        uniqueSongCount++;
                    }
                }

                int prog = Interlocked.Increment(ref pagesChecked);
                progress?.Report(new ReaderProgress(prog, uniqueSongCount));
                if (uniqueSongCount > 0)
                    Logger?.Debug($"Receiving {uniqueSongCount} potential songs from {pageResult.Uri}");
                else
                    Logger?.Debug($"Did not find any new songs on page '{pageResult.Uri}' of {Name}.{settings.FeedName}.");
                if (pageResult.IsLastPage)
                {
                    Logger?.Debug($"Last page reached.");
                    continueLooping = false;
                }
                if (!pageResult.Successful)
                {
                    Logger?.Debug($"Page {pageResult.Uri.ToString()} failed, ending read.");
                    if (pageResult.Exception != null)
                        Logger?.Debug($"{pageResult.Exception.Message}\n{pageResult.Exception.StackTrace}");
                    continueLooping = false;
                }

                //pageReadTasks.Add(GetSongsFromPageAsync(url.ToString()));
                if ((maxPages > 0 && pageNum >= maxPages) || (settings.MaxSongs > 0 && songs.Count >= settings.MaxSongs))
                {
                    continueLooping = false;
                }
            } while (continueLooping);


            if (pageResults.Any(r => r.PageError == PageErrorType.Cancelled))
                return FeedResult.GetCancelledResult(songs, pageResults);
            return new FeedResult(songs, pageResults);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_settings"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException">Thrown when the provided IFeedSettings isn't a ScoreSaberFeedSettings.</exception>
        /// <exception cref="ArgumentException">Thrown when the Search feed is selected and the query in settings isn't valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the feed specified in the settings isn't valid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
        public override Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings _settings, IProgress<ReaderProgress> progress, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (_settings == null)
                throw new ArgumentNullException(nameof(_settings), "settings cannot be null for ScoreSaberReader.GetSongsFromFeedAsync");
            if (!(_settings is ScoreSaberFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (!((settings.FeedIndex >= 0 && settings.FeedIndex <= 3) || settings.FeedIndex == 99)) // Validate FeedIndex
                throw new ArgumentOutOfRangeException(nameof(_settings), "_settings contains an invalid FeedIndex value for ScoreSaberReader");
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            if (settings.Feed == ScoreSaberFeedName.TopRanked || settings.Feed == ScoreSaberFeedName.LatestRanked)
                settings.RankedOnly = true;
            if (settings.Feed == ScoreSaberFeedName.Search)
            {
                if (!IsValidSearchQuery(settings.SearchQuery))
                    throw new ArgumentException($"Search query '{settings.SearchQuery ?? "<nul>"}' is not a valid query.");
            }
            return GetSongsFromScoreSaberAsync(settings, progress, cancellationToken);
        }

        #endregion

        #endregion

    }
}
