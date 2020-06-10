using SongFeedReaders.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public interface IFeed
    {
        /// <summary>
        /// Name of the feed.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// DisplayName of the feed.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Description of the feed.
        /// </summary>
        string Description { get; }
        /// <summary>
        /// The feed's root URI.
        /// </summary>
        Uri RootUri { get; }
        /// <summary>
        /// Base URL used by the feed for constructing the full URI.
        /// </summary>
        string BaseUrl { get; }
        /// <summary>
        /// Number of songs the feed returns per page.
        /// </summary>
        int SongsPerPage { get; }
        /// <summary>
        /// If true, store the full data from the feed for each song as a JSON string.
        /// </summary>
        bool StoreRawData { get; set; }
        /// <summary>
        /// Returns true if the settings are valid for the feed.
        /// </summary>
        IFeedSettings Settings { get; }
        /// <summary>
        /// Returns true if the <see cref="Settings"/> are valid for the feed, false otherwise.
        /// </summary>
        bool HasValidSettings { get; }

        /// <summary>
        /// Throws an <see cref="InvalidFeedSettingsException"/> when the feed's settings aren't valid.
        /// </summary>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        void EnsureValidSettings();
        /// <summary>
        /// Attempts to fetch the specified page and returns it as a <see cref="PageReadResult"/>. 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        /// <returns></returns>
        Task<PageReadResult> GetSongsFromPageAsync(int page, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the feed's full URI for the specified page.
        /// </summary>
        /// <param name="page"></param>
        /// <exception cref="InvalidFeedSettingsException">Thrown when the feed's settings aren't valid.</exception>
        /// <returns></returns>
        Uri GetUriForPage(int page);
        /// <summary>
        /// Returns a <see cref="FeedAsyncEnumerator"/> to handle page navigation for the feed.
        /// </summary>
        /// <returns></returns>
        FeedAsyncEnumerator GetEnumerator();
        /// <summary>
        /// Returns a <see cref="FeedAsyncEnumerator"/> to handle page navigation for the feed. 
        /// If cachePages is true, the <see cref="FeedAsyncEnumerator"/> will store the pages that were fetched.
        /// </summary>
        /// <returns></returns>
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
            if (EnablePageCache && CachedPages.TryGetValue(page, out Task<PageReadResult>? cache))
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
            PageReadResult? result = null;
            if (TryGetCachedPage(page, out Task<PageReadResult>? cachedTask))
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
                    return new PageReadResult(Feed.GetUriForPage(page), new List<IScrapedSong>(), page, new IndexOutOfRangeException($"Page {page} is below the minimum of 1."), PageErrorType.PageOutOfRange);
                CurrentPage--;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return PageReadResult.CancelledResult(Feed.GetUriForPage(page), page);
            }
            Task<PageReadResult> pageTask;
            PageReadResult? result = null;
            if (TryGetCachedPage(page, out Task<PageReadResult>? cachedTask))
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
