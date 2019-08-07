using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using SongFeedReaders.Logging;
using WebUtilities;

namespace SongFeedReaders
{
    public static class WebUtils
    {
        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }
        public static bool IsInitialized { get; private set; }
        private static readonly TimeSpan RateLimitPadding = new TimeSpan(0, 0, 0, 0, 100);
        //private static readonly object lockObject = new object();
        //private static HttpClientHandler _httpClientHandler;
        //public static HttpClientHandler HttpClientHandler
        //{
        //    get
        //    {
        //        if (_httpClientHandler == null)
        //        {
        //            _httpClientHandler = new HttpClientHandler();
        //            HttpClientHandler.MaxConnectionsPerServer = 10;
        //            HttpClientHandler.UseCookies = true;
        //            HttpClientHandler.AllowAutoRedirect = true; // Needs to be false to detect Beat Saver song download rate limit
        //        }
        //        return _httpClientHandler;
        //    }
        //}

        private static IWebClient _webClient;
        public static IWebClient WebClient
        {
            get
            {
                return _webClient;
            }

        }

        /// <summary>
        /// Returns the WebClient, throws an exception if WebClient is null (makes debugging easier).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">Thrown if WebClient is null.</exception>
        public static IWebClient GetWebClientSafe()
        {
            return WebClient ?? throw new NullReferenceException(IsInitialized ?
                "WebClient is null, even though WebUtils was initialized."
                : "WebClient is null, WebUtils was never initialized.");
        }

        public static async Task<IWebResponseMessage> GetBeatSaverAsync(Uri uri, int retries = 5)
        {
            bool rateLimitExceeded = false;
            int tries = 0;
            IWebResponseMessage response;
            do
            {
                
                rateLimitExceeded = false;
                response = await WebUtils.GetWebClientSafe().GetAsync(uri).ConfigureAwait(false);
                if(!response.IsSuccessStatusCode)
                {
                    Logger.Warning("Not successful");
                }
                if (response.StatusCode == 429 &&  tries < retries)
                {
                    rateLimitExceeded = true;
                    
                    var rateLimit = ParseRateLimit(response.Headers);
                    if (rateLimit != null)
                    {
                        var delay = rateLimit.TimeToReset.Add(RateLimitPadding);
                        Logger.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, retrying in {delay.TotalSeconds} seconds");
                        await Task.Delay(delay).ConfigureAwait(false);
                        response.Dispose();
                        tries++;
                        continue;
                    }
                    else
                    {
                        Logger.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, could not parse rate limit, not retrying.");
                        return response;
                    }
                }
                else
                    return response;
            } while (rateLimitExceeded);
            return response;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        private const string RATE_LIMIT_REMAINING_KEY = "Rate-Limit-Remaining";
        private const string RATE_LIMIT_RESET_KEY = "Rate-Limit-Reset";
        private const string RATE_LIMIT_TOTAL_KEY = "Rate-Limit-Total";
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Remove unused private members
        private const string RATE_LIMIT_PREFIX = "Rate-Limit";
#pragma warning restore CA1823 // Remove unused private members
#pragma warning restore IDE0051 // Remove unused private members
        private static readonly string[] RateLimitKeys = new string[] { RATE_LIMIT_REMAINING_KEY, RATE_LIMIT_RESET_KEY, RATE_LIMIT_TOTAL_KEY };
        public static RateLimit ParseRateLimit(IDictionary<string, IEnumerable<string>> headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers), "headers cannot be null for WebUtils.ParseRateLimit");
            if (RateLimitKeys.All(k => headers.Keys.Contains(k)))
            {
                try
                {
                    return new RateLimit()
                    {
                        CallsRemaining = int.Parse(headers[RATE_LIMIT_REMAINING_KEY].FirstOrDefault() ?? "-1"),
                        TimeToReset = UnixTimeStampToDateTime(double.Parse(headers[RATE_LIMIT_RESET_KEY].FirstOrDefault())) - DateTime.Now,
                        CallsPerReset = int.Parse(headers[RATE_LIMIT_TOTAL_KEY].FirstOrDefault())
                    };
                }
                catch (Exception ex)
                {
                    Logger?.Exception("Unable to parse RateLimit from header.", ex);
                    return null;
                }

            }
            else
                return null;
        }

        public static void Initialize(IWebClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client), "client cannot be null for WebUtils.Initialize");
                //_webClient = new HttpClientWrapper();
            }
            if (!IsInitialized || _webClient == null)
            {
                _webClient = client;
                IsInitialized = true;
            }
        }
    }

    public class RateLimit
    {
        public int CallsRemaining { get; set; }
        public TimeSpan TimeToReset { get; set; }
        public int CallsPerReset { get; set; }
    }

    //public class HttpGetException : Exception
    //{
    //    public HttpStatusCode HttpStatusCode { get; private set; }
    //    public string Url { get; private set; }

    //    public HttpGetException()
    //        : base()
    //    {
    //        base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
    //        base.Data.Add("Url", string.Empty);
    //    }

    //    public HttpGetException(string message)
    //        : base(message)
    //    {

    //        base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
    //        base.Data.Add("Url", string.Empty);
    //    }

    //    public HttpGetException(string message, Exception inner)
    //        : base(message, inner)
    //    {
    //        base.Data.Add("StatusCode", HttpStatusCode.BadRequest);
    //        base.Data.Add("Url", string.Empty);
    //    }

    //    public HttpGetException(HttpStatusCode code, string url)
    //        : base()
    //    {
    //        base.Data.Add("StatusCode", code);
    //        base.Data.Add("Url", url);
    //        HttpStatusCode = code;
    //        Url = url;
    //    }

    //    public HttpGetException(HttpStatusCode code, string url, string message)
    //    : base(message)
    //    {
    //        base.Data.Add("StatusCode", code);
    //        base.Data.Add("Url", url);
    //        HttpStatusCode = code;
    //        Url = url;
    //    }
    //}

    // From https://stackoverflow.com/questions/45711428/download-file-with-webclient-or-httpclient
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Downloads the provided HttpContent to the specified file.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="filename"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">Thrown when content or the filename are null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and a file at the provided path already exists.</exception>
        /// <returns></returns>
        public static async Task ReadAsFileAsync(this IWebResponseContent content, string filename, bool overwrite)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content), "content cannot be null for HttpContent.ReadAsFileAsync");
            if (string.IsNullOrEmpty(filename?.Trim()))
                throw new ArgumentNullException(nameof(filename), "filename cannot be null or empty for HttpContent.ReadAsFileAsync");
            string pathname = Path.GetFullPath(filename);
            if (!overwrite && File.Exists(filename))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            using (Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                using (Stream writeStream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await contentStream.CopyToAsync(writeStream).ConfigureAwait(false);
                }
            }
        }
    }
}
