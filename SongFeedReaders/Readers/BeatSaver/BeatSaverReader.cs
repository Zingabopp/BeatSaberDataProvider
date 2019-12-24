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
using SongFeedReaders.DataflowAlternative;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class BeatSaverReader : IFeedReader
    {
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

        public static string NameKey => "BeatSaverReader";
        public static readonly string SourceKey = "BeatSaver";
        public static readonly Uri ReaderRootUri = new Uri("https://beatsaver.com");

        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }

        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public Uri RootUri => ReaderRootUri;
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        public int MaxConcurrency { get; set; }

        private static ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        // { (BeatSaverFeeds)99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }

        public BeatSaverReader()
        {
            MaxConcurrency = 3;
        }

        public BeatSaverReader(int maxConcurrency)
        {
            if (maxConcurrency < 1)
            {
                Logger?.Warning($"{nameof(maxConcurrency)} cannot be less than 1, using 1 instead.");
                MaxConcurrency = 1;
            }
            else
                MaxConcurrency = maxConcurrency;
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
            int pageIndex = settings.StartingPage;
            int maxPages = settings.MaxPages;
            int maxSongs = settings.MaxSongs;
            bool useMaxPages = maxPages != 0;
            bool useMaxSongs = maxSongs != 0;

            int estimatedPageResults = Math.Min(useMaxSongs ? maxSongs / 10 : int.MaxValue, useMaxPages ? maxPages : int.MaxValue);
            if (pageIndex > 1 && useMaxPages)
                maxPages += pageIndex - 1; // Add starting page to maxPages so we actually get songs if maxPages < starting page
            var feed = new BeatSaverFeed(settings);
            try
            {
                feed.EnsureValidSettings();
            }
            catch (InvalidFeedSettingsException ex)
            {
                return new FeedResult(null, null, ex, FeedResultError.Error);
            }
            var feedEnum = feed.GetEnumerator();
            List<PageReadResult> pageResults;
            Dictionary<string, ScrapedSong> newSongs = new Dictionary<string, ScrapedSong>();
            PageReadResult firstPage = await feedEnum.MoveNextAsync().ConfigureAwait(false);
            pageIndex++;
            bool continueLooping = true;
            if (!firstPage.Successful)
            {
                pageResults = new List<PageReadResult>(1) { firstPage };
                string message = $"Error getting first page {firstPage.Uri}: {firstPage.PageError.ToString()}";
                Logger?.Error(message);
                if (firstPage.Exception != null)
                {
                    Logger?.Error(firstPage.Exception.Message);
                    Logger?.Debug(firstPage.Exception.ToString());
                }
                if (firstPage.Exception is FeedReaderException feedReaderException)
                    return new FeedResult(null, pageResults, feedReaderException, FeedResultError.Error);
                else
                    return new FeedResult(null, pageResults, new FeedReaderException(message, firstPage.Exception, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
            }

            bool lastPageKnown = false;
            int lastPage = 0;
            if (firstPage is BeatSaverPageResult bsPage && bsPage.LastPage > 0)
            {
                lastPageKnown = true;
                lastPage = bsPage.LastPage;
                if (useMaxPages)
                    maxPages = Math.Min(maxPages, lastPage);
                else
                    maxPages = lastPage;

                estimatedPageResults = Math.Min(estimatedPageResults, lastPage - pageIndex + 3);
            }
            if (estimatedPageResults == int.MaxValue)
                estimatedPageResults = 10;
            pageResults = new List<PageReadResult>(estimatedPageResults) { firstPage };
            foreach (var song in firstPage.Songs)
            {
                if (!newSongs.ContainsKey(song.Hash))
                    newSongs.Add(song.Hash, song);
                if (useMaxSongs && newSongs.Count >= maxSongs)
                {
                    continueLooping = false;
                    break;
                }
            }
            if (firstPage.Count > 0)
                Logger?.Debug($"Receiving {firstPage.Count} potential songs from {firstPage.Uri}");
            else
                Logger?.Debug($"Did not find any songs in {Name}.{settings.FeedName}.");
            if (pageIndex > maxPages)
                continueLooping = false;
            if (!continueLooping)
                return new FeedResult(newSongs, pageResults);
            var ProcessPageBlock = new TransformBlock<Task<PageReadResult>, PageReadResult>(async pageTask =>
            {
                return await pageTask.ConfigureAwait(false);

            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = MaxConcurrency,
                BoundedCapacity = MaxConcurrency,
                CancellationToken = cancellationToken
                //#if NETSTANDARD
                //                , EnsureOrdered = true
                //#endif
            });
            int itemsInBlock = 0;

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
                    await ProcessPageBlock.SendAsync(feedEnum.MoveNextAsync(cancellationToken), cancellationToken).ConfigureAwait(false); // TODO: Need check with SongsPerPage
                    itemsInBlock++;
                    pageIndex++;

                    if ((useMaxPages && pageIndex > maxPages) || cancellationToken.IsCancellationRequested)
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
                                Logger?.Debug($"Did not find any songs in {Name}.{settings.FeedName}.");

                            // TODO: Process PageReadResults for better error feedback.
                            foreach (var song in pageResult.Songs)
                            {
                                if (!newSongs.ContainsKey(song.Hash))
                                {
                                    if (newSongs.Count < settings.MaxSongs || settings.MaxSongs == 0)
                                        newSongs.Add(song.Hash, song);
                                    if (newSongs.Count >= settings.MaxSongs && useMaxSongs)
                                        continueLooping = false;
                                }
                            }
                            if (!useMaxPages || pageIndex <= maxPages)
                                if (newSongs.Count < settings.MaxSongs)
                                    continueLooping = true;
                        }
                    }
                }
            }
            while (continueLooping);
            return new FeedResult(newSongs, pageResults);
        }

        public async Task<List<string>> GetAuthorNamesByIDAsync(string mapperId, CancellationToken cancellationToken)
        {
            List<string> authorNames = new List<string>();
            var query = new SearchQueryBuilder(BeatSaverSearchType.user, mapperId);
            BeatSaverFeedSettings settings = new BeatSaverFeedSettings(BeatSaverFeedName.Search) { SearchQuery = query.GetQuery() };
            FeedResult result = await GetSongsFromFeedAsync(settings, cancellationToken).ConfigureAwait(false);
            authorNames = result.Songs.Values.Select(s => s.MapperName).Distinct().ToList();
            //authorNames.ForEach(n => Logger?.Warning($"Found authorName: {n}"));
            throw new NotImplementedException();
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
            return new PageReadResult(uri, retList, 0, exception, pageError);
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

        public List<string> GetAuthorNamesByID(string mapperId)
        {
            return GetAuthorNamesByIDAsync(mapperId, CancellationToken.None).Result;
        }

        public static string GetAuthorID(string authorName)
        {
            return GetAuthorIDAsync(authorName, CancellationToken.None).Result;
        }

        public static PageReadResult GetSongByHash(string hash)
        {
            return GetSongByHashAsync(hash, CancellationToken.None).Result;
        }

        #endregion
        #endregion

    }
}
