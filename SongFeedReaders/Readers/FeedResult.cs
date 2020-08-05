using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using SongFeedReaders.Data;

namespace SongFeedReaders.Readers
{
    /// <summary>
    /// Wraps the result of a feed. Can contain Exceptions that occurred in the process of getting the results. Songs is never null.
    /// The Exception will either be a FeedReaderException or an AggregateException containing multiple FeedReaderExceptions. FeedReaderExceptions may contain a more specific Exception in InnerExcepion.
    /// </summary>
    public class FeedResult
    {
        public IReadOnlyList<PageReadResult> PageResults { get; private set; }
        public IReadOnlyList<PageReadResult> FaultedResults { get; private set; }
        /// <summary>
        /// Distinct page errors.
        /// </summary>
        public IReadOnlyList<PageErrorType>? PageErrors { get; private set; }
        public int PagesChecked { get { return PageResults?.Count() ?? 0; } }
        public IReadOnlyDictionary<string, ScrapedSong> Songs { get; private set; }
        public int Count { get { return Songs?.Count ?? 0; } }
        public FeedResultError ErrorCode { get; private set; }
        private bool _successful;
        public bool Successful { get { return _successful && ErrorCode != FeedResultError.Error && ErrorCode != FeedResultError.Cancelled; } }
        /// <summary>
        /// Exception when something goes wrong in the feed readers. More specific exceptions may be stored in InnerException.
        /// </summary>
        public FeedReaderException? Exception { get; private set; }

        public FeedResult(Dictionary<string, ScrapedSong>? songs, IList<PageReadResult>? pageResults)
        {
            PageResults = new ReadOnlyCollection<PageReadResult>(pageResults ?? new PageReadResult[0]);
            if(songs == null)
                songs = new Dictionary<string, ScrapedSong>();
            var pageErrors = new List<PageErrorType>();
            var faultedResults = new List<PageReadResult>();
            if (PageResults != null)
            {
                _successful = true;
                foreach (var pageResult in PageResults)
                {
                    if (!pageResult.Successful)
                    {
                        pageErrors.Add(pageResult.PageError);
                        faultedResults.Add(pageResult);
                    }
                }
            }
            else
            {
                _successful = false;

            }
            int errorCount = pageErrors.Count;
            if (errorCount > 0)
            {
                PageErrors = new ReadOnlyCollection<PageErrorType>(pageErrors.Distinct().ToArray());
                if (errorCount == PagesChecked)
                {
                    ErrorCode = FeedResultError.Error;
                }
                else
                {
                    if (ErrorCode < FeedResultError.Warning)
                        ErrorCode = FeedResultError.Warning;
                }
            }
            Songs = new ReadOnlyDictionary<string, ScrapedSong>(songs);
            FaultedResults = new ReadOnlyCollection<PageReadResult>(faultedResults);
        }

        public FeedResult(Dictionary<string, ScrapedSong>? songs, IList<PageReadResult>? pageResults, Exception? exception, FeedResultError errorLevel)
            : this(songs, pageResults)
        {
            if(ErrorCode < errorLevel)
                ErrorCode = errorLevel;

            if (exception != null)
            {
                if (exception is FeedReaderException frException)
                {
                    Exception = frException;
                }
                else if(exception is OperationCanceledException canceledException)
                {
                    ErrorCode = FeedResultError.Cancelled;
                    Exception = new FeedReaderException(canceledException.Message, canceledException, FeedReaderFailureCode.Cancelled);
                }
                else
                    Exception = new FeedReaderException(exception.Message, exception);
            }
        }

        public static FeedResult GetCancelledResult(Dictionary<string, ScrapedSong>? songs, IList<PageReadResult>? pageResults, OperationCanceledException ex)
        {
            return new FeedResult(songs, pageResults, ex, FeedResultError.Cancelled);
        }
        public static FeedResult GetCancelledResult(Dictionary<string, ScrapedSong>? songs, IList<PageReadResult>? pageResults)
        {
            return new FeedResult(songs, pageResults, new OperationCanceledException("Feed was cancelled before completion"), FeedResultError.Cancelled);
        }

        public static FeedResult CancelledResult => GetCancelledResult(null, null);
    }
    public enum FeedResultError
    {
        None = 0,
        Warning = 1,
        Error = 2,
        Cancelled = 3
    }
}
