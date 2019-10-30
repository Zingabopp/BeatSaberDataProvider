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
using SongFeedReaders.Logging;
using static SongFeedReaders.WebUtils;
using WebUtilities;

namespace SongFeedReaders.Readers
{
    public class BeatSaverReader : IFeedReader
    {
        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public static string NameKey => "BeatSaverReader";
        public string Name { get { return NameKey; } }
        public static readonly string SourceKey = "BeatSaver";
        public string Source { get { return SourceKey; } }
        public Uri RootUri { get { return new Uri("https://beatsaver.com"); } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }
        #region Constants
        //private static readonly string AUTHORKEY = "{AUTHOR}";
        private const string AUTHORIDKEY = "{AUTHORID}";
        private const string PAGEKEY = "{PAGE}";
        private const string SEARCHTYPEKEY = "{TYPE}";
        private const string SEARCHKEY = "{SEARCH}";
        public const int SongsPerPage = 10;
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
        private const string BEATSAVER_DOWNLOAD_URL_BASE = "https://beatsaver.com/api/download/key/";
        private const string BEATSAVER_DETAILS_BASE_URL = "https://beatsaver.com/api/maps/detail/";
        private const string BEATSAVER_GETBYHASH_BASE_URL = "https://beatsaver.com/api/maps/by-hash/";
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Remove unused private members
        private const string BEATSAVER_NIGHTLYDUMP_URL = "https://beatsaver.com/api/download/dump/maps";
#pragma warning restore CA1823 // Remove unused private members
#pragma warning restore IDE0051 // Remove unused private members
        #endregion

