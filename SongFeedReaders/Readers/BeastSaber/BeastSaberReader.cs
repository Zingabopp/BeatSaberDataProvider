using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Threading.Tasks.Dataflow;
using SongFeedReaders.DataflowAlternative;
using SongFeedReaders.Logging;
using SongFeedReaders.Data;
using System.Diagnostics;
using WebUtilities;

namespace SongFeedReaders.Readers.BeastSaber
{
    public class BeastSaberReader : FeedReaderBase
    {
        #region Constants
        public static readonly string NameKey = "BeastSaberReader";
        public static readonly string SourceKey = "BeastSaber";
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeastSaberFeedSettings.";

        //private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        public static readonly Uri ReaderRootUri = new Uri("https://bsaber.com");
        public override Uri RootUri => ReaderRootUri;
        public static readonly int SongsPerXmlPage = BeastSaberFeed.SongsPerXmlPage;
        public static readonly int SongsPerJsonPage = BeastSaberFeed.SongsPerJsonPage;
        
        #endregion

        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public override string Name { get { return NameKey; } }
        public override string Source { get { return SourceKey; } }
        public override bool Ready { get; protected set; }

        public string Username { get; set; }
        private int _maxConcurrency;

        /// <summary>
        /// Sets the maximum number of simultaneous page checks.
        /// </summary>
        ///<exception cref="ArgumentOutOfRangeException">Thrown when setting MaxConcurrency less than 1.</exception>
        public int MaxConcurrency
        {
            get { return _maxConcurrency; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("MaxConcurrency", value, "MaxConcurrency must be >= 1.");
                _maxConcurrency = value;
            }
        }


        public override void PrepareReader()
        {
            if (!Ready)
            {
                Ready = true;
            }
        }

        public override string GetFeedName(IFeedSettings settings)
        {
            if (!(settings is BeastSaberFeedSettings ssSettings))
                throw new ArgumentException("Settings is not BeastSaberFeedSettings", nameof(settings));
            return BeastSaberFeed.Feeds[ssSettings.Feed].DisplayName;
        }

        /// <summary>
        /// Creates a new BeastSaberReader.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="maxConcurrency"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if maxConcurrency is less than 1.</exception>
        public BeastSaberReader(string username, int maxConcurrency = 1)
        {
            Ready = false;
            Username = username;
            MaxConcurrency = maxConcurrency;
        }


        #region Web Requests
        // TODO: Abort early when bsaber.com is down (check if all items in block failed?)
        // TODO: Make cancellationToken actually do something.
        /// <summary>
        /// Gets all songs from the feed defined by the provided settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="_settings"/> is null.</exception>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeastSaberFeedSettings.</exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async override Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, IProgress<ReaderProgress> progress, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return FeedResult.CancelledResult;
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeastSaberReader.GetSongsFromFeedAsync.");
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            if (!(settings is BeastSaberFeedSettings _settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.Feed != BeastSaberFeedName.CuratorRecommended && string.IsNullOrEmpty(_settings.Username))
                _settings.Username = Username;
            BeastSaberFeed feed = new BeastSaberFeed(_settings) { StoreRawData = StoreRawData };
            try
            {
                feed.EnsureValidSettings();
            }
            catch (InvalidFeedSettingsException ex)
            {
                return new FeedResult(null, null, ex, FeedResultError.Error);
            }
            int pageIndex = settings.StartingPage;
            int maxPages = _settings.MaxPages;
            bool useMaxSongs = _settings.MaxSongs != 0;
            bool useMaxPages = maxPages != 0;
            if (useMaxPages && pageIndex > 1)
                maxPages = maxPages + pageIndex - 1;
            var ProcessPageBlock = new TransformBlock<int, PageReadResult>(async pageNum =>
            {
                return await feed.GetSongsFromPageAsync(pageNum, cancellationToken).ConfigureAwait(false);
                
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = MaxConcurrency,
                BoundedCapacity = MaxConcurrency,
                CancellationToken = cancellationToken
                //#if NETSTANDARD
                //                , EnsureOrdered = true
                //#endif
            });
            bool continueLooping = true;
            int itemsInBlock = 0;
            List<PageReadResult> pageResults = new List<PageReadResult>(maxPages + 2);
            do
            {
                if (cancellationToken.IsCancellationRequested)
                    continueLooping = false;
                while (continueLooping)
                {
                    if (Utilities.IsPaused)
                        await Utilities.WaitUntil(() => !Utilities.IsPaused, 500, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        continueLooping = false;
                        break;
                    }
                    await ProcessPageBlock.SendAsync(pageIndex, cancellationToken).ConfigureAwait(false); // TODO: Need check with SongsPerPage
                    itemsInBlock++;
                    pageIndex++;

                    if ((pageIndex > maxPages && useMaxPages) || cancellationToken.IsCancellationRequested)
                        continueLooping = false;
                    // TODO: Better http error handling, what if only a single page is broken and returns 0 songs?
                    while (ProcessPageBlock.OutputCount > 0 || itemsInBlock == MaxConcurrency || !continueLooping)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            continueLooping = false;
                            break;
                        }
                        if (itemsInBlock <= 0)
                            break;
                        await ProcessPageBlock.OutputAvailableAsync(cancellationToken).ConfigureAwait(false);
                        while (ProcessPageBlock.TryReceive(out PageReadResult pageResult))
                        {
                            int songsAdded = 0;
                            if (pageResult != null)
                                pageResults.Add(pageResult);
                            if (Utilities.IsPaused)
                                await Utilities.WaitUntil(() => !Utilities.IsPaused, 500, cancellationToken).ConfigureAwait(false);
                            itemsInBlock--;
                            if (pageResult.IsLastPage || pageResult == null || pageResult.Count == 0) // TODO: This will trigger if a single page has an error.
                            {
                                Logger?.Debug("Received no new songs, last page reached.");
                                ProcessPageBlock.Complete();
                                itemsInBlock = 0;
                                continueLooping = false;
                                break;
                            }
                            if (pageResult.Count > 0)
                                Logger?.Debug($"Receiving {pageResult.Count} potential songs from {pageResult.Uri}");
                            else
                                Logger?.Debug($"Did not find any songs on page {pageResult.Page} of {Name}.{settings.FeedName}.");

                            // TODO: Process PageReadResults for better error feedback.
                            foreach (var song in pageResult.Songs)
                            {
                                if (!retDict.ContainsKey(song.Hash))
                                {
                                    if (retDict.Count < settings.MaxSongs || settings.MaxSongs == 0)
                                    {
                                        retDict.Add(song.Hash, song);
                                        songsAdded++;
                                    }
                                    if (retDict.Count >= settings.MaxSongs && useMaxSongs)
                                        continueLooping = false;
                                }
                            }
                            progress?.Report(new ReaderProgress(pageResult.Page, songsAdded));
                            if (!useMaxPages || pageIndex <= maxPages)
                                if (retDict.Count < settings.MaxSongs)
                                    continueLooping = true;
                        }
                    }
                }
            }
            while (continueLooping);
            return new FeedResult(retDict, pageResults);
        }

        #endregion




    }
}
