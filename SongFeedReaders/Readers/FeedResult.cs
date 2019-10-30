using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    /// <summary>
    /// Wraps the result of a feed. Can contain Exceptions that occurred in the process of getting the results. Songs is never null.
    /// The Exception will either be a FeedReaderException or an AggregateException containing multiple FeedReaderExceptions. FeedReaderExceptions may contain a more specific Exception in InnerExcepion.
    /// </summary>
    public class FeedResult
    {
        public PageReadResult[] PageResults { get; private set; }
        public PageReadResult[] FaultedResults { get; private set; }
        /// <summary>
        /// Distinct page errors.
        /// </summary>
        public PageErrorType[] PageErrors { get; private set; }
        public int PagesChecked { get { return PageResults?.Count() ?? 0; } }
        public IReadOnlyDictionary<string, ScrapedSong> Songs { get; private set; }
        public int Count { get { return Songs?.Count ?? 0; } }
        public FeedResultErrorLevel ErrorLevel { get; private set; }
        private bool _successful;
        public bool Successful { get { return _successful && Exception == null; } }
        /// <summary>
        /// Exception when something goes wrong in the feed readers. More specific exceptions may be stored in InnerException.
        /// </summary>
        public FeedReaderException Exception { get; private set; }

        public FeedResult(Dictionary<string, ScrapedSong> songs, IEnumerable<PageReadResult> pageResults)
        {
            PageResults = pageResults?.ToArray() ?? new PageReadResult[0];
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
                PageErrors = pageErrors.Distinct().ToArray();
                if (errorCount == PagesChecked)
                {
                    ErrorLevel = FeedResultErrorLevel.Error;
                }
                else
                {
                    if (ErrorLevel < FeedResultErrorLevel.Warning)
                        ErrorLevel = FeedResultErrorLevel.Warning;
                }
            }
            Songs = new ReadOnlyDictionary<string, ScrapedSong>(songs);
            FaultedResults = faultedResults.ToArray();
        }

        public FeedResult(Dictionary<string, ScrapedSong> songs, IEnumerable<PageReadResult> pageResults, Exception exception, FeedResultErrorLevel errorLevel)
            : this(songs, pageResults)
        {
            if(ErrorLevel < errorLevel)
                ErrorLevel = errorLevel;
            if (exception != null)
            {
                if (exception is FeedReaderException frException)
                {
                    Exception = frException;
                }
                else
                    Exception = new FeedReaderException(exception.Message, exception);
            }
        }


    }
    public enum FeedResultErrorLevel
    {
        None = 0,
        Warning = 1,
        Error = 2
    }
}
