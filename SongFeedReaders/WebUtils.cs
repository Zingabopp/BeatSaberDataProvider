using SongFeedReaders.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using SongFeedReaders.Services;
namespace SongFeedReaders
{
    /// <summary>
    /// Wrapper for the web client used by SongFeedReaders. Initialize(IWebClient) must be called before it can be used.
    /// </summary>
    public static class WebUtils
    {
        private static SongInfoManager? _songInfoManager;
        public static SongInfoManager SongInfoManager
        {
            get
            {
                if(_songInfoManager == null)
                {
                    _songInfoManager = new SongInfoManager();
                    _songInfoManager.AddProvider<BeatSaverSongInfoProvider>("BeatSaverProvider");
                }
                return _songInfoManager;
            }
        }

        private static FeedReaderLoggerBase? _logger;
        public static FeedReaderLoggerBase? Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        private static bool _isInitialized;
        public static bool IsInitialized
        {
            get { return _isInitialized && _webClient != null; }
            private set { _isInitialized = value; }
        }
        private static readonly TimeSpan RateLimitPadding = new TimeSpan(0, 0, 0, 3, 0);
        private static readonly string[] RateLimitedRoutes = new string[] {
            "/vote",
            "/legal",
            "/users/me",
            "/users/find",
            "/stats",
            "/download/key/*",
            "/download/hash/*",
            "/search/text",
            "/search/advanced",
            "/maps/latest",
            "/maps/downloads",
            "/maps/plays",
            "/maps/hot",
            "/maps/rating",
            "/maps/detail/*",
            "/maps/by-hash/*",
            "/maps/uploader/*"
        };

        public static string? GetRateLimitedBase(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            string? baseUrl = null;
            bool wildCard = false;

            string? route = RateLimitedRoutes.Where(r => url.Contains(r.EndsWith("*") ? r.Substring(0, r.Length - 1) : r)).SingleOrDefault();
            if (string.IsNullOrEmpty(route))
                return string.Empty;
            if (wildCard = route.EndsWith("*"))
            {
                route = route.Substring(0, route.Length - 1);
            }
            int routeLength = route.Length;
            baseUrl = url.Substring(0, url.IndexOf(route) + routeLength);

            if (wildCard)
            {
                string addition = string.Empty;
                string afterRoute = url.Substring(url.IndexOf(route) + route.Length);
                int firstSlash = afterRoute.IndexOf("/");
                if (firstSlash > 0)
                    addition = afterRoute.Substring(0, firstSlash);
                else
                    addition = afterRoute;
                baseUrl += addition;
            }
            return baseUrl;
        }

        public static string? GetRateLimitedBase(Uri uri)
        {
            return GetRateLimitedBase(uri.ToString());
        }
        private static IWebClient? _webClient;

        /// <summary>
        /// Returns the WebClient, throws a <see cref="NullReferenceException"/> if WebClient is null.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown if WebClient is null.</exception>
        public static IWebClient WebClient
        {
            get
            {
                if (_webClient == null)
                {
                    throw new NullReferenceException(IsInitialized ?
                        "WebClient is null, even though WebUtils was initialized."
                        : "WebClient is null, WebUtils was never initialized. ");
                }
                return _webClient;
            }
        }

        /// <summary>
        /// Stores known rate limit remaining time for BeatSaver's routes.
        /// Key is the base URL calculated by <see cref="GetRateLimitedBase(string)"/>.
        /// </summary>
        internal static ConcurrentDictionary<string, RateLimitPair> WaitForRateLimitDict = new ConcurrentDictionary<string, RateLimitPair>();
        internal class RateLimitPair
        {
            public RateLimitPair(int callsRemaining, DateTime timeToReset)
            {
                lock (_updateLock)
                {
                    CallsRemaining = callsRemaining;
                    TimeToReset = timeToReset;
                }
            }

            public RateLimitPair(RateLimit? rateLimit)
            {
                DateTime _resetTime = rateLimit?.TimeToReset ?? DateTime.Now;
                int _callsRemaining = rateLimit?.CallsRemaining ?? 0;
                lock (_updateLock)
                {
                    if (_resetTime >= TimeToReset)
                    {
                        CallsRemaining = _callsRemaining;
                        TimeToReset = _resetTime;
                    }
                }
            }

