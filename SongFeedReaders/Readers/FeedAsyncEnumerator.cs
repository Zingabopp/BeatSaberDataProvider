using SongFeedReaders.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public abstract class FeedAsyncEnumerator
    {
        protected readonly IFeed Feed;

        /// <summary>
        /// How many pages to load forward/backward. Page caching must be enabled.
        /// </summary>
        public int LookAhead { get; set; }

        public bool EnablePageCache { get; }

        public virtual bool CanMovePrevious { get; protected set; }
        public virtual bool CanMoveNext { get; protected set; }

        protected FeedAsyncEnumerator(IFeed feed, bool cachePages = false)
        {
            Feed = feed;
            EnablePageCache = cachePages;
            CanMoveNext = true;
        }

        public abstract Task<PageReadResult> MoveNextAsync(CancellationToken cancellationToken);


        public abstract Task<PageReadResult> MovePreviousAsync(CancellationToken cancellationToken);


        public Task<PageReadResult> MoveNextAsync()
        {
            return MoveNextAsync(CancellationToken.None);
        }

        public Task<PageReadResult> MovePreviousAsync()
        {
            return MovePreviousAsync(CancellationToken.None);
        }
    }

}
