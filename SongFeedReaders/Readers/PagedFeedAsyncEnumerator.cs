using SongFeedReaders.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public class PagedFeedAsyncEnumerator : FeedAsyncEnumerator
    {
        protected readonly IPagedFeed PagedFeed;
        private readonly object _pageLock = new object();
        public int LastPage { get; private set; }
        public int StartingPage { get; }
        public int CurrentPage { get; private set; }


        private ConcurrentDictionary<int, Task<PageReadResult>>? CachedPages;

        //public int MaxCachedPages { get; set; }

        public override bool CanMovePrevious { get { return CurrentPage > 1; } }

        public PagedFeedAsyncEnumerator(IPagedFeed feed, int startingPage = 1, bool cachePages = false)
            : base(feed, cachePages)
        {
            LastPage = 0;
            startingPage--;
            if (startingPage < 0) startingPage = 0;
            PagedFeed = feed;
            StartingPage = startingPage;
            CurrentPage = startingPage;
            if (EnablePageCache)
            {
                CachedPages = new ConcurrentDictionary<int, Task<PageReadResult>>();
            }
            CanMoveNext = true;
        }

        private bool TryGetCachedPage(int page, out Task<PageReadResult>? cachedTask)
        {
            cachedTask = null;
            Task<PageReadResult>? cache = null;
            if (EnablePageCache && (CachedPages?.TryGetValue(page, out cache) ?? false))
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
                        PageReadResult? cachedResult = cache.Result;
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

        public override async Task<PageReadResult> MoveNextAsync(CancellationToken cancellationToken)
        {
            int page;
            lock (_pageLock)
            {
                CurrentPage++;
                page = CurrentPage;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return PageReadResult.CancelledResult(PagedFeed.GetUriForPage(page));
            }

            Task<PageReadResult> pageTask;
            PageReadResult? result = null;
            if (TryGetCachedPage(page, out Task<PageReadResult>? cachedTask) && cachedTask != null)
            {
                if (cachedTask.IsCompleted)
                    return await cachedTask.ConfigureAwait(false);
                else
                {
                    using (CancellationTokenSource? tcs = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        bool completed = await Utilities.WaitUntil(() => cachedTask.IsCompleted, tcs.Token).ConfigureAwait(false);
                        if (completed)
                        {
                            result = await cachedTask.ConfigureAwait(false);
                        }
                        else
                        {
                            return PageReadResult.CancelledResult(PagedFeed.GetUriForPage(page));
                        }

                    }
                }
            }
            else
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return PageReadResult.CancelledResult(PagedFeed.GetUriForPage(page));
                }
                Uri uri = PagedFeed.GetUriForPage(page);
                pageTask = PagedFeed.GetSongsAsync(uri, cancellationToken);
                if (EnablePageCache && CachedPages != null)
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


        public override async Task<PageReadResult> MovePreviousAsync(CancellationToken cancellationToken)
        {
            int page;
            lock (_pageLock)
            {
                page = CurrentPage - 1;
                if (page < 1)
                    return new PageReadResult(PagedFeed.GetUriForPage(page), new List<ScrapedSong>(), null, null, 0, new IndexOutOfRangeException($"Page {page} is below the minimum of 1."), PageErrorType.PageOutOfRange);
                CurrentPage--;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return PageReadResult.CancelledResult(PagedFeed.GetUriForPage(page));
            }
            Task<PageReadResult> pageTask;
            PageReadResult? result = null;
            if (TryGetCachedPage(page, out Task<PageReadResult>? cachedTask) && cachedTask != null)
            {
                if (cachedTask.IsCompleted)
                    return await cachedTask.ConfigureAwait(false);
                else
                {
                    using (CancellationTokenSource? tcs = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        bool completed = await Utilities.WaitUntil(() => cachedTask.IsCompleted, tcs.Token).ConfigureAwait(false);
                        if (completed)
                        {
                            result = await cachedTask.ConfigureAwait(false);
                        }
                        else
                        {
                            return PageReadResult.CancelledResult(PagedFeed.GetUriForPage(page));
                        }

                    }
                }
            }
            else
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return PageReadResult.CancelledResult(PagedFeed.GetUriForPage(page));
                }
                Uri uri = PagedFeed.GetUriForPage(page);
                pageTask = PagedFeed.GetSongsAsync(uri, cancellationToken);
                if (EnablePageCache && CachedPages != null)
                    CachedPages.TryAdd(page, pageTask);
                result = await pageTask.ConfigureAwait(false);
            }
            return result;
        }
    }
}