            public void Update(RateLimit? rateLimit)
            {
                if (rateLimit == null)
                    return;
                DateTime _resetTime = rateLimit.TimeToReset;
                int _callsRemaining = rateLimit.CallsRemaining;
                lock (_updateLock)
                {
                    if (_resetTime >= TimeToReset)
                    {
                        CallsRemaining = _callsRemaining;
                        TimeToReset = _resetTime;
                    }
                }
            }
            public int CallsRemaining;
            public DateTime TimeToReset;
            private object _updateLock = new object();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="TaskCanceledException"></exception>
        /// <returns></returns>
        public static async Task WaitForRateLimit(string? baseUrl, RateLimit? rateLimit, CancellationToken cancellationToken)
        {
            RateLimitPair existing = UpdateRateLimit(baseUrl, rateLimit);
            if (existing == null || existing.CallsRemaining > 0)
                return;
            TimeSpan delay = existing.TimeToReset - DateTime.Now;
            if (delay <= TimeSpan.Zero) // Make sure the delay is > 0
                return;
            else
            {
                Logger?.Debug($"Preemptively waiting {delay.Seconds} seconds for rate limit.");
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            return;
        }

        private static RateLimitPair? UpdateRateLimit(string? baseUrl, RateLimit? rateLimit)
        {
            if (string.IsNullOrEmpty(baseUrl) || baseUrl == null)
                return null;
            RateLimitPair existing = WaitForRateLimitDict.GetOrAdd(baseUrl, (f) => new RateLimitPair(rateLimit));
            existing.Update(rateLimit);
            return existing;
        }

        /// <summary>
        /// Use to get web responses from Beat Saver. If the rate limit is reached, it waits for the limit to expire before trying again unless the wait is longer than maxSecondsToWait.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when Uri is null.</exception>
        /// <exception cref="WebClientException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public static async Task<IWebResponseMessage> GetBeatSaverAsync(Uri uri, CancellationToken cancellationToken, int maxSecondsToWait = 0, int retries = 5)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool retry = false;
            int tries = 0;
            IWebResponseMessage? response = null;
            string baseUrl = GetRateLimitedBase(uri)
                ?? throw new ArgumentNullException(nameof(uri), "uri cannot be null for WebUtils.GetBeatSaverAsync");
            await WaitForRateLimit(baseUrl, null, cancellationToken).ConfigureAwait(false); // Wait for an existing rate limit if it exists
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                retry = false;
                try
                {
                    response = await WebClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        RateLimit rateLimit = ParseBeatSaverRateLimit(response.Headers);
                        UpdateRateLimit(baseUrl, rateLimit);
                    }
                    catch (Exception ex)
                    {
                        Logger?.Debug($"Error parsing rate limit for {uri}: {ex.Message}");
                    }
                }
                catch (WebClientException ex)
                {
                    int statusCode = ex.Response?.StatusCode ?? 0;
                    if (tries >= retries && (statusCode != 429 || statusCode != 0))
                        throw;
                    int errorCode = ex.Response?.StatusCode ?? 0;
                    if (errorCode == 429 && tries < retries)
                    {

                        retry = true;
                        RateLimit? rateLimit = ParseBeatSaverRateLimit(ex.Response?.Headers);

                        WaitForRateLimitDict.AddOrUpdate(baseUrl, u => new RateLimitPair(rateLimit), (url, rateLimitPair) =>
                        {
                            rateLimitPair.Update(rateLimit);
                            return rateLimitPair;
                        });
                        if (rateLimit != null)
                        {
                            TimeSpan delay = new TimeSpan(0);
                            TimeSpan calcDelay = rateLimit.TimeToReset.Add(RateLimitPadding) - DateTime.Now;
                            if (calcDelay > TimeSpan.Zero) // Make sure the delay is > 0
                                delay = calcDelay;
                            if (maxSecondsToWait != 0 && delay.TotalSeconds > maxSecondsToWait)
                            {
                                Logger?.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, delay of {(int)delay.TotalSeconds} seconds is too long, cancelling...");
                                throw ex;
                            }
                            Logger?.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, retrying in {(int)delay.TotalSeconds} seconds");
                            await Task.Delay(delay).ConfigureAwait(false);
                            tries++;
                            continue;
                        }
                        else
                        {
                            Logger?.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, could not parse rate limit, not retrying.");
                            throw ex;
                        }
                    }
                    else if (errorCode == 0 && tries < retries)
                    {
                        Logger?.Warning($"Error getting {uri.ToString()}, retrying...");
                        await Task.Delay(500).ConfigureAwait(false);
                        tries++;
                        retry = true;
                    }
                    else
                    {
                        if (!(ex.Response?.IsSuccessStatusCode ?? true))
                            Logger?.Debug($"Error getting {uri.ToString()}, {errorCode} : {ex.Response?.ReasonPhrase}. Skipping...");
                        throw ex;
                    }
                }

            } while (retry);
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
        public static RateLimit? ParseBeatSaverRateLimit(IDictionary<string, IEnumerable<string>>? headers)
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
        /// Initializes WebUtils, this class cannot be used before calling this. Should only be called once.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
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
                _ = SongInfoManager;
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
