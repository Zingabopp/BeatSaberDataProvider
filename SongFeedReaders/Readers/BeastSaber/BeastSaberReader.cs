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
using System.Diagnostics;
using WebUtilities;

namespace SongFeedReaders.Readers.BeastSaber
{
    public class BeastSaberReader : IFeedReader
    {
        #region Constants
        public static readonly string NameKey = "BeastSaberReader";
        public static readonly string SourceKey = "BeastSaber";
        private const string USERNAMEKEY = "{USERNAME}";
        private const string PAGENUMKEY = "{PAGENUM}";
        private static readonly Dictionary<string, ContentType> ContentDictionary =
            new Dictionary<string, ContentType>() { { "text/xml", ContentType.XML }, { "application/json", ContentType.JSON } };
        //private const string DefaultLoginUri = "https://bsaber.com/wp-login.php?jetpack-sso-show-default-form=1";
        private const string BeatSaverDownloadURL_Base = "https://beatsaver.com/api/download/key/";
        public Uri RootUri { get { return new Uri("https://bsaber.com"); } }
        public const int SongsPerXmlPage = 50;
        public const int SongsPerJsonPage = 50;
        private const string XML_TITLE_KEY = "SongTitle";
        private const string XML_DOWNLOADURL_KEY = "DownloadURL";
        private const string XML_HASH_KEY = "Hash";
        private const string XML_AUTHOR_KEY = "LevelAuthorName";
        private const string XML_SONGKEY_KEY = "SongKey";
        private const string INVALIDFEEDSETTINGSMESSAGE = "The IFeedSettings passed is not a BeastSaberFeedSettings.";

        private const string DescriptionFollows = "Retrieves songs by mappers that are marked as Followed by the provided BeastSaber account name.";
        private const string DescriptionBookmarks = "Retrieves songs that are bookmarked by the provided BeastSaber account name.";
        private const string DescriptionCuratorRecommended = "Retrieves songs from BeastSaber's Curator Recommended list.";
        #endregion

        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public string Name { get { return NameKey; } }
        public string Source { get { return SourceKey; } }
        public bool Ready { get; private set; }
        public bool StoreRawData { get; set; }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }
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

