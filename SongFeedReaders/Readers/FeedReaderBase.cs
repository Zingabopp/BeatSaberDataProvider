using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public abstract class FeedReaderBase : IFeedReader
    {
        public abstract string Name { get; }
        public abstract string Source { get; }
        public abstract Uri RootUri { get; }
        public abstract bool Ready { get; protected set; }
        public virtual bool StoreRawData { get; set; }
        public abstract void PrepareReader();
        public abstract string GetFeedName(IFeedSettings settings);
        public abstract Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, IProgress<ReaderProgress> progress, CancellationToken cancellationToken);
        #region Overloads
        public virtual FeedResult GetSongsFromFeed(IFeedSettings settings)
        {
            try
            {
                return GetSongsFromFeedAsync(settings, default(IProgress<ReaderProgress>), CancellationToken.None).Result;
            }
            catch (AggregateException ex)
            {
                var flattened = ex.Flatten();
                if (flattened.InnerExceptions.Count == 1)
                {
                    throw flattened.InnerException;
                }
                throw ex;
            }
        }
        public virtual FeedResult GetSongsFromFeed(IFeedSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                return GetSongsFromFeedAsync(settings, default(IProgress<ReaderProgress>), cancellationToken).Result;
            }
            catch (AggregateException ex)
            {
                var flattened = ex.Flatten();
                if (flattened.InnerExceptions.Count == 1)
                {
                    throw flattened.InnerException;
                }
                throw ex;
            }
        }
        public virtual Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken) => GetSongsFromFeedAsync(settings, default(IProgress<ReaderProgress>), CancellationToken.None);
        public virtual Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings) => GetSongsFromFeedAsync(settings, default(IProgress<ReaderProgress>), CancellationToken.None);
         #endregion
    }
}
