using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongFeedReaders.Data;
using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace SongFeedReaders.Readers.ScoreSaber
{
    public class ScoreSaberFeed : IFeed
    {
        private const string TOP_RANKED_KEY = "Top Ranked";
        private const string TRENDING_KEY = "Trending";
        private const string TOP_PLAYED_KEY = "Top Played";
        private const string LATEST_RANKED_KEY = "Latest Ranked";
        private const string SEARCH_KEY = "Search";
        private const string DescriptionTrending = "Retrieves songs that are Trending as determined by ScoreSaber.";
        private const string DescriptionLatestRanked = "Retrieves the latest ranked songs.";
        private const string DescriptionTopPlayed = "Retrieves the songs that have the most plays as determined by ScoreSaber.";
        private const string DescriptionTopRanked = "Retrieves songs ordered from highest ranked value to lowest.";
        private const string DescriptionSearch = "Retrieves songs matching the search criteria from ScoreSaber.";

        private const string PAGENUMKEY = "{PAGENUM}";
        //private static readonly string CATKEY = "{CAT}";
        private const string RANKEDKEY = "{RANKKEY}";
        private const string LIMITKEY = "{LIMIT}";
        private const string QUERYKEY = "{QUERY}";

        private static Dictionary<ScoreSaberFeedName, FeedInfo> _feeds;
        public static Dictionary<ScoreSaberFeedName, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<ScoreSaberFeedName, FeedInfo>()
                    {
                        { (ScoreSaberFeedName)0, new FeedInfo(TRENDING_KEY, "ScoreSaber Trending", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=0&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}",DescriptionTrending ) },
                        { (ScoreSaberFeedName)1, new FeedInfo(LATEST_RANKED_KEY, "ScoreSaber Latest Ranked", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=1&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}", DescriptionLatestRanked) },
                        { (ScoreSaberFeedName)2, new FeedInfo(TOP_PLAYED_KEY, "ScoreSaber Top Played", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=2&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}", DescriptionTopPlayed) },
                        { (ScoreSaberFeedName)3, new FeedInfo(TOP_RANKED_KEY, "ScoreSaber Top Ranked", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}", DescriptionTopRanked) },
                        { (ScoreSaberFeedName)99, new FeedInfo(SEARCH_KEY, "ScoreSaber Search", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}&search={QUERYKEY}", DescriptionSearch) }
                    };
                }
                return _feeds;
            }
        }

        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }

        public ScoreSaberFeedName Feed { get; }

        public FeedInfo FeedInfo { get; }

        public string Name { get { return FeedInfo.Name; } }

        public string DisplayName { get { return FeedInfo.DisplayName; } }

        public string Description { get { return FeedInfo.Description; } }

        public Uri RootUri => ScoreSaberReader.ReaderRootUri;

        public string BaseUrl { get { return FeedInfo.BaseUrl; } }

        public int SongsPerPage { get; set; }

        public string SearchQuery { get; }

        public bool RankedOnly { get; }

        public bool StoreRawData { get; set; }

        public IFeedSettings Settings => ScoreSaberFeedSettings;

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
            if (Feed == ScoreSaberFeedName.Search)
            {
                if (string.IsNullOrEmpty(ScoreSaberFeedSettings.SearchQuery))
                {
                    message = $"{nameof(ScoreSaberFeedSettings)}.{nameof(ScoreSaberFeedSettings.SearchQuery)} cannot be null or empty for the {Feed.ToString()} feed.";
                    valid = false;
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

        public ScoreSaberFeedSettings ScoreSaberFeedSettings { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScoreSaberFeed(ScoreSaberFeedSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings), "settings cannot be null when creating a new ScoreSaberFeed.");
            ScoreSaberFeedSettings = (ScoreSaberFeedSettings)settings.Clone();
            Feed = ScoreSaberFeedSettings.Feed;
            FeedInfo = Feeds[ScoreSaberFeedSettings.Feed];
            SongsPerPage = ScoreSaberFeedSettings.SongsPerPage;
            SearchQuery = ScoreSaberFeedSettings.SearchQuery;
            RankedOnly = ScoreSaberFeedSettings.RankedOnly;
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
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page cannot be less than 1.");
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            Uri uri;
            try
            {
                uri = GetUriForPage(page);
            }
            catch (InvalidFeedSettingsException)
            {
                throw;
            }
            string pageText = "";

            Logger.Debug($"Getting songs from '{uri}'");
            IWebResponseMessage? response = null;

            try
            {
                response = await WebUtils.WebClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (WebClientException ex)
            {
                string errorText = string.Empty;
                int statusCode = ex?.Response?.StatusCode ?? 0;
                if (statusCode != 0)
                {
                    switch (statusCode)
                    {
                        case 404:
                            errorText = $"{uri.ToString()} was not found.";
                            break;
                        case 408:
                            errorText = $"Timeout getting first page in ScoreSaberReader: {uri}: {ex.Message}";
                            break;
                        default:
                            errorText = $"Site Error getting first page in ScoreSaberReader: {uri}: {ex.Message}";
                            break;
                    }
                }
                Logger?.Debug(errorText);
                // No need for a stacktrace if it's one of these errors.
                if (!(statusCode == 404 || statusCode == 408 || statusCode == 500))
                    Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return PageReadResult.FromWebClientException(ex, uri, page);
            }
            catch (Exception ex)
            {
                string message = $"Uncaught error getting the first page in ScoreSaberReader.GetSongsFromScoreSaberAsync(): {ex.Message}";
                return new PageReadResult(uri, new List<ScrapedSong>(), page, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), PageErrorType.Unknown);
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
            bool isLastPage;
            try
            {
                List<ScrapedSong>? diffs = GetSongsFromPageText(pageText, uri, Settings.StoreRawData || StoreRawData);
                isLastPage = diffs.Count < SongsPerPage;
                foreach (ScrapedSong? diff in diffs)
                {
                    if (!songs.ContainsKey(diff.Hash) && (Settings.Filter == null || Settings.Filter(diff)))
                        songs.Add(diff.Hash, diff);
                    if (Settings.StopWhenAny != null && Settings.StopWhenAny(diff))
                        isLastPage = true;
                }
            }
            catch (JsonReaderException ex)
            {
                string message = "Unable to parse JSON from text";
                Logger?.Debug($"{message}: {ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(uri, null, page, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
            }
            catch (Exception ex)
            {
                string message = $"Unhandled exception from GetSongsFromPageText() while parsing {uri}";
                Logger?.Debug($"{message}: {ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(uri, null, page, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
            }

            return new PageReadResult(uri, songs.Values.ToList(), page, isLastPage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="sourceUri"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        /// <exception cref="JsonReaderException"></exception>
        /// 
        public static List<ScrapedSong>? GetSongsFromPageText(string pageText, Uri sourceUri, bool storeRawData)
        {
            JObject result;
            List<ScrapedSong> songs = new List<ScrapedSong>();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (JsonReaderException)
            {
                throw;
                //string message = "Unable to parse JSON from text";
                //Logger?.Debug($"{message}: {ex.Message}\n{ex.StackTrace}");
                //return new PageReadResult(sourceUri, null, page, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
            }
            JToken[]? songJSONAry = result["songs"]?.ToArray();
            if (songJSONAry == null)
            {
                string message = "Invalid page text: 'songs' field not found.";
                Logger?.Debug(message);
                return null;
            }
            foreach (JObject song in songJSONAry)
            {
                string? hash = song["id"]?.Value<string>();
                string? songName = song["name"]?.Value<string>();
                string? mapperName = song["levelAuthorName"]?.Value<string>();

                if (!string.IsNullOrEmpty(hash))
                    songs.Add(new ScrapedSong(hash, songName, mapperName, Utilities.GetDownloadUriByHash(hash), sourceUri, storeRawData ? song : null));
            }
            return songs;
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
            string url = BaseUrl;
            Dictionary<string, string> urlReplacements = new Dictionary<string, string>() {
                {LIMITKEY, SongsPerPage.ToString() },
                {PAGENUMKEY, page.ToString()},
                {RANKEDKEY, RankedOnly ? "1" : "0" }
            };
            if (Feed == ScoreSaberFeedName.Search)
            {
                urlReplacements.Add(QUERYKEY, SearchQuery);
            }
            foreach (KeyValuePair<string, string> pair in urlReplacements)
            {
                url = url.Replace(pair.Key, pair.Value);
            }
            return new Uri(url);
        }

        public FeedAsyncEnumerator GetEnumerator(bool cachePages)
        {
            return new FeedAsyncEnumerator(this, Settings.StartingPage, cachePages);
        }
        public FeedAsyncEnumerator GetEnumerator()
        {
            return GetEnumerator(false);
        }

        public static List<ScrapedSong> GetSongsFromPageText(string pageText, string sourceUrl, bool storeRawData)
        {
            return GetSongsFromPageText(pageText, new Uri(sourceUrl), storeRawData);
        }
    }
}
