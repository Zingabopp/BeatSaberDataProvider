using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Logging;

namespace SongFeedReaders
{
    public static class Utilities
    {
        static Utilities()
        {
            Logger = new FeedReaderLogger(LoggingController.DefaultLogController);
            MaxAggregateExceptionDepth = 10;
        }

        public static bool IsPaused { get; internal set; }

        public static FeedReaderLoggerBase Logger { get; set; } 
        public static int MaxAggregateExceptionDepth { get; set; }

        public static void WriteExceptions(this AggregateException ae, string message)
        {
            Logger.Exception(message, ae);
            for (int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
                if (ae.InnerExceptions[i] is AggregateException ex)
                    WriteExceptions(ex, 0); // TODO: This could get very long
            }
        }
        public static void WriteExceptions(this AggregateException ae, int depth = 0)
        {
            for (int i = 0; i < ae.InnerExceptions.Count; i++)
            {
                Logger.Exception($"Exception {i}:\n", ae.InnerExceptions[i]);
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
            if(!string.IsNullOrEmpty(uriString))
            {
                retVal = new Uri(uriString);
            }
            return retVal;
        }

        /// <summary>
        /// Waits until the provided condition function returns true or the cancellationToken is triggered.
        /// Poll rate is in milliseconds. Returns false if cancellationToken is triggered.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="milliseconds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> WaitUntil(Func<bool> condition, int milliseconds, CancellationToken cancellationToken)
        {
            while(!(condition?.Invoke() ?? true))
            {
                if (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
                    return false;
                await Task.Delay(milliseconds).ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// Waits until the provided condition function returns true. Poll rate is in milliseconds.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static Task<bool> WaitUntil(Func<bool> condition, int milliseconds)
        {
            return WaitUntil(condition, milliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Waits until the provided condition function returns true or the cancellationToken is triggered.
        /// Default poll rate is 25ms. Returns false if cancellationToken is triggered.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<bool> WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
        {
            return WaitUntil(condition, 25, cancellationToken);
        }
    }
}
