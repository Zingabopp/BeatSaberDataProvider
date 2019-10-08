using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public class FeedReaderException : Exception
    {
        public FeedReaderFailureCode FailureCode { get; protected set; }

        public override string Message
        {
            get
            {
                if (string.IsNullOrEmpty(base.Message) && InnerException != null)
                {
                    return InnerException.Message;
                }
                return base.Message;
            }
        }

        public FeedReaderException()
        { }

        public FeedReaderException(string message)
            : base(message)
        { }

        public FeedReaderException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public FeedReaderException(string message, Exception innerException, FeedReaderFailureCode reason)
            : base(message, innerException)
        {
            FailureCode = reason;
        }
    }

    public enum FeedReaderFailureCode
    {
        Generic = 0,
        SourceFailed = 1,
        PageFailed = 2
    }
}
