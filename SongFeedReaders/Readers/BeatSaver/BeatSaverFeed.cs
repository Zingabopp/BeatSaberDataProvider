using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongFeedReaders.Logging;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using static SongFeedReaders.WebUtils;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class BeatSaverFeed : IFeed
    {
        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        #region Constants
        //private static readonly string AUTHORKEY = "{AUTHOR}";
        private static readonly string AUTHORIDKEY = "{AUTHORID}";
        public static readonly string PAGEKEY = "{PAGE}";
        public static readonly string SEARCHTYPEKEY = "{SEARCHTYPE}"; // text or advanced
        public static readonly string SEARCHQUERYKEY = "{SEARCHQUERY}";
        public static readonly int GlobalSongsPerPage = 10;

        private const string DescriptionAuthor = "Retrieves songs by the specified map author.";
        private const string DescriptionLatest = "Retrieves the latest beatmaps posted to BeatSaver.";
        private const string DescriptionHot = "Retrieves songs ordered from most popular to least popular as determined by BeatSaver.";
        private const string DescriptionPlays = "Retrieves the songs with the highest number of plays. (Play count is no longer updated by BeatSaver)";
        private const string DescriptionDownloads = "Retrieves songs ordered from most downloaded to least downloaded.";
        private const string DescriptionSearch = "Retrieves songs matching the provided search criteria from BeatSaver.";
        #endregion

        private static Dictionary<BeatSaverFeedName, FeedInfo> _feeds;
        public static Dictionary<BeatSaverFeedName, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeatSaverFeedName, FeedInfo>()
                    {
                        { (BeatSaverFeedName)0, new FeedInfo("Author", "BeatSaver Authors", "https://beatsaver.com/api/maps/uploader/" +  AUTHORIDKEY + "/" + PAGEKEY, DescriptionAuthor)},
                        { (BeatSaverFeedName)1, new FeedInfo("Latest", "BeatSaver Latest", "https://beatsaver.com/api/maps/latest/" + PAGEKEY, DescriptionLatest) },
                        { (BeatSaverFeedName)2, new FeedInfo("Hot", "BeatSaver Hot", "https://beatsaver.com/api/maps/hot/" + PAGEKEY, DescriptionHot) },
                        { (BeatSaverFeedName)3, new FeedInfo("Plays", "BeatSaver Plays", "https://beatsaver.com/api/maps/plays/" + PAGEKEY, DescriptionPlays) },
                        { (BeatSaverFeedName)4, new FeedInfo("Downloads", "BeatSaver Downloads", "https://beatsaver.com/api/maps/downloads/" + PAGEKEY, DescriptionDownloads) },
                        { (BeatSaverFeedName)98, new FeedInfo("Search", "BeatSaver Search", $"https://beatsaver.com/api/search/{SEARCHTYPEKEY}/{PAGEKEY}/?q={SEARCHQUERYKEY}", DescriptionSearch) },
                    };
                }
                return _feeds;
            }
        }

        public BeatSaverFeedName Feed { get; }
        public FeedInfo FeedInfo { get; }
        public string Name { get { return FeedInfo.Name; } }

        public string DisplayName { get { return FeedInfo.DisplayName; } }

        public string Description { get { return FeedInfo.Description; } }

        public Uri RootUri => BeatSaverReader.ReaderRootUri;

        public string BaseUrl { get { return FeedInfo.BaseUrl; } }

        public BeatSaverSearchQuery? SearchQuery { get; }

        public int SongsPerPage => GlobalSongsPerPage;

        public bool StoreRawData { get; set; }

        public IFeedSettings Settings => BeatSaverFeedSettings;

        public BeatSaverFeedSettings BeatSaverFeedSettings { get; }

        /// <summary>
        /// Returns true if the <see cref="Settings"/> are valid for the feed, false otherwise.
        /// </summary>
        public bool HasValidSettings
        {
            get { return EnsureValidSettings(false); }
        }

        private bool EnsureValidSettings(bool throwException = true)
        {
            string message = string.Empty;
            bool valid = true;
            //if (BeatSaverFeedSettings == null) // BeatSaverFeedSettings should never be null anyway
            //{
            //    return !throwException ? false : throw new InvalidFeedSettingsException($"{nameof(BeatSaverFeedSettings)} cannot be null.");
            //}
            if (Feed == BeatSaverFeedName.Author || Feed == BeatSaverFeedName.Search)
            {
                if (!BeatSaverFeedSettings.SearchQuery.HasValue)
                {
                    message = $"{nameof(BeatSaverFeedSettings)}.{nameof(BeatSaverFeedSettings.SearchQuery)} cannot be null for the {Feed.ToString()} feed.";
                    valid = false;
                }
                else if (string.IsNullOrEmpty(BeatSaverFeedSettings.SearchQuery.Value.Criteria))
                {
                    message = $"{nameof(BeatSaverFeedSettings)}.{nameof(BeatSaverFeedSettings.SearchQuery)}.{nameof(BeatSaverFeedSettings.SearchQuery.Value.Criteria)} cannot be null for the {Feed.ToString()} feed.";
                    valid = false;
                }
                else
                {
                    switch (Feed)
                    {
                        case BeatSaverFeedName.Author:
                            if (!string.IsNullOrEmpty(BeatSaverFeedSettings.AuthorId))
                                break;
                            if (BeatSaverFeedSettings.SearchQuery.Value.SearchType != BeatSaverSearchType.author)
                            {
                                message = $"{nameof(BeatSaverFeedSettings)}.{nameof(BeatSaverFeedSettings.SearchType)} must be 'author' for the {Feed.ToString()} feed.";
                                valid = false;
                            }
                            break;
                        case BeatSaverFeedName.Search:
                            break;
                        default:
                            return false;
                    }
                }
            }
            if (!valid && throwException)
                throw new InvalidFeedSettingsException(message);
            return valid;
        }

        /// <summary>
        /// Throws an <see cref="InvalidFeedSettingsException"/> when the feed's settings aren't valid.
        /// </summary>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        public void EnsureValidSettings()
        {
            EnsureValidSettings(true);
        }

        public FeedAsyncEnumerator GetEnumerator(bool cachePages)
        {
            return new FeedAsyncEnumerator(this, Settings.StartingPage, cachePages);
        }
        public FeedAsyncEnumerator GetEnumerator()
        {
            return GetEnumerator(false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public BeatSaverFeed(BeatSaverFeedSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings), "settings cannot be null when creating a new ScoreSaberFeed.");
            BeatSaverFeedSettings = (BeatSaverFeedSettings)settings.Clone();
            Feed = BeatSaverFeedSettings.Feed;
            FeedInfo = Feeds[BeatSaverFeedSettings.Feed];
            if ((Feed == BeatSaverFeedName.Search || Feed == BeatSaverFeedName.Author) && !BeatSaverFeedSettings.SearchQuery.HasValue)
                throw new ArgumentException(nameof(settings), $"SearchQuery cannot be null in settings for feed {FeedInfo.DisplayName}.");
            SearchQuery = BeatSaverFeedSettings.SearchQuery;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        /// <returns></returns>
        public async Task<PageReadResult> GetSongsFromPageAsync(int page, CancellationToken cancellationToken)
        {
            string pageText;
            JObject result;
            List<IScrapedSong> newSongs;
            Uri pageUri;
            try
            {
                pageUri = GetUriForPage(page);
            }
            catch (InvalidFeedSettingsException)
            {
                throw;
            }

            Logger.Debug($"Getting songs from '{pageUri}'");
            int? lastPage;
            bool isLastPage = false;
            IWebResponseMessage response = null;
            try
            {
                response = await GetBeatSaverAsync(pageUri, cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result = JObject.Parse(pageText);
                int? numSongs = result["totalDocs"]?.Value<int>();
                lastPage = result["lastPage"]?.Value<int>();
                if (lastPage.HasValue)
                    lastPage = lastPage.Value + 1; // BeatSaver pages start at 0, Readers at 1.
                if (numSongs == null || lastPage == null || numSongs == 0)
                {
                    Logger?.Warning($"Error checking Beat Saver's {Name} feed.");
                    return new PageReadResult(pageUri, null, page, new FeedReaderException($"Error getting page in BeatSaverFeed.GetSongsFromPageAsync()", null, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
                }
                isLastPage = page >= lastPage.Value;
                newSongs = new List<IScrapedSong>();
                var scrapedSongs = BeatSaverReader.ParseSongsFromPage(pageText, pageUri, Settings.StoreRawData || StoreRawData);
                foreach (var song in scrapedSongs)
                {
                    if (Settings.Filter == null || Settings.Filter(song))
                        newSongs.Add(song);
                    if (Settings.StopWhenAny != null && Settings.StopWhenAny(song))
                    {
                        isLastPage = true;
                        break;
                    }
                }
            }
            catch (WebClientException ex)
            {
                string errorText = string.Empty;
                if (ex.Response != null)
                {
                    switch (ex.Response.StatusCode)
                    {
                        case 408:
                            errorText = "Timeout";
                            break;
                        default:
                            errorText = "Site Error";
                            break;
                    }
                }
                string message = $"{errorText} getting a response from {pageUri}: {ex.Message}";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return PageReadResult.FromWebClientException(ex, pageUri, page);
            }
            catch (JsonReaderException ex)
            {
                string message = $"Unable to parse JSON from text on page {pageUri.ToString()}";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(pageUri, null, page, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), PageErrorType.ParsingError);
            }
            catch (OperationCanceledException)
            {
                return PageReadResult.CancelledResult(pageUri, page);
            }
            catch (Exception ex)
            {
                string message = $"Uncaught error getting page {pageUri} in BeatSaverFeed.GetSongsFromPageAsync(): {ex.Message}";
                return new PageReadResult(pageUri, null, page, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), PageErrorType.ParsingError);
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
            if (lastPage.HasValue && !isLastPage)
                return new BeatSaverPageResult(pageUri, newSongs, page, lastPage.Value);
            else
                return new PageReadResult(pageUri, newSongs, page, isLastPage);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        /// <returns></returns>
        public Uri GetUriForPage(int page)
        {
            EnsureValidSettings();
            page = page - 1; // BeatSaver pages start at 0, readers at 1
            Uri uri;
            if (Feed == BeatSaverFeedName.Search ||
                (Feed == BeatSaverFeedName.Author && string.IsNullOrEmpty(BeatSaverFeedSettings.AuthorId)))
                uri = SearchQuery.Value.GetUriForPage(page);
            else if (Feed == BeatSaverFeedName.Author)
                uri = new Uri(BaseUrl.Replace(PAGEKEY, page.ToString()).Replace(AUTHORIDKEY, BeatSaverFeedSettings.AuthorId));
            else
                uri = new Uri(BaseUrl.Replace(PAGEKEY, page.ToString()));
            return uri;
        }
    }
}
