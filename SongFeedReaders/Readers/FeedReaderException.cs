using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
#pragma warning disable CA2237 // Mark ISerializable types with serializable
    public class FeedReaderException : Exception
#pragma warning restore CA2237 // Mark ISerializable types with serializable
    {
        /// <summary>
        /// A <see cref="FeedReaderFailureCode"/> associated with the exception.
        /// </summary>
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

        public FeedReaderException(string message, Exception? innerException)
            : base(message, innerException)
        { }

        public FeedReaderException(string message, Exception? innerException, FeedReaderFailureCode reason)
            : base(message, innerException)
        {
            FailureCode = reason;
        }
    }


    public enum FeedReaderFailureCode
    {
        /// <summary>
        /// Generic error.
        /// </summary>
        Generic = 0,
        /// <summary>
        /// All pages failed, likely a site problem.
        /// </summary>
        SourceFailed = 1,
        /// <summary>
        /// Some pages failed.
        /// </summary>
        PageFailed = 2,
        /// <summary>
        /// CancellationToken was triggered before the reader finished.
        /// </summary>
        Cancelled = 3
    }
}
