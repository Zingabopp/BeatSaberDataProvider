using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Logging;
using static SongFeedReaders.WebUtils;
using Newtonsoft.Json;
using WebUtilities;

namespace SongFeedReaders.Readers
{
    public class ScoreSaberReader : IFeedReader
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
        private const string BEATSAVER_DOWNLOAD_URL_BASE = "http://beatsaver.com/api/download/hash/";
        public static string NameKey => "ScoreSaberReader";
        public static string SourceKey => "ScoreSaber";
        private const string PAGENUMKEY = "{PAGENUM}";
        //private static readonly string CATKEY = "{CAT}";
        private const string RANKEDKEY = "{RANKKEY}";
        private const string LIMITKEY = "{LIMIT}";
        private const string QUERYKEY = "{QUERY}";
        private const string INVALID_FEED_SETTINGS_MESSAGE = "The IFeedSettings passed is not a ScoreSaberFeedSettings.";
        private const string TOP_RANKED_KEY = "Top Ranked";
        private const string TRENDING_KEY = "Trending";
        private const string TOP_PLAYED_KEY = "Top Played";
        private const string LATEST_RANKED_KEY = "Latest Ranked";
        private const string SEARCH_KEY = "Search";
        #endregion
        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }
        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public Uri RootUri { get { return new Uri("https://scoresaber.com/"); } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        public void PrepareReader()
        {
            if (!Ready)
            {
                Ready = true;
            }
        }

        private static Dictionary<ScoreSaberFeed, FeedInfo> _feeds;
        public static Dictionary<ScoreSaberFeed, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<ScoreSaberFeed, FeedInfo>()
                    {
                        { (ScoreSaberFeed)0, new FeedInfo(TRENDING_KEY, "ScoreSaber Trending", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=0&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)1, new FeedInfo(LATEST_RANKED_KEY, "ScoreSaber Latest Ranked", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=1&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)2, new FeedInfo(TOP_PLAYED_KEY, "ScoreSaber Top Played", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=2&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)3, new FeedInfo(TOP_RANKED_KEY, "ScoreSaber Top Ranked", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}") },
                        { (ScoreSaberFeed)99, new FeedInfo(SEARCH_KEY, "ScoreSaber Search", $"https://scoresaber.com/api.php?function=get-leaderboards&cat=3&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}&search={QUERYKEY}") }
                    };
                }
                return _feeds;
            }
        }

        public string GetFeedName(IFeedSettings settings)
        {
            if (!(settings is ScoreSaberFeedSettings ssSettings))
                throw new ArgumentException("Settings is not ScoreSaberFeedSettings", nameof(settings));
            return Feeds[ssSettings.Feed].DisplayName;
        }

        public static void GetPageUrl(ref StringBuilder baseUrl, Dictionary<string, string> replacements)
        {
            if (baseUrl == null)
                throw new ArgumentNullException(nameof(replacements), "baseUrl cannot be null for ScoreSaberReader.GetPageUrl");
            if (replacements == null)
                throw new ArgumentNullException(nameof(replacements), "replacements cannot be null for ScoreSaberReader.GetPageUrl");
            foreach (var key in replacements.Keys)
            {
                baseUrl.Replace(key, replacements[key]);
            }
        }

        // TODO: This should return a List<ScrapedSong> (or dictionary) and throw exceptions if it fails.
        public PageReadResult GetSongsFromPageText(string pageText, Uri sourceUri)
        {
            JObject result = new JObject();
            List<ScrapedSong> songs = new List<ScrapedSong>();
            try
            {
                result = JObject.Parse(pageText);

            }
            catch (JsonReaderException ex)
            {
                string message = "Unable to parse JSON from text";
                Logger.Exception(message, ex);
                return new PageReadResult(sourceUri, null, new FeedReaderException(message, ex));
            }
            var songJSONAry = result["songs"]?.ToArray();
            if (songJSONAry == null)
            {
                Logger.Error("Invalid page text: 'songs' field not found.");
            }
            foreach (var song in songJSONAry)
            {
                var hash = song["id"]?.Value<string>();
                var songName = song["name"]?.Value<string>();
                var mapperName = song["levelAuthorName"]?.Value<string>();

                if (!string.IsNullOrEmpty(hash))
                    songs.Add(new ScrapedSong(hash)
                    {
                        DownloadUri = Utilities.GetUriFromString(BEATSAVER_DOWNLOAD_URL_BASE + hash),
                        SourceUri = sourceUri,
                        SongName = songName,
                        MapperName = mapperName,
                        RawData = StoreRawData ? song.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                    });
            }
            return new PageReadResult(sourceUri, songs);
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
        public async Task<FeedResult> GetSongsFromScoreSaberAsync(ScoreSaberFeedSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for ScoreSaberReader.GetSongsFromScoreSaberAsync");
            // "https://scoresaber.com/api.php?function=get-leaderboards&cat={CATKEY}&limit={LIMITKEY}&page={PAGENUMKEY}&ranked={RANKEDKEY}"
            int songsPerPage = settings.SongsPerPage;
            if (songsPerPage == 0)
                songsPerPage = 100;
            int pageNum = settings.StartingPage;
            //int maxPages = (int)Math.Ceiling(settings.MaxSongs / ((float)songsPerPage));
            int maxPages = settings.MaxPages;
            if (pageNum > 1 && maxPages != 0)
                maxPages = maxPages + pageNum - 1;
            //if (settings.MaxPages > 0)
            //    maxPages = maxPages < settings.MaxPages ? maxPages : settings.MaxPages; // Take the lower limit.
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            StringBuilder url = new StringBuilder(Feeds[settings.Feed].BaseUrl);
            Dictionary<string, string> urlReplacements = new Dictionary<string, string>() {
                {LIMITKEY, songsPerPage.ToString() },
                {PAGENUMKEY, pageNum.ToString()},
                {RANKEDKEY, settings.RankedOnly ? "1" : "0" }
            };
            if (settings.Feed == ScoreSaberFeed.Search)
            {
                urlReplacements.Add(QUERYKEY, settings.SearchQuery);
            }
            GetPageUrl(ref url, urlReplacements);
            var uri = new Uri(url.ToString());
            string pageText = "";
            using (var response = await WebUtils.WebClient.GetAsync(uri).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                else
                {
                    // TODO: Should be returning the failed PageReadResult here.
                    Logger.Error($"Error getting text from {uri}, HTTP Status Code is: {response.StatusCode.ToString()}: {response.ReasonPhrase}");
                }
            }
            var result = GetSongsFromPageText(pageText, uri);
            foreach (var song in result.Songs)
            {
                if (!songs.ContainsKey(song.Hash) && (songs.Count < settings.MaxSongs || settings.MaxSongs == 0))
                {
                    songs.Add(song.Hash, song);
                }
            }
            bool continueLooping = true;
            do
            {
                pageNum++;
                int diffCount = 0;
                if ((maxPages > 0 && pageNum > maxPages) || (settings.MaxSongs > 0 && songs.Count >= settings.MaxSongs))
                    break;
                url.Clear();
                url.Append(Feeds[settings.Feed].BaseUrl);
                if (!urlReplacements.ContainsKey(PAGENUMKEY))
                    urlReplacements.Add(PAGENUMKEY, pageNum.ToString());
                else
                    urlReplacements[PAGENUMKEY] = pageNum.ToString();
                GetPageUrl(ref url, urlReplacements);
                uri = new Uri(url.ToString());
                if (Utilities.IsPaused)
                    await Utilities.WaitUntil(() => !Utilities.IsPaused, 500).ConfigureAwait(false);

                // TODO: Handle PageReadResult here
                var scrapedDiffs = await GetSongsFromPageAsync(uri).ConfigureAwait(false);
                foreach (var song in scrapedDiffs.Songs)
                {
                    diffCount++;
                    if (!songs.ContainsKey(song.Hash) && (songs.Count < settings.MaxSongs || settings.MaxSongs == 0))
                    {
                        songs.Add(song.Hash, song);
                    }
                }
                if (diffCount == 0)
                {
                    Logger?.Debug($"No diffs found on {uri.ToString()}, should be after last page.");
                    continueLooping = false;
                }
                //pageReadTasks.Add(GetSongsFromPageAsync(url.ToString()));
                if ((maxPages > 0 && pageNum >= maxPages) || (settings.MaxSongs > 0 && songs.Count >= settings.MaxSongs))
                {
                    continueLooping = false;
                }
            } while (continueLooping);


            return new FeedResult(songs);
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
        public Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings _settings, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (_settings == null)
                throw new ArgumentNullException(nameof(_settings), "settings cannot be null for ScoreSaberReader.GetSongsFromFeedAsync");
            if (!(_settings is ScoreSaberFeedSettings settings))
                throw new InvalidCastException(INVALID_FEED_SETTINGS_MESSAGE);
            if (!((settings.FeedIndex >= 0 && settings.FeedIndex <= 3) || settings.FeedIndex == 99)) // Validate FeedIndex
                throw new ArgumentOutOfRangeException(nameof(_settings), "_settings contains an invalid FeedIndex value for ScoreSaberReader");
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            if (settings.Feed == ScoreSaberFeed.TopRanked || settings.Feed == ScoreSaberFeed.LatestRanked)
                settings.RankedOnly = true;
            if (settings.Feed == ScoreSaberFeed.Search)
            {
                if (!IsValidSearchQuery(settings.SearchQuery))
                    throw new ArgumentException($"Search query '{settings.SearchQuery ?? "<nul>"}' is not a valid query.");
            }
            return GetSongsFromScoreSaberAsync(settings);
        }

        public async Task<PageReadResult> GetSongsFromPageAsync(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri), "uri cannot be null in ScoreSaberReader.GetSongsFromPageAsync");

            try
            {
                using (var response = await WebUtils.WebClient.GetAsync(uri).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return GetSongsFromPageText(pageText, uri);
                }
            }
            catch (WebClientException ex)
            {
                string respMessage;
                if (ex.Response != null)
                    respMessage = $", response was {ex.Response.StatusCode.ToString()}: {ex.Response.ReasonPhrase}";
                else
                    respMessage = ", response was null.";
                string message = $"Error getting page {uri?.ToString()}{respMessage}";
                Logger.Error(message);
                return new PageReadResult(uri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed));
            }
            catch (Exception ex)
            {
                string message = $"Uncaught error getting page {uri?.ToString()}: {ex.Message}";
                Logger.Error(message);
                return new PageReadResult(uri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed));

            }
        }

        #endregion

        #region Sync
        public FeedResult GetSongsFromFeed(IFeedSettings _settings)
        {
            return GetSongsFromFeedAsync(_settings).Result;
        }

        #endregion

        #endregion

        #region Overloads
        public async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }

        public Task<PageReadResult> GetSongsFromPageAsync(string url)
        {
            return GetSongsFromPageAsync(Utilities.GetUriFromString(url));
        }

        public PageReadResult GetSongsFromPageText(string pageText, string sourceUrl)
        {
            return GetSongsFromPageText(pageText, Utilities.GetUriFromString(sourceUrl));
        }

        #endregion

    }

    public class ScoreSaberFeedSettings : IFeedSettings
    {

        public string FeedName { get { return ScoreSaberReader.Feeds[Feed].Name; } }
        public ScoreSaberFeed Feed { get { return (ScoreSaberFeed)FeedIndex; } set { FeedIndex = (int)value; } }
        public int FeedIndex { get; set; }

        /// <summary>
        /// Only get ranked songs. Forced true for TOP_RANKED and LATEST_RANKED feeds.
        /// </summary>
        public bool RankedOnly { get; set; }

        /// <summary>
        /// Maximum songs to retrieve, will stop the reader before MaxPages is met. Use 0 for unlimited.
        /// </summary>
        public int MaxSongs { get; set; }

        /// <summary>
        /// Maximum pages to check, will stop the reader before MaxSongs is met. Use 0 for unlimited.
        /// </summary>
        public int MaxPages { get; set; }

        /// <summary>
        /// Number of songs shown on a page. 100 is default.
        /// </summary>
        public int SongsPerPage { get; set; }

        /// <summary>
        /// Page of the feed to start on, default is 1. For all feeds, setting '1' here is the same as starting on the first page.
        /// </summary>
        public int StartingPage { get; set; }

        /// <summary>
        /// String to search ScoreSaber with (only used for the Search feed).
        /// </summary>
        public string SearchQuery { get; set; }

        public ScoreSaberFeedSettings(int feedIndex)
        {
            FeedIndex = feedIndex;
            SongsPerPage = 100;
            StartingPage = 1;
        }
    }

    public enum ScoreSaberFeed
    {
        Trending = 0,
        LatestRanked = 1,
        TopPlayed = 2,
        TopRanked = 3,
        Search = 99
    }
}
