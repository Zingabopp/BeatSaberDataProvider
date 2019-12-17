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

namespace SongFeedReaders.Readers.BeatSaver
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
        public static readonly Uri ReaderRootUri = new Uri("https://beatsaver.com");
        public Uri RootUri => ReaderRootUri;
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }
        #region Constants
        //private static readonly string AUTHORKEY = "{AUTHOR}";
        private const string AUTHORIDKEY = "{AUTHORID}";
        private const string PAGEKEY = "{PAGE}";
        private const string SEARCHTYPEKEY = "{SEARCHTYPE}"; // text or advanced
        private const string SEARCHQUERY = "{SEARCHQUERY}";
        public static readonly int SongsPerPage = 10;
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Remove unused private members
        private const string BEATSAVER_NIGHTLYDUMP_URL = "https://beatsaver.com/api/download/dump/maps";
#pragma warning restore CA1823 // Remove unused private members
#pragma warning restore IDE0051 // Remove unused private members

        private const string DescriptionAuthor = "Retrieves songs by the specified map author.";
        private const string DescriptionLatest = "Retrieves the latest beatmaps posted to BeatSaver.";
        private const string DescriptionHot = "Retrieves songs ordered from most popular to least popular as determined by BeatSaver.";
        private const string DescriptionPlays = "Retrieves the songs with the highest number of plays. (Play count is no longer updated by BeatSaver)";
        private const string DescriptionDownloads = "Retrieves songs ordered from most downloaded to least downloaded.";
        private const string DescriptionSearch = "Retrieves songs matching the provided search criteria from BeatSaver.";
        #endregion

        private static ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        // { (BeatSaverFeeds)99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }

        

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
            return BeatSaverFeed.Feeds[ssSettings.Feed].DisplayName;
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
            Uri downloadUri = Utilities.GetDownloadUriByHash(songHash);
            var newSong = new ScrapedSong(songHash)
            {
                DownloadUri = downloadUri,
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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="_settings"/> is null.</exception>
        public async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings _settings, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (_settings == null)
                throw new ArgumentNullException(nameof(_settings), "Settings cannot be null for BeatSaverReader.GetSongsFromFeedAsync");
            if (!(_settings is BeatSaverFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            FeedResult songs = null;

            switch ((BeatSaverFeedName)settings.FeedIndex)
            {
                // Author
                case BeatSaverFeedName.Author:
                    songs = await GetSongsByAuthorAsync(settings.Criteria, cancellationToken, CalcMaxSongs(settings.MaxPages, settings.MaxSongs)).ConfigureAwait(false);
                    break;
                case BeatSaverFeedName.Search:
                    songs = await SearchAsync(settings, cancellationToken).ConfigureAwait(false);
                    break;
                // Latest/Hot/Plays/Downloads
                default:
                    songs = await GetBeatSaverSongsAsync(settings, cancellationToken).ConfigureAwait(false);
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
        public static async Task<FeedResult> GetBeatSaverSongsAsync(BeatSaverFeedSettings settings, CancellationToken cancellationToken)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeatSaverReader.GetBeatSaverSongsAsync");
            if (cancellationToken.IsCancellationRequested)
                return FeedResult.CancelledResult;
            // TODO: double checks the first page
            int feedIndex = settings.FeedIndex;
            bool useMaxPages = settings.MaxPages != 0;
            bool useMaxSongs = settings.MaxSongs != 0;
            Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
            string pageText = string.Empty;

            JObject result = new JObject();
            var pageUri = GetPageUrl(feedIndex);
            IWebResponseMessage response = null;
            try
            {
                response = await GetBeatSaverAsync(pageUri, cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
            }
            catch (JsonReaderException ex)
            {
                string message = "Unable to parse JSON from text on first page in GetBeatSaverSongAsync()";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
            }
            catch (OperationCanceledException)
            {
                return FeedResult.CancelledResult;
            }
            catch (Exception ex)
            {
                string message = $"Uncaught error getting the first page in BeatSaverReader.GetBeatSaverSongsAsync(): {ex.Message}";
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
            int? numSongs = result["totalDocs"]?.Value<int>();
            int? lastPage = result["lastPage"]?.Value<int>();
            if (numSongs == null || lastPage == null || numSongs == 0)
            {
                Logger?.Warning($"Error checking Beat Saver's {settings.FeedName} feed.");
                return new FeedResult(null, null, new FeedReaderException($"Error getting the first page in GetBeatSaverSongsAsync()", null, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
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
                pageReadTasks.Add(GetSongsFromPageAsync(pageUri, cancellationToken));
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
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
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
        public static async Task<List<string>> GetAuthorNamesByIDAsync(string mapperId, CancellationToken cancellationToken)
        {
            List<string> authorNames = new List<string>();
            FeedResult result = await GetSongsByUploaderIdAsync(mapperId, cancellationToken).ConfigureAwait(false);
            authorNames = result.Songs.Values.Select(s => s.MapperName).Distinct().ToList();
            //authorNames.ForEach(n => Logger?.Warning($"Found authorName: {n}"));
            return authorNames;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorName"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public static async Task<string> GetAuthorIDAsync(string authorName, CancellationToken cancellationToken)
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
            var queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.author, authorName);

            do
            {
                Logger?.Debug($"Checking page {page + 1} for the author ID.");
                sourceUri = new Uri(BeatSaverFeed.Feeds[BeatSaverFeedName.Search].BaseUrl.Replace(SEARCHTYPEKEY, "advanced").Replace(SEARCHQUERY, queryBuilder.GetQueryString()).Replace(PAGEKEY, (page * SongsPerPage).ToString()));
                result = new JObject();
                IWebResponseMessage response = null;
                try
                {
                    response = await WebUtils.GetBeatSaverAsync(sourceUri, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger?.Error($"Uncaught error getting UploaderID from author name {authorName}");
                    Logger?.Debug($"{ex}");
                    return string.Empty;
                }
                finally
                {
                    response?.Dispose();
                    response = null;
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
                sourceUri = new Uri(BeatSaverFeed.Feeds[BeatSaverFeedName.Search].BaseUrl.Replace(SEARCHQUERY, authorName).Replace(PAGEKEY, (page * SongsPerPage).ToString()));
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

        /// <summary>
        /// Gets a list of songs by an author with the provided ID (NOT the author's username).
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        public static async Task<FeedResult> GetSongsByUploaderIdAsync(string authorId, CancellationToken cancellationToken, int maxSongs = 0)
        {
            int feedIndex = 0;
            var songDict = new Dictionary<string, ScrapedSong>();
            string pageText = string.Empty;
            Uri uri = GetPageUrl(feedIndex, 0, new Dictionary<string, string>() { { AUTHORIDKEY, authorId } });
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    else
                    {
                        string message = $"Error getting songs by UploaderId, {uri?.ToString()} responded with {response.StatusCode}:{response.ReasonPhrase}";
                        Logger?.Error(message);
                        return new FeedResult(null, null, new FeedReaderException(message, null, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                string message = $"Operation canceled getting songs by UploaderId, {authorId}, from {uri}";
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.Cancelled), FeedResultError.Cancelled);
            }
            catch (Exception ex)
            {
                string message = $"Error getting songs by UploaderId, {authorId}, from {uri}";
                Logger?.Exception(message, ex);
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
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
                return new FeedResult(null, null, new FeedReaderException(message, ex, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
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
                pageReadTasks.Add(GetSongsFromPageAsync(uri, cancellationToken));
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

        public static async Task<FeedResult> GetSongsByAuthorAsync(string uploader, CancellationToken cancellationToken, int maxSongs = 0)
        {
            try
            {
                string mapperId = await GetAuthorIDAsync(uploader, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(mapperId))
                    return new FeedResult(null, null, new FeedReaderException($"Unable to find a mapper ID for uploader {uploader}", null, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
                return await GetSongsByUploaderIdAsync(mapperId, cancellationToken, maxSongs).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return FeedResult.CancelledResult;
            }
        }
        public static async Task<PageReadResult> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {
            var uri = Utilities.GetBeatSaverDetailsByHash(hash);
            ScrapedSong song = null;
            var pageError = PageErrorType.None;
            Exception exception = null;
            IWebResponseMessage response = null;
            try
            {
                response = await WebUtils.GetBeatSaverAsync(uri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                song = ParseSongsFromPage(pageText, uri).FirstOrDefault();
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
                string message = $"{errorText} while trying to populate fields for {hash}";
                Logger?.Error(message);
                exception = new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed);
            }
            catch (OperationCanceledException ex)
            {
                exception = new FeedReaderException(ex.Message, ex, FeedReaderFailureCode.Cancelled);
            }
            catch (AggregateException ae)
            {
                string message = $"Exception while trying to get details for {hash}";
                ae.WriteExceptions(message);
                exception = new FeedReaderException(message, ae, FeedReaderFailureCode.Generic);
            }
            catch (Exception ex)
            {
                string message = $"Exception getting page {uri.ToString()}";
                Logger?.Exception(message, ex);
                exception = new FeedReaderException(message, ex, FeedReaderFailureCode.Generic);
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
            List<ScrapedSong> retList = new List<ScrapedSong>();
            if (song != null)
                retList.Add(song);
            return new PageReadResult(uri, retList, page, exception, pageError);
        }

        public static async Task<ScrapedSong> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {
            var uri = Utilities.GetBeatSaverDetailsByKey(key);
            string pageText = "";
            ScrapedSong song = null;
            IWebResponseMessage response = null;
            try
            {
                response = await WebUtils.GetBeatSaverAsync(uri, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                else
                {
                    Logger?.Error($"Error getting song by key, {uri} responded with {response.StatusCode}:{response.ReasonPhrase}");
                    return song;
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
                        case 404:
                            errorText = $"Song {key} was not found on BeatSaver.";
                            break;
                        case 408:
                            errorText = "Timeout while trying to populate fields for {key}";
                            break;
                        default:
                            errorText = "Site Error while trying to populate fields for {key}";
                            break;
                    }
                }
                Logger?.Error(errorText);
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
            finally
            {
                response?.Dispose();
                response = null;
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
        public static async Task<FeedResult> SearchAsync(BeatSaverFeedSettings settings, CancellationToken cancellationToken)
        {
            if (settings.SearchType == BeatSaverSearchType.key)
            {
                var songDict = new Dictionary<string, ScrapedSong>();
                var song = await GetSongByKeyAsync(settings.Criteria, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(song?.Hash))
                    songDict.Add(song.Hash, song);
                return new FeedResult(songDict, null);
            }

            if (settings.SearchType == BeatSaverSearchType.user)
            {
                var authorNames = await GetAuthorNamesByIDAsync(settings.Criteria, cancellationToken).ConfigureAwait(false);
                return await GetSongsByUploaderIdAsync(authorNames.FirstOrDefault(), cancellationToken).ConfigureAwait(false);
            }

            if (settings.SearchType == BeatSaverSearchType.hash)
            {
                var songDict = new Dictionary<string, ScrapedSong>();
                var result = await GetSongByHashAsync(settings.Criteria, cancellationToken).ConfigureAwait(false);
                var song = result.Songs.FirstOrDefault();
                if (!string.IsNullOrEmpty(song?.Hash))
                    songDict.Add(song.Hash, song);
                return new FeedResult(songDict, new List<PageReadResult>(1) { result });
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
            PageReadResult newSongs;
            List<PageReadResult> pageResults = new List<PageReadResult>();
            do
            {
                url = new StringBuilder(BeatSaverFeed.Feeds[BeatSaverFeedName.Search].BaseUrl);
                url.Replace(SEARCHTYPEKEY, settings.SearchType == BeatSaverSearchType.all ? "text" : "advanced");
                url.Replace(SEARCHQUERY, settings.Criteria);
                url.Replace(PAGEKEY, pageIndex.ToString());
                var uri = new Uri(url.ToString());
                newSongs = await GetSongsFromPageAsync(uri, cancellationToken).ConfigureAwait(false);
                if (!useMaxSongs || songs.Count < maxSongs)
                    pageResults.Add(newSongs);
                foreach (var song in newSongs.Songs)
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

            return new FeedResult(songs, pageResults);
        }

        [Obsolete("This isn't even finished.")]
        public static async Task<List<JToken>> ScrapeBeatSaver(int timeBetweenRequests, CancellationToken cancellationToken, DateTime? stopAtDate = null)
        {
            throw new NotImplementedException("Not finished");
           

        }

        #endregion

        #endregion

        #region Overloads
        public async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return await GetSongsFromFeedAsync(settings, CancellationToken.None).ConfigureAwait(false);
        }

        #region Sync

        public FeedResult GetSongsFromFeed(IFeedSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                return GetSongsFromFeedAsync(settings, cancellationToken).Result;
            }
            catch (AggregateException ex)
            {
                var flattened = ex.Flatten();
                if (flattened.InnerExceptions.Count == 1)
                {
                    throw flattened.InnerException;
                }
                throw ex;
            }
        }

        public FeedResult GetSongsFromFeed(IFeedSettings settings)
        {
            try
            {
                return GetSongsFromFeedAsync(settings, CancellationToken.None).Result;
            }
            catch (AggregateException ex)
            {
                var flattened = ex.Flatten();
                if (flattened.InnerExceptions.Count == 1)
                {
                    throw flattened.InnerException;
                }
                throw ex;
            }
        }

        public static FeedResult GetBeatSaverSongs(BeatSaverFeedSettings settings)
        {
            return GetBeatSaverSongsAsync(settings, CancellationToken.None).Result;
        }

        public static List<string> GetAuthorNamesByID(string mapperId)
        {
            return GetAuthorNamesByIDAsync(mapperId, CancellationToken.None).Result;
        }

        public static string GetAuthorID(string authorName)
        {
            return GetAuthorIDAsync(authorName, CancellationToken.None).Result;
        }

        [Obsolete("Check this")]
        public static FeedResult GetSongsByUploaderId(string authorId)
        {
            return GetSongsByUploaderIdAsync(authorId, CancellationToken.None).Result;
        }

        /// <summary>
        /// Searches Beat Saver and retrieves all songs by the provided uploader name.
        /// </summary>
        /// <param name="uploader"></param>
        /// <returns></returns>
        public static FeedResult GetSongsByAuthor(string uploader)
        {
            return GetSongsByAuthorAsync(uploader, CancellationToken.None).Result;
        }

        public static PageReadResult GetSongByHash(string hash)
        {
            return GetSongByHashAsync(hash, CancellationToken.None).Result;
        }

        public static ScrapedSong GetSongByKey(string key)
        {
            return GetSongByKeyAsync(key, CancellationToken.None).Result;
        }

        public static FeedResult Search(BeatSaverFeedSettings settings)
        {
            return SearchAsync(settings, CancellationToken.None).Result;
        }

        public static FeedResult Search(BeatSaverFeedSettings settings, CancellationToken cancellationToken)
        {
            return SearchAsync(settings, cancellationToken).Result;
        }


        #endregion
        #endregion

    }
}
