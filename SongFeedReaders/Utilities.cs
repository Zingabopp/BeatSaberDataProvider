﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Logging;

namespace SongFeedReaders
{
    public static class Utilities
    {
        public static string BeatSaverDownloadUrlKeyBase => "https://beatsaver.com/api/download/key/";
        public static string BeatSaverDetailsFromKeyBaseUrl => "https://beatsaver.com/api/maps/detail/";
        public static string BeatSaverDetailsFromHashBaseUrl => "https://beatsaver.com/api/maps/by-hash/";
        public static string BeatSaverDownloadUrlHashBase => "http://beatsaver.com/api/download/hash/";
        static Utilities()
        {
            MaxAggregateExceptionDepth = 10;
        }

        public static bool IsPaused { get; internal set; }

        public static void Pause()
        {
            IsPaused = true;
        }

        public static void UnPause()
        {
            IsPaused = false;
        }

        private static FeedReaderLoggerBase _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public static int MaxAggregateExceptionDepth { get; set; }

        public static void WriteExceptions(this AggregateException ae, string message)
        {
            Logger?.Exception(message, ae);
            for (int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger?.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
                if (ae.InnerExceptions[i] is AggregateException ex)
                    WriteExceptions(ex, 0); // TODO: This could get very long
            }
        }
        public static void WriteExceptions(this AggregateException ae, int depth = 0)
        {
            for (int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger?.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
                if (ae.InnerExceptions[i] is AggregateException ex)
                {
                    if (depth < MaxAggregateExceptionDepth)
                    {
                        WriteExceptions(ex, depth + 1);
                    }
                }
            }
        }

        public static Uri GetUriFromString(string uriString)
        {
            Uri retVal = null;
            if (!string.IsNullOrEmpty(uriString))
            {
                retVal = new Uri(uriString);
            }
            return retVal;
        }

        public static Uri GetDownloadUriByHash(string hash)
        {
            return new Uri(BeatSaverDownloadUrlHashBase + hash.ToLower());
        }

        public static Uri GetDownloadUriByKey(string key)
        {
            return new Uri(BeatSaverDownloadUrlKeyBase + key.ToLower());
        }

        public static Uri GetBeatSaverDetailsByKey(string key)
        {
            return new Uri(BeatSaverDetailsFromKeyBaseUrl + key.ToLower());
        }

        public static Uri GetBeatSaverDetailsByHash(string hash)
        {
            return new Uri(BeatSaverDetailsFromHashBaseUrl + hash.ToLower());
        }

        /// <summary>
        /// Waits until the provided condition function returns true or the cancellationToken is triggered.
        /// Poll rate is in milliseconds. Returns false if cancellationToken is triggered.
        /// WARNING: If this task doesn't complete or get cancelled it will run until the program ends.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="milliseconds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> WaitUntil(Func<bool> condition, int milliseconds, CancellationToken cancellationToken)
        {
            while (!(condition?.Invoke() ?? true))
            {
                await Task.Yield();
                if (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
                    return false;
                await Task.Delay(milliseconds).ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// Waits until the provided condition function returns true. Poll rate is in milliseconds.
        /// WARNING: If this task doesn't complete it will run until the program ends.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static Task<bool> WaitUntil(Func<bool> condition, int milliseconds)
        {
            return WaitUntil(condition, milliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Waits until the provided condition function returns true. Poll rate and timeout is in milliseconds.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="milliseconds"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>The value of the condition function or false if a timeout occurred.</returns>
        public static async Task<bool> WaitUntil(Func<bool> condition, int milliseconds, int timeoutMilliseconds)
        {
            using (var tcs = new CancellationTokenSource(timeoutMilliseconds))
            {
                return await WaitUntil(condition, milliseconds, tcs.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Waits until the provided condition function returns true. Poll rate is in milliseconds.
        /// WARNING: If this task doesn't complete it will run until the program ends.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static Task<bool> WaitUntil(Func<bool> condition)
        {
            return WaitUntil(condition, 25, CancellationToken.None);
        }

        /// <summary>
        /// Waits until the provided condition function returns true or the cancellationToken is triggered.
        /// Default poll rate is 25ms. Returns false if cancellationToken is triggered.
        /// WARNING: If this task doesn't complete or get cancelled it will run until the program ends.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">Thrown if cancellationToken is triggered.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the cancellationToken's source is disposed.</exception>
        public static Task<bool> WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
        {
            return WaitUntil(condition, 25, cancellationToken);
        }

        public static Task<TResult> GetCancelledTask<TResult>(this CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                throw new ArgumentOutOfRangeException(nameof(cancellationToken), "cancellationToken must be cancelled");
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }
}