using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public interface IFeed
    {
        string Name { get; }
        string DisplayName { get; }
        string Description { get; }
        Uri RootUri { get; }
        string BaseUrl { get; }
        int SongsPerPage { get; }
        bool StoreRawData { get; set; }
        IFeedSettings Settings { get; }

        Task<PageReadResult> GetSongsFromPageAsync(int page, CancellationToken cancellationToken);

        Uri GetUriForPage(int page);

        FeedAsyncEnumerator GetEnumerator();
        FeedAsyncEnumerator GetEnumerator(bool cachePages);
    }

    public class FeedAsyncEnumerator
    {
        readonly IFeed Feed;
        private object _pageLock = new object();
        public int LastPage { get; private set; }
        public int StartingPage { get; }
        public int CurrentPage { get; private set; }

        /// <summary>
        /// How many pages to load forward/backward. Page caching must be enabled.
        /// </summary>
        public int LookAhead { get; set; }

        public bool EnablePageCache { get; }

        private ConcurrentDictionary<int, Task<PageReadResult>> CachedPages;

        //public int MaxCachedPages { get; set; }

        public bool CanMovePrevious { get { return CurrentPage > 1; } }
        public bool CanMoveNext { get; private set; }

        public FeedAsyncEnumerator(IFeed feed, int startingPage = 1, bool cachePages = false)
        {
            LastPage = 0;
            startingPage--;
            if (startingPage < 0) startingPage = 0;
            Feed = feed;
            StartingPage = startingPage;
            CurrentPage = startingPage;
            EnablePageCache = cachePages;
            if (EnablePageCache)
            {
                CachedPages = new ConcurrentDictionary<int, Task<PageReadResult>>();
            }
            CanMoveNext = true;
        }

        private bool TryGetCachedPage(int page, out Task<PageReadResult> cachedTask)
        {
            cachedTask = null;
            if (EnablePageCache && CachedPages.TryGetValue(page, out var cache))
            {
                if (cache != null)
                {
                    if (cache.IsCanceled)
                        return false;
                    if (!cache.IsCompleted)
                    {
                        cachedTask = cache;
                        return true;
                    }
                    else if (!cache.IsFaulted)
                    {
                        var cachedResult = cache.Result;
                        if (cachedResult.Successful)
                        {
                            cachedTask = cache;
                            return true;
                        }
                        else
                            CachedPages.TryRemove(page, out _);
                    }
                    else
                        CachedPages.TryRemove(page, out _);
                }
            }
            return false;
        }

        public async Task<PageReadResult> MoveNextAsync(CancellationToken cancellationToken)
        {
            int page;
            lock (_pageLock)
            {
                CurrentPage++;
                page = CurrentPage;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
            }

            Task<PageReadResult> pageTask;
            PageReadResult result = null;
            if (TryGetCachedPage(page, out var cachedTask))
            {
                if (cachedTask.IsCompleted)
                    return await cachedTask.ConfigureAwait(false);
                else
                {
                    using (var tcs = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        bool completed = await Utilities.WaitUntil(() => cachedTask.IsCompleted, tcs.Token).ConfigureAwait(false);
                        if (completed)
                        {
                            result = await cachedTask.ConfigureAwait(false);
                        }
                        else
                        {
                            return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
                        }

                    }
                }
            }
            else
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
                }
                pageTask = Feed.GetSongsFromPageAsync(page, cancellationToken);
                if (EnablePageCache)
                    CachedPages.TryAdd(page, pageTask);
                result = await pageTask.ConfigureAwait(false);
            }
            if (result.IsLastPage)
            {
                CanMoveNext = false;
                LastPage = Math.Min(LastPage, page);
            }

            return result;
        }


        public async Task<PageReadResult> MovePreviousAsync(CancellationToken cancellationToken)
        {
            int page;
            lock (_pageLock)
            {
                page = CurrentPage - 1;
                if (page < 1)
                    return new PageReadResult(Feed.GetUriForPage(page), new List<ScrapedSong>(), page, new IndexOutOfRangeException($"Page {page} is below the minimum of 1."), PageErrorType.PageOutOfRange);
                CurrentPage--;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
            }
            Task<PageReadResult> pageTask;
            PageReadResult result = null;
            if (TryGetCachedPage(page, out var cachedTask))
            {
                if (cachedTask.IsCompleted)
                    return await cachedTask.ConfigureAwait(false);
                else
                {
                    using (var tcs = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        bool completed = await Utilities.WaitUntil(() => cachedTask.IsCompleted, tcs.Token).ConfigureAwait(false);
                        if (completed)
                        {
                            result = await cachedTask.ConfigureAwait(false);
                        }
                        else
                        {
                            return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
                        }

                    }
                }
            }
            else
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
                }
                pageTask = Feed.GetSongsFromPageAsync(page, cancellationToken);
                if (EnablePageCache)
                    CachedPages.TryAdd(page, pageTask);
                result = await pageTask.ConfigureAwait(false);
            }
            return result;
        }


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