        private static Dictionary<BeastSaberFeed, FeedInfo> _feeds;
        public static Dictionary<BeastSaberFeed, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeastSaberFeed, FeedInfo>()
                    {
                        { (BeastSaberFeed)0, new FeedInfo("Follows", "BeastSaber Follows", "https://bsaber.com/members/" + USERNAMEKEY + "/wall/followings/feed/?acpage=" + PAGENUMKEY, DescriptionFollows) },
                        { (BeastSaberFeed)1, new FeedInfo("Bookmarks", "BeastSaber Bookmarks", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=" + USERNAMEKEY + "&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage, DescriptionBookmarks)},
                        { (BeastSaberFeed)2, new FeedInfo("Curator Recommended","BeastSaber CuratorRecommended", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage, DescriptionCuratorRecommended) }
                    };
                }
                return _feeds;
            }
        }

        public void PrepareReader()
        {
            if (!Ready)
            {
                Ready = true;
            }
        }

        public string GetFeedName(IFeedSettings settings)
        {
            if (!(settings is BeastSaberFeedSettings ssSettings))
                throw new ArgumentException("Settings is not BeastSaberFeedSettings", nameof(settings));
            return Feeds[ssSettings.Feed].DisplayName;
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

        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText.</exception>
        /// <exception cref="JsonReaderException">Invalid JSON in page text.</exception>
        /// <returns></returns>
        public List<ScrapedSong> GetSongsFromPageText(string pageText, Uri sourceUri, ContentType contentType)
        {
            List<ScrapedSong> songsOnPage = new List<ScrapedSong>();
            if (string.IsNullOrEmpty(pageText))
            {
                Logger?.Warning($"Null or empty string passed to GetSongsFromPageText. SourceUri: {sourceUri?.ToString()}");
                return songsOnPage;
            }
            //if (pageText.ToLower().StartsWith(@"<?xml"))
            if (contentType == ContentType.XML)
            {
                songsOnPage = ParseXMLPage(pageText, sourceUri);
            }
            else if (contentType == ContentType.JSON) // Page is JSON
            {
                songsOnPage = ParseJsonPage(pageText, sourceUri);
            }
            //Logger?.Debug($"{songsOnPage.Count} songs on page at {sourceUrl}");
            return songsOnPage;
        }

        /// <summary>
        /// Most of this yoinked from Brian's SyncSaber.
        /// https://github.com/brian91292/SyncSaber/blob/master/SyncSaber/SyncSaber.cs#L259
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException"></exception>
        /// <returns></returns>
        public List<ScrapedSong> ParseXMLPage(string pageText, Uri sourceUrl)
        {
            if (string.IsNullOrEmpty(pageText))
                return new List<ScrapedSong>();
            bool retry = false;
            var songsOnPage = new List<ScrapedSong>();
            XmlDocument xmlDocument = null;
            do
            {
                try
                {
                    xmlDocument = new XmlDocument() { XmlResolver = null };
                    var sr = new StringReader(pageText);
                    using (var reader = XmlReader.Create(sr, new XmlReaderSettings() { XmlResolver = null }))
                    {
                        xmlDocument.Load(reader);
                    }
                    retry = false;
                }
                catch (XmlException ex)
                {
                    if (retry == true)
                    {
                        // TODO: Probably don't need logging here.
                        Logger?.Exception("Exception parsing XML.", ex);
                        throw;
                    }
                    else
                    {
                        Logger?.Debug("Invalid XML formatting detected, attempting to fix...");
                        pageText = pageText.Replace(" & ", " &amp; ");
                        retry = true;
                    }
                    //File.WriteAllText("ErrorText.xml", pageText);
                }
            } while (retry == true);
            XmlNodeList xmlNodeList = xmlDocument.DocumentElement.SelectNodes("/rss/channel/item");
            foreach (object obj in xmlNodeList)
            {
                XmlNode node = (XmlNode)obj;
                if (node["DownloadURL"] == null || node["SongTitle"] == null)
                {
                    Logger?.Debug("Not a song! Skipping!");
                }
                else
                {
                    string songName = node[XML_TITLE_KEY].InnerText;
                    string downloadUrl = node[XML_DOWNLOADURL_KEY]?.InnerText;
                    string hash = node[XML_HASH_KEY]?.InnerText?.ToUpper();
                    string authorName = node[XML_AUTHOR_KEY]?.InnerText;
                    string songKey = node[XML_SONGKEY_KEY]?.InnerText;
                    if (downloadUrl.Contains("dl.php"))
                    {
                        Logger?.Warning("Skipping BeastSaber download with old url format!");
                    }
                    else
                    {
                        string songIndex = !string.IsNullOrEmpty(songKey) ? songKey : downloadUrl.Substring(downloadUrl.LastIndexOf('/') + 1);
                        //string mapper = !string.IsNullOrEmpty(authorName) ? authorName : GetMapperFromBsaber(node.InnerText);
                        //string songUrl = !string.IsNullOrEmpty(downloadUrl) ? downloadUrl : BeatSaverDownloadURL_Base + songIndex;
                        if (!string.IsNullOrEmpty(hash))
                        {
                            JObject jObject = null;
                            if (StoreRawData)
                            {
                                jObject = new JObject();
                                jObject.Add(XML_TITLE_KEY, songName);
                                jObject.Add(XML_DOWNLOADURL_KEY, downloadUrl);
                                jObject.Add(XML_HASH_KEY, hash);
                                jObject.Add(XML_AUTHOR_KEY, authorName);
                                jObject.Add(XML_SONGKEY_KEY, songKey);
                            }

                            songsOnPage.Add(new ScrapedSong(hash)
                            {
                                DownloadUri = Utilities.GetUriFromString(downloadUrl),
                                SourceUri = sourceUrl,
                                SongName = songName,
                                SongKey = songIndex,
                                MapperName = authorName,
                                RawData = jObject != null ? jObject.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                            });
                        }
                    }
                }
            }
            return songsOnPage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="sourceUri"></param>
        /// <exception cref="JsonReaderException">Thrown when the page text is unable to parsed.</exception>
        /// <returns></returns>
        public List<ScrapedSong> ParseJsonPage(string pageText, Uri sourceUri)
        {
            JObject result = new JObject();
            var songsOnPage = new List<ScrapedSong>();
            //try
            //{
            result = JObject.Parse(pageText);

            //}
            //catch (JsonReaderException ex)
            //{
            //    throw;
            //}

            var songs = result["songs"];
            foreach (var bSong in songs)
            {
                // Try to get the song hash from BeastSaber
                string songHash = bSong["hash"]?.Value<string>();
                string songKey = bSong["song_key"]?.Value<string>();
                string songName = bSong["title"]?.Value<string>();
                string mapperName = bSong["level_author_name"]?.Value<string>();
                string downloadUrl = "";
                if (!string.IsNullOrEmpty(songKey))
                {
                    downloadUrl = BeatSaverDownloadURL_Base + songKey;
                }
                if (!string.IsNullOrEmpty(songHash))
                {
                    songsOnPage.Add(new ScrapedSong(songHash)
                    {
                        DownloadUri = Utilities.GetUriFromString(downloadUrl),
                        SourceUri = sourceUri,
                        SongName = songName,
                        SongKey = songKey,
                        MapperName = mapperName,
                        RawData = StoreRawData ? bSong.ToString(Newtonsoft.Json.Formatting.None) : string.Empty
                    });
                }
            }
            return songsOnPage;
        }

#pragma warning disable CA1054 // Uri parameters should not be strings
        /// <summary>
        /// Gets the page URI for a given UrlBase and page number.
        /// </summary>
        /// <param name="feedUrlBase"></param>
        /// <param name="page"></param>
        /// <exception cref="ArgumentNullException">Thrown when feedUrlBase is null or empty.</exception>
        /// <returns></returns>
        public Uri GetPageUri(string feedUrlBase, int page)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            if (string.IsNullOrEmpty(feedUrlBase))
                throw new ArgumentNullException(nameof(feedUrlBase), "feedUrlBase cannot be null or empty for GetPageUrl");
            string pageStr = page.ToString();
            string feedUrl = feedUrlBase.Replace(USERNAMEKEY, _username).Replace(PAGENUMKEY, pageStr);
            //if (page == 2)
            //    feedUrl = feedUrl.Replace("followings", "asdfasdf");
            //Logger?.Debug($"Replacing {USERNAMEKEY} with {_username} in base URL:\n   {feedUrlBase}");
            return Utilities.GetUriFromString(feedUrl);
        }

        #region Web Requests
        #region Async
        // TODO: Abort early when bsaber.com is down (check if all items in block failed?)
        // TODO: Make cancellationToken actually do something.
        /// <summary>
        /// Gets all songs from the feed defined by the provided settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidCastException">Thrown when the passed IFeedSettings isn't a BeastSaberFeedSettings.</exception>
        /// <exception cref="ArgumentException">Thrown when trying to access a feed that requires a username and the username wasn't provided.</exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return FeedResult.CancelledResult;
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "settings cannot be null for BeastSaberReader.GetSongsFromFeedAsync.");
            Dictionary<string, ScrapedSong> retDict = new Dictionary<string, ScrapedSong>();
            if (!(settings is BeastSaberFeedSettings _settings))
                throw new InvalidCastException(INVALIDFEEDSETTINGSMESSAGE);
            if (_settings.FeedIndex != 2 && string.IsNullOrEmpty(_username?.Trim()))
            {
                //Logger?.Error($"Can't access feed without a valid username in the config file");
                throw new ArgumentException("Cannot access this feed without a valid username.");
            }
            int pageIndex = settings.StartingPage;
            int maxPages = _settings.MaxPages;
            bool useMaxSongs = _settings.MaxSongs != 0;
            bool useMaxPages = maxPages != 0;
            if (useMaxPages && pageIndex > 1)
                maxPages = maxPages + pageIndex - 1;
            var ProcessPageBlock = new TransformBlock<Uri, PageReadResult>(async feedUri =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Logger?.Debug($"Checking URL: {feedUrl}");
                string pageText = "";

                ContentType contentType = ContentType.Unknown;
                string contentTypeStr = string.Empty;
                IWebResponseMessage response = null;
                try
                {
                    response = await WebUtils.WebClient.GetAsync(feedUri, cancellationToken).ConfigureAwait(false);
                    if ((response?.StatusCode ?? 500) == 500)
                    {
                        response?.Dispose();
                        Logger?.Warning($"Internal server error on {feedUri}, retrying in 20 seconds");
                        await Task.Delay(20000);
                        response = await WebUtils.WebClient.GetAsync(feedUri, cancellationToken).ConfigureAwait(false);
                    }
                    response.EnsureSuccessStatusCode();
                    contentTypeStr = response.Content.ContentType.ToLower();
                    if (ContentDictionary.ContainsKey(contentTypeStr))
                        contentType = ContentDictionary[contentTypeStr];
                    else
                        contentType = ContentType.Unknown;
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                }
                catch (WebClientException ex)
                {
                    return PageReadResult.FromWebClientException(ex, feedUri);
                }
                catch (OperationCanceledException)
                {
                    return new PageReadResult(feedUri, null, new FeedReaderException("Page read was cancelled.", new OperationCanceledException(), FeedReaderFailureCode.Cancelled), PageErrorType.Cancelled);
                }
                catch (Exception ex)
                {
                    string message = $"Error downloading {feedUri} in TransformBlock.";
                    Logger?.Debug(message);
                    Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                    return new PageReadResult(feedUri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.Unknown);
                }
                finally
                {
                    response?.Dispose();
                }
                List<ScrapedSong> newSongs = null;
                try
                {
                    newSongs = GetSongsFromPageText(pageText, feedUri, contentType);
                }
                catch (JsonReaderException ex)
                {
                    // TODO: Probably don't need a logger message here, caller can deal with it.
                    string message = $"Error parsing page text for {feedUri} in TransformBlock.";
                    Logger?.Debug(message);
                    Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                    return new PageReadResult(feedUri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
                }
                catch (XmlException ex)
                {
                    // TODO: Probably don't need a logger message here, caller can deal with it.
                    string message = $"Error parsing page text for {feedUri} in TransformBlock.";
                    Logger?.Debug(message);
                    Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                    return new PageReadResult(feedUri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
                }
                catch (Exception ex)
                {
                    // TODO: Probably don't need a logger message here, caller can deal with it.
                    string message = $"Uncaught error parsing page text for {feedUri} in TransformBlock.";
                    Logger?.Debug(message);
                    Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                    return new PageReadResult(feedUri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.Unknown);
                }
                sw.Stop();
                //Logger?.Debug($"Task for {feedUrl} completed in {sw.ElapsedMilliseconds}ms");
                return new PageReadResult(feedUri, newSongs);
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
                    var feedUrl = GetPageUri(Feeds[_settings.Feed].BaseUrl, pageIndex);
                    await ProcessPageBlock.SendAsync(feedUrl, cancellationToken).ConfigureAwait(false); // TODO: Need check with SongsPerPage
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
                            if (pageResult != null)
                                pageResults.Add(pageResult);
                            if (Utilities.IsPaused)
                                await Utilities.WaitUntil(() => !Utilities.IsPaused, 500, cancellationToken).ConfigureAwait(false);
                            itemsInBlock--;
                            if (pageResult == null || pageResult.Count == 0) // TODO: This will trigger if a single page has an error.
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
                                if (!retDict.ContainsKey(song.Hash))
                                {
                                    if (retDict.Count < settings.MaxSongs || settings.MaxSongs == 0)
                                        retDict.Add(song.Hash, song);
                                    if (retDict.Count >= settings.MaxSongs && useMaxSongs)
                                        continueLooping = false;
                                }
                            }
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

        public Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings)
        {
            return GetSongsFromFeedAsync(settings, CancellationToken.None);
        }

        #endregion

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
                if(flattened.InnerExceptions.Count == 1)
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
        #endregion
        #endregion

        #region Overloads
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="sourceUrl"></param>
        /// <exception cref="JsonReaderException"></exception>
        /// <returns></returns>
        public List<ScrapedSong> ParseJsonPage(string pageText, string sourceUrl)
        {
            return ParseJsonPage(pageText, Utilities.GetUriFromString(sourceUrl));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="sourceUrl"></param>
        /// <exception cref="XmlException"></exception>
        /// <returns></returns>
        public List<ScrapedSong> ParseXMLPage(string pageText, string sourceUrl)
        {
            return ParseXMLPage(pageText, Utilities.GetUriFromString(sourceUrl));
        }
        public Uri GetPageUrl(int feedIndex, int page)
        {
            return GetPageUri(Feeds[(BeastSaberFeed)feedIndex].BaseUrl, page);
        }
        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <exception cref="JsonReaderException">Invalid JSON in pageText</exception>
        /// <returns></returns>
        public List<ScrapedSong> GetSongsFromPageText(string pageText, string sourceUrl, ContentType contentType)
        {
            return GetSongsFromPageText(pageText, Utilities.GetUriFromString(sourceUrl), contentType);
        }


        #endregion


        public enum ContentType
        {
            Unknown = 0,
            XML = 1,
            JSON = 2
        }
    }

    public class BeastSaberFeedSettings : IFeedSettings
    {
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return BeastSaberReader.Feeds[Feed].Name; } }
        private int _feedIndex;
        private int _startingPage;
        private int _maxSongs;
        private int _maxPages;

        /// <summary>
        /// Index of the feed defined by <see cref="BeastSaberFeed"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a value that is not a valid <see cref="BeastSaberFeed"/></exception>
        public int FeedIndex { 
            get { return _feedIndex; }
            set
            {
                if (!Enum.IsDefined(typeof(BeastSaberFeed), value))
                    throw new ArgumentOutOfRangeException($"Failed to set FeedIndex: No BeastSaberFeed defined for an index of {value}.");
                _feedIndex = value;
            }
        }
        
        public BeastSaberFeed Feed 
        { 
            get { return (BeastSaberFeed)FeedIndex; } 
            set 
            { 
                FeedIndex = (int)value; 
            } 
        }

        public int SongsPerPage { get { return FeedIndex == 0 ? BeastSaberReader.SongsPerXmlPage : BeastSaberReader.SongsPerJsonPage; } }

        /// <summary>
        /// Maximum songs to retrieve, will stop the reader before MaxPages is met. Use 0 for unlimited.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 0.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 0.</exception>
        public int MaxSongs
        {
            get { return _maxSongs; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxSongs), "MaxSongs cannot be less than 0.");
                _maxSongs = value;
            }
        }

        /// <summary>
        /// Maximum pages to check, will stop the reader before MaxSongs is met. Use 0 for unlimited.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 0.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 0.</exception>
        public int MaxPages
        {
            get { return _maxPages; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxPages), "MaxPages cannot be less than 0.");
                _maxPages = value;
            }
        }

        /// <summary>
        /// Page of the feed to start on, default is 1. Setting '1' here is the same as starting on the first page.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 1.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 1.</exception>
        public int StartingPage
        {
            get { return _startingPage; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(StartingPage), "StartingPage cannot be less than 1.");
                _startingPage = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="feedIndex"></param>
        /// <param name="maxPages"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="feedIndex"/> is not a valid <see cref="BeastSaberFeed"/></exception>
        public BeastSaberFeedSettings(int feedIndex)
        {
            if (!Enum.IsDefined(typeof(BeastSaberFeed), feedIndex))
                throw new ArgumentOutOfRangeException(nameof(feedIndex), $"No BeastSaberFeed defined for an index of {feedIndex}.");
            FeedIndex = feedIndex;
            MaxPages = 0;
            StartingPage = 1;
        }

        public BeastSaberFeedSettings(BeastSaberFeed feed)
        {
            Feed = feed;
            MaxPages = 0;
            StartingPage = 1;
        }
    }

    public enum BeastSaberFeed
    {
        Following = 0,
        Bookmarks = 1,
        CuratorRecommended = 2
    }
}
