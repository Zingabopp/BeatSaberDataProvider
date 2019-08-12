using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using SongFeedReaders.Logging;
using WebUtilities;
using System.Collections.Concurrent;

namespace SongFeedReaders
{
    public static class WebUtils
    {
        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }
        public static bool IsInitialized { get; private set; }
        private static readonly TimeSpan RateLimitPadding = new TimeSpan(0, 0, 0, 0, 100);

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

        /// <summary>
        /// Maybe have to move to WebUtils and use url.LastIndexOf("/")
        /// </summary>
        private static ConcurrentDictionary<string, DateTime> WaitForRateLimitDict = new ConcurrentDictionary<string, DateTime>();

        public static async Task WaitForRateLimit(string baseUrl)
        {
            TimeSpan delay = WaitForRateLimitDict.GetOrAdd(baseUrl, (f) => DateTime.Now) - DateTime.Now;
            if (delay <= TimeSpan.Zero) // Make sure the delay is > 0
                delay = TimeSpan.Zero;
            else
            {
                Logger.Debug($"Preemptively waiting {delay.Seconds} seconds for rate limit.");
            }
            await Task.Delay(delay).ConfigureAwait(false);
            return;
        }

        /// <summary>
        /// Use to get web responses from Beat Saver. If the rate limit is reached, it waits for the limit to expire before trying again.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        public static async Task<IWebResponseMessage> GetBeatSaverAsync(Uri uri, int retries = 5)
        {
            
            bool rateLimitExceeded = false;
            int tries = 0;
            IWebResponseMessage response;
            string baseUrl = uri?.OriginalString.Substring(0, uri.OriginalString.LastIndexOf("/")) 
                ?? throw new ArgumentNullException(nameof(uri), "uri cannot be null for WebUtils.GetBeatSaverAsync");
            await WaitForRateLimit(baseUrl).ConfigureAwait(false); // Wait for an existing rate limit if it exists
            do
            {

                rateLimitExceeded = false;
                response = await WebUtils.GetWebClientSafe().GetAsync(uri).ConfigureAwait(false);
                if (response.StatusCode == 429 && tries < retries)
                {
                    rateLimitExceeded = true;

                    var rateLimit = ParseBeatSaverRateLimit(response.Headers);
                    WaitForRateLimitDict.AddOrUpdate(baseUrl, rateLimit.TimeToReset, (url, resetTime) =>
                    {
                        resetTime = rateLimit.TimeToReset;
                        return resetTime;
                    });
                    if (rateLimit != null)
                    {
                        TimeSpan delay = new TimeSpan(0);
                        var calcDelay = rateLimit.TimeToReset.Add(RateLimitPadding) - DateTime.Now;
                        if (calcDelay > TimeSpan.Zero) // Make sure the delay is > 0
                            delay = calcDelay;

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

        /// <summary>
        /// Converts a Unix timestamp to a DateTime object.
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Parse the rate limit from Beat Saver's response headers.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static RateLimit ParseBeatSaverRateLimit(IDictionary<string, IEnumerable<string>> headers)
        {
            if (headers == null)
            {
                return null;
                //throw new ArgumentNullException(nameof(headers), "headers cannot be null for WebUtils.ParseRateLimit");
            }
            if (RateLimitKeys.All(k => headers.Keys.Contains(k)))
            {
                try
                {
                    return new RateLimit()
                    {
                        CallsRemaining = int.Parse(headers[RATE_LIMIT_REMAINING_KEY].FirstOrDefault()),
                        TimeToReset = UnixTimeStampToDateTime(double.Parse(headers[RATE_LIMIT_RESET_KEY].FirstOrDefault())),
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

        /// <summary>
        /// Initializes WebUtils, this class cannot be used before calling this.
        /// </summary>
        /// <param name="client"></param>
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
        public DateTime TimeToReset { get; set; }
        public int CallsPerReset { get; set; }
    }
}
