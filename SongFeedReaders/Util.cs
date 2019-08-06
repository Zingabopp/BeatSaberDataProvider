using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