        private static ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        // { (BeatSaverFeeds)99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }
        private static Dictionary<BeatSaverFeed, FeedInfo> _feeds;
        public static Dictionary<BeatSaverFeed, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeatSaverFeed, FeedInfo>()
                    {
                        { (BeatSaverFeed)0, new FeedInfo("Author", "BeatSaver Authors", "https://beatsaver.com/api/maps/uploader/" +  AUTHORIDKEY + "/" + PAGEKEY)},
                        { (BeatSaverFeed)1, new FeedInfo("Latest", "BeatSaver Latest", "https://beatsaver.com/api/maps/latest/" + PAGEKEY) },
                        { (BeatSaverFeed)2, new FeedInfo("Hot", "BeatSaver Hot", "https://beatsaver.com/api/maps/hot/" + PAGEKEY) },
                        { (BeatSaverFeed)3, new FeedInfo("Plays", "BeatSaver Plays", "https://beatsaver.com/api/maps/plays/" + PAGEKEY) },
                        { (BeatSaverFeed)4, new FeedInfo("Downloads", "BeatSaver Downloads", "https://beatsaver.com/api/maps/downloads/" + PAGEKEY) },
                        { (BeatSaverFeed)98, new FeedInfo("Search", "BeatSaver Search", $"https://beatsaver.com/api/search/text/{PAGEKEY}?q={SEARCHKEY}") },
                    };
                }
                return _feeds;
            }
        }

        public void PrepareReader()
        {
            Ready = true;
        }

        /// <summary>
        /// Gets the DisplayName of the feed specified in settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        /// <exception cref="InvalidCastException">Thrown when settings is not a BeatSaverFeedSettings.</exception>
        public string GetFeedName(IFeedSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeatSaverReader.GetFeedName");
            if (!(settings is BeatSaverFeedSettings ssSettings))
                throw new InvalidCastException("Settings is not BeatSaverFeedSettings in BeatSaverReader.GetFeedName");
            return Feeds[ssSettings.Feed].DisplayName;
        }

        public static Uri GetPageUrl(BeatSaverFeed feed, int pageIndex = 0, Dictionary<string, string> replacements = null)
        {
            string mapperId = string.Empty;
            StringBuilder url = new StringBuilder(Feeds[feed].BaseUrl);
            //if (!string.IsNullOrEmpty(author) && author.Length > 3)
            //    mapperId = GetAuthorID(author);
            if (replacements != null)
                foreach (var key in replacements.Keys)
                {
                    url.Replace(key, replacements[key]);
                }
            return Utilities.GetUriFromString(url.Replace(PAGEKEY, pageIndex.ToString()).ToString());

        }

        public static List<ScrapedSong> ParseSongsFromPage(string pageText, string sourceUrl)
        {
            return ParseSongsFromPage(pageText, Utilities.GetUriFromString(sourceUrl));
        }

        /// <summary>
        /// Parses out a List of ScrapedSongs from the given page text. Also works if the page is for a single song.
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public static List<ScrapedSong> ParseSongsFromPage(string pageText, Uri sourceUrl)
        {
            JObject result = new JObject();
            List<ScrapedSong> songs = new List<ScrapedSong>();
            try
            {
                result = JObject.Parse(pageText);

            }
            catch (JsonReaderException ex)
            {
                Logger?.Exception("Unable to parse JSON from text", ex);
                return songs;
            }
            ScrapedSong newSong;
            int? resultTotal = result["totalDocs"]?.Value<int>();
            if (resultTotal == null) resultTotal = 0;

            // Single song in page text.
            if (resultTotal == 0)
            {
                if (result["key"] != null)
                {
                    newSong = ParseSongFromJson(result, sourceUrl);
                    if (newSong != null)
                    {
                        songs.Add(newSong);
                        return songs;
                    }
                }
                return songs;
            }

            // Array of songs in page text.
            var songJSONAry = result["docs"]?.ToArray();

            if (songJSONAry == null)
            {
                Logger?.Error("Invalid page text: 'songs' field not found.");
                return songs;
            }

            foreach (JObject song in songJSONAry)
            {
                newSong = ParseSongFromJson(song, sourceUrl);
                if (newSong != null)
                    songs.Add(newSong);
            }
            return songs;
        }

        public static ScrapedSong ParseSongFromJson(JObject song, string sourceUrl)
        {
            return ParseSongFromJson(song, Utilities.GetUriFromString(sourceUrl));
        }

        /// <summary>
        /// Creates a SongInfo from a JObject.
        /// </summary>
        /// <param name="song"></param>
        /// <exception cref="ArgumentException">Thrown when a hash can't be found for the given song JObject.</exception>
        /// <returns></returns>
        public static ScrapedSong ParseSongFromJson(JObject song, Uri sourceUrl)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null for BeatSaverReader.ParseSongFromJson.");
            //JSONObject song = (JSONObject) aKeyValue;
            string songKey = song["key"]?.Value<string>();
            string songHash = song["hash"]?.Value<string>().ToUpper();
            var songName = song["name"]?.Value<string>();
            var mapperName = song["uploader"]?["username"]?.Value<string>();
            if (string.IsNullOrEmpty(songHash))
                throw new ArgumentException("Unable to find hash for the provided song, is this a valid song JObject?");
            string downloadUri = !string.IsNullOrEmpty(songKey) ? BEATSAVER_DOWNLOAD_URL_BASE + songKey : string.Empty;
            var newSong = new ScrapedSong(songHash)
            {
                DownloadUri = Utilities.GetUriFromString(downloadUri),
                SourceUri = sourceUrl,
                SongName = songName,
                SongKey = songKey,
                MapperName = mapperName,
                RawData = song.ToString()
            };
            return newSong;
        }

        private static int CalcMaxSongs(int maxPages, int maxSongs)
        {
            int retVal = 0;
            if (maxPages > 0)
                retVal = maxPages * SongsPerPage;
            if (maxSongs > 0)
            {
                if (retVal == 0)
                    retVal = maxSongs;
                else
                    retVal = Math.Min(retVal, maxSongs);
            }
            return retVal;
        }

        private static int CalcMaxPages(int maxPages, int maxSongs)
        {
            int retVal = 0;
            if (maxPages > 0)
                retVal = maxPages;
            if (maxSongs > 0)
            {
                int pagesForSongs = (int)Math.Ceiling(maxSongs / (float)SongsPerPage);
                if (retVal == 0)
                    retVal = pagesForSongs;
                else
                    retVal = Math.Min(retVal, pagesForSongs);
            }
            return retVal;
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
        /// <exception cref="ArgumentNullException">Thrown when _setting is null.</exception>
        public async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings _settings, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (_settings == null)
                throw new ArgumentNullException(nameof(_settings), "Settings cannot be null for BeatSaverReader.GetSongsFromFeedAsync");
            if (!(_settings is BeatSaverFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            FeedResult songs = null;

            switch ((BeatSaverFeed)settings.FeedIndex)
            {
                // Author
                case BeatSaverFeed.Author:
                    songs = await GetSongsByAuthorAsync(settings.Criteria, CalcMaxSongs(settings.MaxPages, settings.MaxSongs)).ConfigureAwait(false);
                    break;
                case BeatSaverFeed.Search:
                    songs = await SearchAsync(settings.Criteria, settings.SearchType).ConfigureAwait(false);
                    break;
                // Latest/Hot/Plays/Downloads
                default:
                    songs = await GetBeatSaverSongsAsync(settings).ConfigureAwait(false);
                    break;
            }

            //Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            //foreach (var song in songs)
            //{
            //    if (!retDict.ContainsKey(song.Hash))
            //    {
            //        retDict.Add(song.Hash, song);
            //    }
            //}
            return songs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
        public static async Task<FeedResult> GetBeatSaverSongsAsync(BeatSaverFeedSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeatSaverReader.GetBeatSaverSongsAsync");
            // TODO: double checks the first page
            int feedIndex = settings.FeedIndex;
            bool useMaxPages = settings.MaxPages != 0;
            bool useMaxSongs = settings.MaxSongs != 0;
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            string pageText = string.Empty;

            JObject result = new JObject();
            var pageUri = GetPageUrl(feedIndex);
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(pageUri).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                }
                result = JObject.Parse(pageText);
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
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }
            catch (JsonReaderException ex)
            {
                string message = "Unable to parse JSON from text on first page in GetBeatSaverSongAsync()";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }
            catch (Exception ex)
            {
                string message = $"Uncaught error getting the first page in BeatSaverReader.GetBeatSaverSongsAsync(): {ex.Message}";
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }
            int? numSongs = result["totalDocs"]?.Value<int>();
            int? lastPage = result["lastPage"]?.Value<int>();
            if (numSongs == null || lastPage == null || numSongs == 0)
            {
                Logger?.Warning($"Error checking Beat Saver's {settings.FeedName} feed.");
                return new FeedResult(null, null, new FeedReaderException($"Error getting the first page in GetBeatSaverSongsAsync()", null, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }
            Logger?.Info($"Checking Beat Saver's {settings.FeedName} feed, {numSongs} songs available");
            int maxPages = settings.MaxPages;
            int pageNum = Math.Max(settings.StartingPage - 1, 0);
            if (pageNum > 0 && useMaxPages)
                maxPages += pageNum; // Add starting page to maxPages so we actually get songs if maxPages < starting page
            List<Task<PageReadResult>> pageReadTasks = new List<Task<PageReadResult>>();
            bool continueLooping = true;
            do
            {
                pageUri = GetPageUrl(feedIndex, pageNum);
                Logger?.Trace($"Creating task for {pageUri.ToString()}");
                pageReadTasks.Add(GetSongsFromPageAsync(pageUri));
                pageNum++;
                if ((pageNum > lastPage))
                    continueLooping = false;
                if (useMaxPages && (pageNum >= maxPages))
                    continueLooping = false;
                if (useMaxSongs && pageNum * SongsPerPage >= settings.MaxSongs)
                    continueLooping = false;
            } while (continueLooping);
            try
            {
                // TODO: Won't hit max songs if a song is added while reading the pages. (It'll bump all the songs down and we'll get a repeat)
                await Task.WhenAll(pageReadTasks.ToArray()).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                string message = $"Error waiting for pageReadTasks: {ex.Message}";
                // TODO: Probably don't need logging here.
                Logger?.Debug(message);
                Logger?.Debug(ex.StackTrace);
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }
            var pageResults = new List<PageReadResult>();
            foreach (var job in pageReadTasks)
            {
                var pageResult = await job.ConfigureAwait(false);
                pageResults.Add(pageResult);
                foreach (var song in pageResult.Songs)
                {
                    if (songs.ContainsKey(song.Hash))
                        continue;
                    if (!useMaxSongs || songs.Count < settings.MaxSongs)
                        songs.Add(song.Hash, song);
                }
            }
            return new FeedResult(songs, pageResults);
        }
        public static async Task<List<string>> GetAuthorNamesByIDAsync(string mapperId)
        {
            List<string> authorNames = new List<string>();
            FeedResult result = await GetSongsByUploaderIdAsync(mapperId).ConfigureAwait(false);
            authorNames = result.Songs.Values.Select(s => s.MapperName).Distinct().ToList();
            //authorNames.ForEach(n => Logger?.Warning($"Found authorName: {n}"));
            return authorNames;
        }
        public static async Task<string> GetAuthorIDAsync(string authorName)
        {
            if (string.IsNullOrEmpty(authorName))
                return string.Empty;
            if (_authors.ContainsKey(authorName))
                return _authors[authorName];
            string mapperId = string.Empty;

            int page = 0;
            int? totalResults;
            Uri sourceUri = null;
            string pageText;
            JObject result;
            JToken matchingSong;
            JToken[] songJSONAry;
            do
            {
                Logger?.Debug($"Checking page {page + 1} for the author ID.");
                sourceUri = new Uri(Feeds[BeatSaverFeed.Search].BaseUrl.Replace(SEARCHKEY, authorName).Replace(PAGEKEY, (page * SongsPerPage).ToString()));
                result = new JObject();
                try
                {
                    using (var response = await WebUtils.GetBeatSaverAsync(sourceUri).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    }
                    result = JObject.Parse(pageText);
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
                    Logger?.Error($"{errorText} getting UploaderID from author name, {sourceUri} responded with {ex.Response?.StatusCode}:{ex.Response?.ReasonPhrase}");
                    return string.Empty;
                }
                catch (JsonReaderException ex)
                {
                    // TODO: Should I break the loop here, or keep trying?
                    Logger?.Exception("Unable to parse JSON from text", ex);
                }
                catch (Exception ex)
                {
                    Logger?.Error($"Uncaught error getting UploaderID from author name {authorName}");
                    return string.Empty;
                }
                totalResults = result["totalDocs"]?.Value<int>(); // TODO: Check this
                if (totalResults == null || totalResults == 0)
                {
                    Logger?.Warning($"No songs by {authorName} found, is the name spelled correctly?");
                    return string.Empty;
                }
                songJSONAry = result["docs"].ToArray();
                matchingSong = (JObject)songJSONAry.FirstOrDefault(c => c["uploader"]?["username"]?.Value<string>()?.ToLower() == authorName.ToLower());

                page++;
                sourceUri = new Uri(Feeds[BeatSaverFeed.Search].BaseUrl.Replace(SEARCHKEY, authorName).Replace(PAGEKEY, (page * SongsPerPage).ToString()));
            } while ((matchingSong == null) && page * SongsPerPage < totalResults);


            if (matchingSong == null)
            {
                Logger?.Warning($"No songs by {authorName} found, is the name spelled correctly?");
                return string.Empty;
            }
            mapperId = matchingSong["uploader"]["_id"].Value<string>();
            _authors.TryAdd(authorName, mapperId);

            return mapperId;
        }

        public static async Task<PageReadResult> GetSongsFromPageAsync(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri), "uri cannot be null in BeatSaverReader.GetSongsFromPageAsync.");
            string pageText = string.Empty;
            var songs = new List<ScrapedSong>();
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (WebClientException ex)
            {
                return PageReadResult.FromWebClientException(ex, uri);
            }

            foreach (var song in ParseSongsFromPage(pageText, uri))
            {
                songs.Add(song);
            }
            return new PageReadResult(uri, songs);
        }
        /// <summary>
        /// Gets a list of songs by an author with the provided ID (NOT the author's username).
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        public static async Task<FeedResult> GetSongsByUploaderIdAsync(string authorId, int maxSongs = 0)
        {
            int feedIndex = 0;
            var songDict = new Dictionary<string, ScrapedSong>();
            string pageText = string.Empty;
            Uri uri = GetPageUrl(feedIndex, 0, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } });
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        string message = $"Error getting songs by UploaderId, {uri?.ToString()} responded with {response.StatusCode}:{response.ReasonPhrase}";
                        Logger?.Error(message);
                        return new FeedResult(null, null, new FeedReaderException(message, null, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = $"Error getting songs by UploaderId, {authorId}, from {uri}";
                Logger?.Exception(message, ex);
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }

            JObject result;
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (JsonReaderException ex)
            {
                string message = "Unable to parse JSON from text";
                Logger?.Exception(message, ex);
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            }

            int numSongs = result["totalDocs"]?.Value<int>() ?? 0; // Check this
            int lastPage = result["lastPage"]?.Value<int>() ?? 0;
            // TODO: Redo this using TransformBlock
            if (maxSongs > 0)
                lastPage = Math.Min(lastPage, maxSongs / SongsPerPage + 1);
            Logger?.Debug($"{numSongs} songs by {authorId} available on Beat Saver");
            int pageNum = 0;
            List<Task<PageReadResult>> pageReadTasks = new List<Task<PageReadResult>>();
            do
            {
                uri = GetPageUrl(feedIndex, pageNum, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } });
                //Logger?.Trace($"Creating task for {uri}");
                pageReadTasks.Add(GetSongsFromPageAsync(uri));
                pageNum++;
            } while (pageNum <= lastPage);

            await Task.WhenAll(pageReadTasks.ToArray()).ConfigureAwait(false);
            var pageResults = new List<PageReadResult>();
            foreach (var job in pageReadTasks)
            {
                var pageResult = await job.ConfigureAwait(false);
                pageResults.Add(pageResult);
                foreach (var song in pageResult.Songs)
                {
                    if (songDict.Count < maxSongs && !songDict.ContainsKey(song.Hash))
                        songDict.Add(song.Hash, song);
                }
                //songs.AddRange(await job.ConfigureAwait(false));
            }
            return new FeedResult(songDict, pageResults);
        }

        public static async Task<FeedResult> GetSongsByAuthorAsync(string uploader, int maxSongs = 0)
        {
            string mapperId = await GetAuthorIDAsync(uploader).ConfigureAwait(false);
            if (string.IsNullOrEmpty(mapperId))
                return new FeedResult(null, null, new FeedReaderException($"Unable to find a mapper ID for uploader {uploader}", null, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
            return await GetSongsByUploaderIdAsync(mapperId, maxSongs).ConfigureAwait(false);
        }
        public static async Task<ScrapedSong> GetSongByHashAsync(string hash)
        {
            var uri = new Uri(BEATSAVER_GETBYHASH_BASE_URL + hash);
            string pageText = "";
            ScrapedSong song = null;
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger?.Error($"Error getting song by hash, {uri.ToString()} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return song;
                    }
                }
                if (string.IsNullOrEmpty(pageText))
                {
                    Logger?.Warning($"Unable to get web page at {uri.ToString()}");
                    return null;
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
                Logger?.Error($"{errorText} while trying to populate fields for {hash}");
                return null;
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {hash}");
            }
            catch (Exception ex)
            {
                Logger?.Exception($"Exception getting page {uri.ToString()}", ex);
                throw;
            }
            song = ParseSongsFromPage(pageText, uri).FirstOrDefault();
            return song;
        }

        public static async Task<ScrapedSong> GetSongByKeyAsync(string key)
        {
            var uri = new Uri(BEATSAVER_DETAILS_BASE_URL + key);
            string pageText = "";
            ScrapedSong song = null;
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        Logger?.Error($"Error getting song by key, {uri} responded with {response.StatusCode}:{response.ReasonPhrase}");
                        return song;
                    }
                }
                if (string.IsNullOrEmpty(pageText))
                {
                    Logger?.Warning($"Unable to get web page at {uri}");
                    return null;
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
                Logger?.Error($"{errorText} while trying to populate fields for {key}");
                return null;
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {key}");
            }
            catch (Exception ex)
            {
                Logger?.Exception($"Exception getting page {uri}", ex);
                throw;
            }
            song = ParseSongsFromPage(pageText, uri).FirstOrDefault();
            return song;
        }

        /// <summary>
        /// Searches for songs with the specified criteria and search type and returns them in a FeedResult.
        /// TODO: PageResults is always an empty array.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="type"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static async Task<FeedResult> SearchAsync(string criteria, SearchType type, BeatSaverFeedSettings settings = null)
        {
            // TODO: Hits rate limit
            if (type == SearchType.key)
            {
                var songDict = new Dictionary<string, ScrapedSong>();
                var song = await GetSongByKeyAsync(criteria).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(song?.Hash))
                    songDict.Add(song.Hash, song);
                return new FeedResult(songDict, null);
            }

            if (type == SearchType.user)
            {
                return await GetSongsByUploaderIdAsync((await GetAuthorNamesByIDAsync(criteria).ConfigureAwait(false)).FirstOrDefault()).ConfigureAwait(false);
            }

            if (type == SearchType.hash)
            {
                var songDict = new Dictionary<string, ScrapedSong>();
                var song = await GetSongByHashAsync(criteria).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(song?.Hash))
                    songDict.Add(song.Hash, song);
                return new FeedResult(songDict, null);
            }
            StringBuilder url;
            int maxSongs = 0;
            int maxPages = 0;
            //int lastPage;
            //int nextPage;
            int pageIndex = 0;
            if (settings != null)
            {
                maxSongs = settings.MaxSongs;
                maxPages = settings.MaxPages;
                pageIndex = Math.Max(settings.StartingPage - 1, 0);
            }
            bool useMaxPages = maxPages > 0;
            bool useMaxSongs = maxSongs > 0;
            if (useMaxPages && pageIndex > 0)
                maxPages = maxPages + pageIndex;
            bool continueLooping = true;
            var songs = new Dictionary<string, ScrapedSong>();
            List<ScrapedSong> newSongs;
            do
            {
                url = new StringBuilder(Feeds[BeatSaverFeed.Search].BaseUrl);
                url.Replace(SEARCHTYPEKEY, type.ToString());
                url.Replace(SEARCHKEY, criteria);
                url.Replace(PAGEKEY, pageIndex.ToString());
                var uri = new Uri(url.ToString());
                string pageText = string.Empty;
                // TODO: Should probably wrap using in a try/catch
                using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
                {
                    Logger?.Debug($"Checking {uri} for songs.");
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        string message = $"Error searching for song, {uri} responded with {response.StatusCode}:{response.ReasonPhrase}";
                        Logger?.Error(message);
                        return new FeedResult(null, null, new FeedReaderException(message, null, FeedReaderFailureCode.SourceFailed), FeedResultErrorLevel.Error);
                    }
                }
                newSongs = ParseSongsFromPage(pageText, uri);
                foreach (var song in newSongs)
                {
                    if (songs.ContainsKey(song.Hash))
                        continue;
                    if (!useMaxSongs || songs.Count < maxSongs)
                        songs.Add(song.Hash, song);
                }
                pageIndex++;
                if (newSongs.Count == 0)
                    continueLooping = false;
                if (useMaxPages && (pageIndex >= maxPages))
                    continueLooping = false;
                if (useMaxSongs && pageIndex * SongsPerPage >= maxSongs)
                    continueLooping = false;
            } while (continueLooping);

            return new FeedResult(songs, null);
        }

        [Obsolete("This isn't even finished.")]
        public static async Task<List<JToken>> ScrapeBeatSaver(int timeBetweenRequests, DateTime? stopAtDate = null)
        {
            throw new NotImplementedException("Not finished");
            List<JToken> songs = null;
            string pageText = string.Empty;
            using (var response = await WebUtils.GetBeatSaverAsync(GetPageUrl(BeatSaverFeed.Latest)).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                else
                    return songs;
            }

            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (JsonReaderException ex)
            {
                Logger?.Exception("Unable to parse JSON from text", ex);
            }
            int? totalSongs = result["totalDocs"]?.Value<int>();
            int? lastPage = result["lastPage"]?.Value<int>();
            if (totalSongs == null || lastPage == null || totalSongs == 0)
            {
                Logger?.Warning($"Error scraping Beat Saver.");
                return songs;
            }

            return songs;

        }

        #endregion

        #region Sync
        /// <summary>
        /// Retrieves the songs from a feed with the given settings in the form of a Dictionary, with the key being the song's hash and a ScrapedSong as the value.
        /// </summary>
        /// <param name="_settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeatSaverFeedSettings</exception>
        /// <returns></returns>
        public FeedResult GetSongsFromFeed(IFeedSettings _settings)
        {
            return GetSongsFromFeedAsync(_settings).Result;
        }
        public static FeedResult GetBeatSaverSongs(BeatSaverFeedSettings settings)
        {
            return GetBeatSaverSongsAsync(settings).Result;
        }

        public static List<string> GetAuthorNamesByID(string mapperId)
        {
            return GetAuthorNamesByIDAsync(mapperId).Result;
        }
        public static string GetAuthorID(string authorName)
        {
            return GetAuthorIDAsync(authorName).Result;
        }
        public static PageReadResult GetSongsFromPage(Uri uri)
        {
            return GetSongsFromPageAsync(uri).Result;
        }
        public static PageReadResult GetSongsFromPage(string url)
        {
            return GetSongsFromPageAsync(Utilities.GetUriFromString(url)).Result;
        }
        [Obsolete("Check this")]
        public static FeedResult GetSongsByUploaderId(string authorId)
        {
            return GetSongsByUploaderIdAsync(authorId).Result;
        }
        /// <summary>
        /// Searches Beat Saver and retrieves all songs by the provided uploader name.
        /// </summary>
        /// <param name="uploader"></param>
        /// <returns></returns>
        public static FeedResult GetSongsByAuthor(string uploader)
        {
            return GetSongsByAuthorAsync(uploader).Result;
        }
        public static ScrapedSong GetSongByHash(string hash)
        {
            return GetSongByHashAsync(hash).Result;
        }
        public static ScrapedSong GetSongByKey(string key)
        {
            return GetSongByKeyAsync(key).Result;
        }
        public static FeedResult Search(string criteria, SearchType type)
        {
            return SearchAsync(criteria, type).Result;
        }


        #endregion
        #endregion

        #region Overloads
        public async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }

        public static Task<PageReadResult> GetSongsFromPageAsync(string url)
        {
            return GetSongsFromPageAsync(Utilities.GetUriFromString(url));
        }

        public static Uri GetPageUrl(int feedIndex, int pageIndex = 0, Dictionary<string, string> replacements = null)
        {
            return GetPageUrl((BeatSaverFeed)feedIndex, pageIndex, replacements);
        }

        #endregion

        public enum SearchType
        {
            author, // author name (not necessarily uploader)
            name, // song name only
            user, // user (uploader) name
            hash, // MD5 Hash
            song, // song name, song subname, author 
            key,
            all // name, user, song
        }

    }

    public class BeatSaverFeedSettings : IFeedSettings
    {
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return BeatSaverReader.Feeds[Feed].Name; } } // Name of the chosen feed
        public BeatSaverFeed Feed { get { return (BeatSaverFeed)FeedIndex; } set { FeedIndex = (int)value; } } // Which feed to use
        public int FeedIndex { get; private set; } // Which feed to use

        /// <summary>
        /// Additional feed criteria, used for Search and Author feed.
        /// </summary>
        public string Criteria { get; set; }

        /// <summary>
        /// Type of search to perform, only used for SEARCH feed.
        /// Default is 'song' (song name, song subname, author)
        /// </summary>
        public BeatSaverReader.SearchType SearchType { get; set; }

        public int SongsPerPage { get { return BeatSaverReader.SongsPerPage; } }

        /// <summary>
        /// Maximum songs to retrieve, will stop the reader before MaxPages is met. Use 0 for unlimited.
        /// </summary>
        public int MaxSongs { get; set; }

        /// <summary>
        /// Maximum pages to check, will stop the reader before MaxSongs is met. Use 0 for unlimited.
        /// </summary>
        public int MaxPages { get; set; }

        /// <summary>
        /// Page of the feed to start on, default is 1. For all feeds, setting '1' here is the same as starting on the first page.
        /// </summary>
        public int StartingPage { get; set; }

        public BeatSaverFeedSettings(int feedIndex)
        {
            FeedIndex = feedIndex;
            MaxPages = 0;
            StartingPage = 1;
            SearchType = BeatSaverReader.SearchType.song;
        }
    }

    public enum BeatSaverFeed
    {
        Author = 0,
        Latest = 1,
        Hot = 2,
        Plays = 3,
        Downloads = 4,
        Search = 98,
    }
}
