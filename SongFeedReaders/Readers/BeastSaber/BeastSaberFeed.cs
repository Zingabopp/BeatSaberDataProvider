using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongFeedReaders.Logging;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WebUtilities;

namespace SongFeedReaders.Readers.BeastSaber
{
    public class BeastSaberFeed : IPagedFeed
    {
        private static FeedReaderLoggerBase? _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        private const string USERNAMEKEY = "{USERNAME}";
        private const string PAGENUMKEY = "{PAGENUM}";
        private const string XML_TITLE_KEY = "SongTitle";
        private const string XML_DOWNLOADURL_KEY = "DownloadURL";
        private const string XML_HASH_KEY = "Hash";
        private const string XML_AUTHOR_KEY = "LevelAuthorName";
        private const string XML_SONGKEY_KEY = "SongKey";

        public static readonly int SongsPerXmlPage = 50;
        public static readonly int SongsPerJsonPage = 50;

        private const string DescriptionFollows = "Retrieves songs by mappers that are marked as Followed by the provided BeastSaber account name.";
        private const string DescriptionBookmarks = "Retrieves songs that are bookmarked by the provided BeastSaber account name.";
        private const string DescriptionCuratorRecommended = "Retrieves songs from BeastSaber's Curator Recommended list.";

        private static readonly Dictionary<string, ContentType> ContentDictionary =
            new Dictionary<string, ContentType>() { { "text/xml", ContentType.XML }, { "application/json", ContentType.JSON } };


