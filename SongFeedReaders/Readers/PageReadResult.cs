using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebUtilities;
using SongFeedReaders.Logging;
using SongFeedReaders.Data;

namespace SongFeedReaders.Readers
{
    public class PageReadResult
    {
        private static FeedReaderLoggerBase? _logger;
        public static FeedReaderLoggerBase Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }
        public Uri Uri { get; private set; }
        public List<ScrapedSong>? Songs { get; private set; }

        public bool IsLastPage { get; private set; }

        public PageErrorType PageError { get; private set; }
        public int Count { get { return Songs?.Count ?? 0; } }
        public FeedReaderException? Exception { get; private set; }

        private bool _successful;
        public bool Successful { get { return _successful && Exception == null; } }
        public PageReadResult(Uri uri, List<ScrapedSong>? songs, bool isLastPage = false)
        {
            IsLastPage = isLastPage;
            Uri = uri;
            if (songs == null)
            {
                _successful = false;
                songs = new List<ScrapedSong>();
            }
            else
                _successful = true;
            Songs = songs;
        }

        public PageReadResult(Uri uri, List<ScrapedSong>? songs, Exception? exception, PageErrorType pageError, bool isLastPage = false)
            : this(uri, songs, isLastPage)
        {

            if (exception != null)
            {
                if (pageError == PageErrorType.None)
                    pageError = PageErrorType.Unknown;
                PageError = pageError;
                if (exception is FeedReaderException frException)
                {
                    Exception = frException;
                }
                else
                    Exception = new FeedReaderException(exception.Message, exception);
            }
            else
            {
                if (pageError > PageError)
                    PageError = pageError;
            }
        }

        public override string ToString()
        {
            return $"{Uri} | {Songs?.Count.ToString() ?? "<NULL>"}";
        }

        public static PageReadResult FromWebClientException(WebClientException? ex, Uri requestUri)
        {
            PageErrorType pageError = PageErrorType.SiteError;
            string errorText = string.Empty;
            int statusCode = ex?.Response?.StatusCode ?? 0;
            if (statusCode != 0)
            {
                switch (statusCode)
                {
                    case 408:
                        errorText = "Timeout";
                        pageError = PageErrorType.Timeout;
                        break;
                    default:
                        errorText = "Site Error";
                        pageError = PageErrorType.SiteError;
                        break;
                }
            }
            string message = $"{errorText} getting page '{requestUri}'.";
            Logger?.Debug(message);
            // No need for a stacktrace if it's one of these errors.
            if (!(pageError == PageErrorType.Timeout || statusCode == 500) && ex != null)
                Logger?.Debug($"{ex.Message}\n{ex.StackTrace}");
            return new PageReadResult(requestUri, null, new FeedReaderException(message, ex, FeedReaderFailureCode.PageFailed), pageError);
        }

        public static PageReadResult CancelledResult(Uri requestUri, OperationCanceledException ex)
        {
            return new PageReadResult(requestUri, new List<ScrapedSong>(), ex, PageErrorType.Cancelled);
        }

        public static PageReadResult CancelledResult(Uri requestUri)
        {
            return new PageReadResult(requestUri, new List<ScrapedSong>(), new OperationCanceledException(), PageErrorType.Cancelled);
        }
    }


    public static class PageErrorTypeExtensions
    {
        public static string ErrorToString(this PageErrorType pageError)
        {
            switch (pageError)
            {
                case PageErrorType.None:
                    return string.Empty;
                case PageErrorType.Timeout:
                    return "Timeout";
                case PageErrorType.SiteError:
                    return "Site Error";
                case PageErrorType.ParsingError:
                    return "Parsing Error";
                case PageErrorType.PageOutOfRange:
                    return "Page out of range";
                case PageErrorType.Unknown:
                    return "Unknown Error";
                default:
                    return "Unknown Error";
            }
        }
    }
    public enum PageErrorType
    {
        None = 0,
        Timeout = 1,
        SiteError = 2,
        ParsingError = 3,
        Cancelled = 4,
        Unknown = 5,
        PageOutOfRange = 6
    }
}
