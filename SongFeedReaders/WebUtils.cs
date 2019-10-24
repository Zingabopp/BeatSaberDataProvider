﻿using System;
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
    /// <summary>
    /// Wrapper for the web client used by SongFeedReaders. Initialize(IWebClient) must be called before it can be used.
    /// </summary>
    public static class WebUtils
    {
        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public static bool IsInitialized { get; private set; }
        private static readonly TimeSpan RateLimitPadding = new TimeSpan(0, 0, 0, 0, 100);

        private static IWebClient _webClient;

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
                Logger?.Debug($"Preemptively waiting {delay.Seconds} seconds for rate limit.");
            }
            await Task.Delay(delay).ConfigureAwait(false);
            return;
        }

        /// <summary>
        /// Use to get web responses from Beat Saver. If the rate limit is reached, it waits for the limit to expire before trying again unless the wait is longer than maxSecondsToWait.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when Uri is null.</exception>
        /// <exception cref="WebClientException"></exception>
        public static async Task<IWebResponseMessage> GetBeatSaverAsync(Uri uri, int maxSecondsToWait = 0, int retries = 5)
        {

            bool retry = false;
            int tries = 0;
            IWebResponseMessage response = null;
            string baseUrl = uri?.OriginalString.Substring(0, uri.OriginalString.LastIndexOf("/"))
                ?? throw new ArgumentNullException(nameof(uri), "uri cannot be null for WebUtils.GetBeatSaverAsync");
            await WaitForRateLimit(baseUrl).ConfigureAwait(false); // Wait for an existing rate limit if it exists
            do
            {

                retry = false;
                try
                {
                    response = await WebClient.GetAsync(uri).ConfigureAwait(false);
                }
                catch (WebClientException ex)
                {
                    var statusCode = response?.StatusCode ?? 0;
                    if (tries >= retries && (statusCode != 429 || statusCode != 0))
                        throw;
                    response = ex.Response;
                }
                var errorCode = response?.StatusCode ?? 0;
                if (errorCode == 429 && tries < retries)
                {

                    retry = true;
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
                        if (maxSecondsToWait != 0 && delay.TotalSeconds > maxSecondsToWait)
                        {
                            Logger?.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, delay of {(int)delay.TotalSeconds} seconds is too long, cancelling...");
                            return response;
                        }
                        Logger?.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, retrying in {(int)delay.TotalSeconds} seconds");
                        await Task.Delay(delay).ConfigureAwait(false);
                        response.Dispose();
                        tries++;
                        continue;
                    }
                    else
                    {
                        Logger?.Warning($"Try {tries}: Rate limit exceeded on url, {uri.ToString()}, could not parse rate limit, not retrying.");
                        return response;
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
                    if (!(response?.IsSuccessStatusCode ?? true))
                        Logger?.Debug($"Error getting {uri.ToString()}, {errorCode} : {response?.ReasonPhrase}. Skipping...");
                    return response;
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
        /// Initializes WebUtils, this class cannot be used before calling this. Should only be called once.
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
