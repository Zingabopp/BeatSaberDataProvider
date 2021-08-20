using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongFeedReaders.Data;
using SongFeedReaders.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class BeatSaverReader : FeedReaderBase
    {
        #region Constants
        //private static readonly string AUTHORKEY = "{AUTHOR}";
        private const string AUTHORIDKEY = "{AUTHORID}";
        private const string PAGEKEY = "{PAGE}";
        private const string SEARCHTYPEKEY = "{SEARCHTYPE}"; // text or advanced
        private const string SEARCHQUERY = "{SEARCHQUERY}";
        public static readonly int SongsPerPage = 20;
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeatSaverFeedSettings.";
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Remove unused private members
        //private const string BEATSAVER_NIGHTLYDUMP_URL = "https://beatsaver.com/api/download/dump/maps";
#pragma warning restore CA1823 // Remove unused private members
#pragma warning restore IDE0051 // Remove unused private members
        #endregion

        public static string NameKey => "BeatSaverReader";
        public static readonly string SourceKey = "BeatSaver";
        public static Uri ReaderRootUri => WebUtils.BeatSaverUri;

        private static FeedReaderLoggerBase? _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }

        public override string Name { get { return NameKey; } }
        public override string Source { get { return SourceKey; } }
        public override Uri RootUri => ReaderRootUri;
        public override bool Ready { get; protected set; }

        private static ConcurrentDictionary<string, string> _authors = new ConcurrentDictionary<string, string>();
        // { (BeatSaverFeeds)99, new FeedInfo("search-by-author", "https://beatsaver.com/api/songs/search/user/" + AUTHORKEY) }

        public override void PrepareReader()
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
        public override string GetFeedName(IFeedSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeatSaverReader.GetFeedName");
            if (!(settings is BeatSaverFeedSettings ssSettings))
                throw new InvalidCastException("Settings is not BeatSaverFeedSettings in BeatSaverReader.GetFeedName");
            return BeatSaverFeed.Feeds[ssSettings.Feed].DisplayName;
        }

        public static List<ScrapedSong> ParseSongsFromPage(string pageText, string sourceUrl, bool storeRawData)
        {
            return ParseSongsFromPage(pageText, Utilities.GetUriFromString(sourceUrl), storeRawData);
        }

        /// <summary>
        /// Parses out a List of ScrapedSongs from the given page text. Also works if the page is for a single song.
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public static List<ScrapedSong> ParseSongsFromPage(string pageText, Uri? sourceUrl, bool storeRawData)
        {
            JObject? result; ;
            try
            {
                result = JObject.Parse(pageText) ?? new JObject();
                return ParseSongsFromJson(result, sourceUrl, storeRawData);
            }
            catch (JsonReaderException ex)
            {
                Logger?.Exception("Unable to parse JSON from text", ex);
                return new List<ScrapedSong>();
            }

        }


        /// <summary>
        /// Parses out a List of ScrapedSongs from the json. Also works if the page is for a single song.
        /// </summary>
        /// <param name="pageText"></param>
        /// <returns></returns>
        public static List<ScrapedSong> ParseSongsFromJson(JToken result, Uri? sourceUrl, bool storeRawData)
        {

            List<ScrapedSong> songs = new List<ScrapedSong>();
            ScrapedSong newSong;

            // Single song in page text.
            if (result["docs"] == null && result.Type != JTokenType.Array)
            {
                if (result["id"] != null)
                {
                    newSong = ParseSongFromJson(result, sourceUrl, storeRawData);
                    if (newSong != null)
                    {
                        songs.Add(newSong);
                        return songs;
                    }
                }
                return songs;
            }

            // Array of songs in page text.
            JToken[]? songJSONAry = result["docs"]?.ToArray();

            if (songJSONAry == null)
            {
                Logger?.Error("Invalid page text: 'docs' field not found.");
                return songs;
            }

            foreach (JObject song in songJSONAry)
            {
                newSong = ParseSongFromJson(song, sourceUrl, storeRawData);
                if (newSong != null)
                    songs.Add(newSong);
            }
            return songs;
        }

        public static ScrapedSong ParseSongFromJson(JToken song, string sourceUrl, bool storeRawData)
        {
            return ParseSongFromJson(song, Utilities.GetUriFromString(sourceUrl), storeRawData);
        }

        /// <summary>
        /// Creates a SongInfo from a JObject.
        /// </summary>
        /// <param name="song"></param>
        /// <exception cref="ArgumentException">Thrown when a hash can't be found for the given song JObject.</exception>
        /// <returns></returns>
        public static ScrapedSong ParseSongFromJson(JToken song, Uri? sourceUri, bool storeRawData)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null for BeatSaverReader.ParseSongFromJson.");
            if (song["versions"] is JArray versions && versions.Count > 0)
            {
                JObject latest = GetLatestSongVersion(song);
                //JSONObject song = (JSONObject) aKeyValue;
                string? songKey = latest["key"]?.Value<string>();
                string? songHash = latest["hash"]?.Value<string>().ToUpper();
                string? songName = song["metadata"]?["songName"]?.Value<string>();
                string? mapperName = song["uploader"]?["name"]?.Value<string>();
                DateTime uploadDate = song["uploaded"]?.Value<DateTime>() ?? DateTime.MinValue;
                if (songHash == null || songHash.Length == 0)
                    throw new ArgumentException("Unable to find hash for the provided song, is this a valid song JObject?");
                Uri downloadUri = WebUtils.GetDownloadUriByHash(songHash);
                ScrapedSong newSong = new ScrapedSong(songHash, songName, mapperName, downloadUri, sourceUri, storeRawData ? song as JObject : null)
                {
                    Key = songKey,
                    UploadDate = uploadDate
                };
                return newSong;
            }
            else
                throw new ArgumentException("Song does not appear to have any versions available.", nameof(song));
        }

        public static JObject GetLatestSongVersion(JToken song)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null for BeatSaverReader.ParseSongFromJson.");
            if (song["versions"] is JArray versions && versions.Count > 0)
            {
                // take latest version
                if (versions.Where(VersionIsPublished).LastOrDefault() is JObject latest)
                    return latest;
                throw new Exception("Song has no published versions.");
            }
            else
                throw new ArgumentException("Song does not appear to have any versions available.", nameof(song));
        }

        private static bool VersionIsPublished(JToken v)
        {
            JToken? state = v["state"];
            return state != null && state.Value<string>().Equals("Published", StringComparison.OrdinalIgnoreCase);
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
        public override async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings _settings, IProgress<ReaderProgress> progress, CancellationToken cancellationToken)
        {
            PrepareReader();
            if (_settings == null)
                throw new ArgumentNullException(nameof(_settings), "Settings cannot be null for BeatSaverReader.GetSongsFromFeedAsync");
            if (!(_settings is BeatSaverFeedSettings settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            int maxPages = settings.MaxPages;
            int maxSongs = settings.MaxSongs;
            int pagesChecked = 0;
            bool useMaxPages = maxPages != 0;
            bool useMaxSongs = maxSongs != 0;

            int estimatedPageResults = Math.Min(useMaxSongs ? maxSongs / SongsPerPage : int.MaxValue, useMaxPages ? maxPages : int.MaxValue);
            if (settings.Feed == BeatSaverFeedName.Author && string.IsNullOrEmpty(settings.AuthorId))
            {
                string? uploaderName = settings.SearchQuery?.Criteria;
                if (uploaderName == null || uploaderName.Length == 0)
                    throw new ArgumentException("SearchQuery.Criteria is null or empty for Author feed.", nameof(_settings));
                settings.AuthorId = await GetAuthorIDAsync(uploaderName, cancellationToken).ConfigureAwait(false);
                if (settings.AuthorId == null || settings.AuthorId.Length == 0)
                {
                    return new FeedResult(null, null, new FeedReaderException($"Unable to get uploader ID for '{uploaderName}' from Beat Saver", null, FeedReaderFailureCode.SourceFailed), FeedResultError.Error);
                }
            }
            BeatSaverFeed feed = new BeatSaverFeed(settings) { StoreRawData = true };
            try
            {
                feed.EnsureValidSettings();
            }
            catch (InvalidFeedSettingsException ex)
            {
                return new FeedResult(null, null, ex, FeedResultError.Error);
            }
            FeedAsyncEnumerator feedEnum = feed.GetEnumerator();
            List<PageReadResult> pageResults = new List<PageReadResult>();
            Dictionary<string, ScrapedSong> newSongs = new Dictionary<string, ScrapedSong>();
            bool continueLooping = true;
            int erroredPages = 0;
            try
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    PageReadResult pageResult = await feedEnum.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                    pagesChecked++;
                    pageResults.Add(pageResult);
                    if (!pageResult.Successful)
                    {
                        erroredPages++;
                        if (erroredPages > 3)
                            return new FeedResult(newSongs, pageResults, pageResult.Exception, FeedResultError.Warning);
                    }
                    if (Utilities.IsPaused)
                        await Utilities.WaitUntil(() => !Utilities.IsPaused, 500, cancellationToken).ConfigureAwait(false);
                    if (pageResult.IsLastPage)
                    {
                        Logger?.Debug("Last page reached.");
                        continueLooping = false;
                        if (pageResult == null)
                            break;
                    }
                    int songsAdded = 0;
                    if (pageResult.Songs != null && pageResult.Count > 0)
                    {
                        foreach (ScrapedSong song in pageResult.Songs)
                        {
                            string? songHash = song.Hash;
                            if (songHash == null || songHash.Length == 0)
                                continue;
                            if (!newSongs.ContainsKey(songHash))
                            {
                                if (newSongs.Count < settings.MaxSongs || settings.MaxSongs == 0)
                                {
                                    newSongs.Add(songHash, song);
                                    songsAdded++;
                                }
                                if ((useMaxSongs && newSongs.Count >= settings.MaxSongs))
                                {
                                    continueLooping = false;
                                    break;
                                }
                            }
                        }
                    }
                    progress?.Report(new ReaderProgress(pagesChecked, songsAdded));
                } while (continueLooping);
            }
            catch (OperationCanceledException ex)
            {
                return FeedResult.GetCancelledResult(newSongs, pageResults, ex);
            }
            return new FeedResult(newSongs, pageResults);
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
            if (_authors.TryGetValue(authorName, out string authorId))
            {
                Logger.Debug($"Author '{authorName}' in cache with ID '{authorId}'");
                return authorId;
            }
            string? mapperId = string.Empty;

            string pageText;
            JObject? result = null;

            Logger?.Debug($"Checking response for the author ID.");
            Uri? sourceUri = new Uri($"https://api.beatsaver.com/users/name/{authorName}");
            IWebResponseMessage ? response = null;
            try
            {
                response = await WebUtils.GetBeatSaverAsync(sourceUri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content == null)
                {
                    Logger?.Error($"WebResponse Content was null getting UploaderID from author name '{authorName}'");
                    return string.Empty;
                }
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
                Logger?.Error($"{errorText} getting UploaderID from author name, '{sourceUri}' responded with {ex.Response?.StatusCode}:{ex.Response?.ReasonPhrase}");
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
                Logger?.Error($"Uncaught error getting UploaderID from author name '{authorName}'");
                Logger?.Debug($"{ex}");
                return string.Empty;
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
            if (result != null && result["id"] is JToken idProp)
            {
                int mapperIdInt = idProp.Value<int>();
                if (mapperIdInt > 0)
                {
                    mapperId = idProp.Value<string>();
                    _authors[authorName] = mapperId;
                    Logger?.Debug($"Matched author '{authorName}' to ID '{mapperId}'");
                }
            }

            return mapperId ?? string.Empty;
        }

        [Obsolete("Not implemented.")]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<List<JToken>> ScrapeBeatSaver(int timeBetweenRequests, CancellationToken cancellationToken, DateTime? stopAtDate = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException("Not finished");
        }

        #endregion
        #endregion

    }
}
