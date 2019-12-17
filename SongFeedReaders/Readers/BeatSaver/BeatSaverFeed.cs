using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongFeedReaders.Logging;
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
        private const string SEARCHTYPEKEY = "{SEARCHTYPE}"; // text or advanced
        private const string SEARCHQUERY = "{SEARCHQUERY}";
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
                        { (BeatSaverFeedName)98, new FeedInfo("Search", "BeatSaver Search", $"https://beatsaver.com/api/search/{SEARCHTYPEKEY}/{PAGEKEY}/?q={SEARCHQUERY}", DescriptionSearch) },
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

        public string Criteria { get; }

        public string AuthorId { get; }

        public int SongsPerPage => GlobalSongsPerPage;

        public bool StoreRawData { get; set; }

        public IFeedSettings Settings { get; }

        public BeatSaverFeedSettings BeatSaverFeedSettings { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public BeatSaverFeed(BeatSaverFeedSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings), "settings cannot be null when creating a new ScoreSaberFeed.");
            
            Feed = settings.Feed;
            FeedInfo = Feeds[settings.Feed];
            if (Feed == BeatSaverFeedName.Search && !settings.SearchQuery.HasValue) 
                throw new ArgumentException(nameof(settings), $"SearchQuery cannot be null in settings for feed {FeedInfo.DisplayName}.");
            if (Feed == BeatSaverFeedName.Author && string.IsNullOrEmpty(settings.AuthorId))
                throw new ArgumentException(nameof(settings), $"AuthorId cannot be null in settings for feed {FeedInfo.DisplayName}.");
            BeatSaverFeedSettings = settings;
            Criteria = settings.Criteria;
            AuthorId = settings.AuthorId;
            Settings = settings;
        }

        public async Task<PageReadResult> GetSongsFromPageAsync(int page, CancellationToken cancellationToken)
        {
            string pageText = string.Empty;

            JObject result = new JObject();
            List<ScrapedSong> newSongs;
            var pageUri = GetUriForPage(page);
            bool isLastPage = false;
            IWebResponseMessage response = null;
            try
            {
                response = await GetBeatSaverAsync(pageUri, cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result = JObject.Parse(pageText);
                int? numSongs = result["totalDocs"]?.Value<int>();
                int? lastPage = result["lastPage"]?.Value<int>();
                if (numSongs == null || lastPage == null || numSongs == 0)
                {
                    Logger?.Warning($"Error checking Beat Saver's {Name} feed.");
                    return new PageReadResult(pageUri, null, page, new FeedReaderException($"Error getting page in BeatSaverFeed.GetSongsFromPageAsync()", null, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
                }
                isLastPage = page > lastPage.Value; // BeatSaver pages start at 0, Readers at 1.
                newSongs = BeatSaverReader.ParseSongsFromPage(pageText, pageUri);
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

            return new PageReadResult(pageUri, newSongs, page, isLastPage);
        }

        public Uri GetUriForPage(int page)
        {
            Uri uri;
            if (Feed == BeatSaverFeedName.Search && SearchQuery.HasValue)
                uri = SearchQuery.Value.GetUriForPage(page);
            else
                uri = new Uri(BaseUrl.Replace(PAGEKEY, page.ToString()).Replace(AUTHORIDKEY, AuthorId));
            return uri;
        }
    }
}