        private static Dictionary<BeastSaberFeedName, FeedInfo> _feeds;
        public static Dictionary<BeastSaberFeedName, FeedInfo> Feeds
        {
            get
            {
                if (_feeds == null)
                {
                    _feeds = new Dictionary<BeastSaberFeedName, FeedInfo>()
                    {
                        { (BeastSaberFeedName)0, new FeedInfo("Follows", "BeastSaber Follows", "https://bsaber.com/wp-json/bsaber-api/songs/?followed_by=" + USERNAMEKEY + "&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage, DescriptionFollows) },
                        { (BeastSaberFeedName)1, new FeedInfo("Bookmarks", "BeastSaber Bookmarks", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=" + USERNAMEKEY + "&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage, DescriptionBookmarks)},
                        { (BeastSaberFeedName)2, new FeedInfo("Curator Recommended","BeastSaber CuratorRecommended", "https://bsaber.com/wp-json/bsaber-api/songs/?bookmarked_by=curatorrecommended&page=" + PAGENUMKEY + "&count=" + SongsPerJsonPage, DescriptionCuratorRecommended) }
                    };
                }
                return _feeds;
            }
        }

        public BeastSaberFeedName Feed { get; }
        public FeedInfo FeedInfo { get; }
        public string Name { get { return FeedInfo.Name; } }

        public string DisplayName { get { return FeedInfo.DisplayName; } }

        public string Description { get { return FeedInfo.Description; } }

        public Uri RootUri => BeastSaberReader.ReaderRootUri;

        public string BaseUrl { get { return FeedInfo.BaseUrl; } }

        public string Username { get; }

        public int SongsPerPage { get; set; }

        public bool StoreRawData { get; set; }

        public IFeedSettings Settings => BeastSaberFeedSettings;

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
            if (Feed != BeastSaberFeedName.CuratorRecommended)
            {
                if (string.IsNullOrEmpty(BeastSaberFeedSettings.Username))
                {
                    message = $"{nameof(BeastSaberFeedSettings)}.{nameof(BeastSaberFeedSettings.Username)} cannot be null or empty for the {Feed.ToString()} feed.";
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

        public BeastSaberFeedSettings BeastSaberFeedSettings { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null./></exception>
        public BeastSaberFeed(BeastSaberFeedSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings), "settings cannot be null when creating a new ScoreSaberFeed.");
            BeastSaberFeedSettings = (BeastSaberFeedSettings)settings.Clone();
            Feed = BeastSaberFeedSettings.Feed;
            FeedInfo = Feeds[BeastSaberFeedSettings.Feed];
            if (Feed != BeastSaberFeedName.Following)
            {
                SongsPerPage = BeastSaberFeedSettings.SongsPerPage;
                if (SongsPerPage < 1)
                    SongsPerPage = SongsPerJsonPage;
            }
            else
                SongsPerPage = SongsPerXmlPage;
            Username = BeastSaberFeedSettings.Username;
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
            return new Uri(FeedInfo.BaseUrl.Replace(PAGENUMKEY, page.ToString()).Replace(USERNAMEKEY, Username));
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

            try
            {
                Uri pageUri = GetUriForPage(page);
                return await GetSongsAsync(pageUri, cancellationToken);
            }
            catch (InvalidFeedSettingsException)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        /// <returns></returns>
        public async Task<PageReadResult> GetSongsAsync(Uri pageUri, CancellationToken cancellationToken)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string pageText = "";
            bool isLastPage = false;

            Logger.Debug($"Getting songs from '{pageUri}'");
            ContentType contentType = ContentType.Unknown;
            string? contentTypeStr;
            IWebResponseMessage? response = null;
            //PageReadResult result = null;
            try
            {
                response = await WebUtils.WebClient.GetAsync(pageUri, cancellationToken).ConfigureAwait(false);
                if ((response?.StatusCode ?? 500) == 500)
                {
                    response?.Dispose();
                    response = null;
                    Logger?.Warning($"Internal server error on {pageUri}, retrying in 20 seconds");
                    await Task.Delay(20000).ConfigureAwait(false);
                    response = await WebUtils.WebClient.GetAsync(pageUri, cancellationToken).ConfigureAwait(false);
                }
                if (response == null) 
                    throw new WebClientException($"Response was null for '{pageUri}'.");
                response.EnsureSuccessStatusCode();
                contentTypeStr = response.Content?.ContentType?.ToLower();
                if (contentTypeStr != null && ContentDictionary.ContainsKey(contentTypeStr))
                    contentType = ContentDictionary[contentTypeStr];
                else
                    contentType = ContentType.Unknown;

                if (response.Content == null) throw new WebClientException($"Response content was null for '{pageUri}'.");
                pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            }
            catch (WebClientException ex)
            {
                return PageReadResult.FromWebClientException(ex, pageUri);
            }
            catch (OperationCanceledException)
            {
                return new PageReadResult(pageUri, null, null, null, 0, new FeedReaderException("Page read was cancelled.", new OperationCanceledException(), FeedReaderFailureCode.Cancelled), PageErrorType.Cancelled);
            }
            catch (Exception ex)
            {
                string message = $"Error downloading {pageUri} in TransformBlock.";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(pageUri, null, null, null, 0, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.Unknown);
            }
            finally
            {
                response?.Dispose();
                response = null;
            }
            List<ScrapedSong> newSongs;

            ScrapedSong? firstSong = null;
            ScrapedSong? lastSong = null;
            int songsOnPage = 0;

            try
            {
                var scrapedSongs = GetSongsFromPageText(pageText, pageUri, contentType, Settings.StoreRawData || StoreRawData);
                isLastPage = scrapedSongs.Count == 0; 
                firstSong = scrapedSongs.FirstOrDefault();
                lastSong = scrapedSongs.LastOrDefault();
                songsOnPage = scrapedSongs.Count;
                newSongs = new List<ScrapedSong>();
                foreach (var song in scrapedSongs)
                {
                    if (Settings.Filter == null || Settings.Filter(song))
                        newSongs.Add(song);
                    if (Settings.StopWhenAny != null && Settings.StopWhenAny(song))
                        isLastPage = true;
                }
            }
            catch (JsonReaderException ex)
            {
                // TODO: Probably don't need a logger message here, caller can deal with it.
                string message = $"Error parsing page text for {pageUri} in TransformBlock.";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(pageUri, null, firstSong, lastSong, songsOnPage, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
            }
            catch (XmlException ex)
            {
                // TODO: Probably don't need a logger message here, caller can deal with it.
                string message = $"Error parsing page text for {pageUri} in TransformBlock.";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(pageUri, null, firstSong, lastSong, songsOnPage, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.ParsingError);
            }
            catch (Exception ex)
            {
                // TODO: Probably don't need a logger message here, caller can deal with it.
                string message = $"Uncaught error parsing page text for {pageUri} in TransformBlock.";
                Logger?.Debug(message);
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
                return new PageReadResult(pageUri, null, firstSong, lastSong, songsOnPage, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), PageErrorType.Unknown);
            }
            sw.Stop();
            //Logger?.Debug($"Task for {feedUrl} completed in {sw.ElapsedMilliseconds}ms");

            return new PageReadResult(pageUri, newSongs, firstSong, lastSong, songsOnPage, isLastPage);
        }

        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText.</exception>
        /// <exception cref="JsonReaderException">Invalid JSON in page text.</exception>
        /// <returns></returns>
        public static List<ScrapedSong> GetSongsFromPageText(string pageText, Uri sourceUri, ContentType contentType, bool storeRawData)
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
                songsOnPage = ParseXMLPage(pageText, sourceUri, storeRawData);
            }
            else if (contentType == ContentType.JSON) // Page is JSON
            {
                songsOnPage = ParseJsonPage(pageText, sourceUri, storeRawData);
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
        public static List<ScrapedSong> ParseXMLPage(string pageText, Uri sourceUri, bool storeRawData)
        {
            if (string.IsNullOrEmpty(pageText))
                return new List<ScrapedSong>();
            bool retry = false;
            var songsOnPage = new List<ScrapedSong>();
            XmlDocument? xmlDocument = null;
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
            if (xmlDocument == null) throw new XmlException($"xmlDocument was null for '{sourceUri}'.");
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
                    string? songName = node[XML_TITLE_KEY].InnerText;
                    string? downloadUrl = node[XML_DOWNLOADURL_KEY]?.InnerText;
                    string? hash = node[XML_HASH_KEY]?.InnerText?.ToUpper();
                    string? mapperName = node[XML_AUTHOR_KEY]?.InnerText;
                    string? songKey = node[XML_SONGKEY_KEY]?.InnerText;
                    if(hash == null || hash.Length == 0) // TODO: Could use Key if Hash was null.
                    {
                        Logger?.Warning($"Skipping BeastSaber song with null hash.");
                        continue;
                    }
                    if (downloadUrl?.Contains("dl.php") ?? true)
                    {
                        Logger?.Warning("Skipping BeastSaber download with old url format!");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(hash))
                        {
                            JObject? jObject = null;
                            if (storeRawData)
                            {
                                jObject = new JObject();
                                jObject.Add(XML_TITLE_KEY, songName);
                                jObject.Add(XML_DOWNLOADURL_KEY, downloadUrl);
                                jObject.Add(XML_HASH_KEY, hash);
                                jObject.Add(XML_AUTHOR_KEY, mapperName);
                                jObject.Add(XML_SONGKEY_KEY, songKey);
                            }

                            songsOnPage.Add(new ScrapedSong(hash, songName, mapperName, Utilities.GetUriFromString(downloadUrl), sourceUri, jObject) { Key = songKey });
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
        public static List<ScrapedSong> ParseJsonPage(string pageText, Uri sourceUri, bool storeRawData)
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
            foreach (JObject bSong in songs)
            {
                // Try to get the song hash from BeastSaber
                string? songHash = bSong["hash"]?.Value<string>();
                string? songKey = bSong["song_key"]?.Value<string>();
                string? songName = bSong["title"]?.Value<string>();
                string? mapperName = bSong["level_author_name"]?.Value<string>();
                Uri? downloadUri = null;
                if (songHash != null && songHash.Length > 0)
                {
                    downloadUri = WebUtils.GetDownloadUriByHash(songHash);
                    songsOnPage.Add(new ScrapedSong(songHash, songName, mapperName, downloadUri, sourceUri, storeRawData ? bSong : null) { Key = songKey });
                }
                // TODO: This will break if songHash is null
                //else if (songKey != null && songKey.Length > 0)
                //{
                //    downloadUri = Utilities.GetDownloadUriByKey(songKey);
                //    songsOnPage.Add(new ScrapedSong(songHash, songName, mapperName, downloadUri, sourceUri, storeRawData ? bSong : null) { Key = songKey });
                //}
            }
            return songsOnPage;
        }

        public FeedAsyncEnumerator GetEnumerator(bool cachePages)
        {
            return new PagedFeedAsyncEnumerator(this, Settings.StartingPage, cachePages);
        }
        public FeedAsyncEnumerator GetEnumerator()
        {
            return GetEnumerator(false);
        }

        #region Overloads
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="sourceUrl"></param>
        /// <exception cref="JsonReaderException"></exception>
        /// <returns></returns>
        public static List<ScrapedSong> ParseJsonPage(string pageText, string sourceUrl, bool storeRawData)
        {
            return ParseJsonPage(pageText, Utilities.GetUriFromString(sourceUrl), storeRawData);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageText"></param>
        /// <param name="sourceUrl"></param>
        /// <exception cref="XmlException"></exception>
        /// <returns></returns>
        public static List<ScrapedSong> ParseXMLPage(string pageText, string sourceUrl, bool storeRawData)
        {
            return ParseXMLPage(pageText, Utilities.GetUriFromString(sourceUrl), storeRawData);
        }
        /// <summary>
        /// Parses the page text and returns all the songs it can find.
        /// </summary>
        /// <param name="pageText"></param>
        /// <exception cref="XmlException">Invalid XML in pageText</exception>
        /// <exception cref="JsonReaderException">Invalid JSON in pageText</exception>
        /// <returns></returns>
        public static List<ScrapedSong> GetSongsFromPageText(string pageText, string sourceUrl, ContentType contentType, bool storeRawData)
        {
            return GetSongsFromPageText(pageText, Utilities.GetUriFromString(sourceUrl), contentType, storeRawData);
        }
        #endregion

    }

    public enum ContentType
    {
        Unknown = 0,
        XML = 1,
        JSON = 2
    }
}
