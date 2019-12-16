using System;
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


    }

    public class FeedAsyncEnumerator
    {
        readonly IFeed Feed;
        private object _pageLock = new object();
        private int _lastPage;
        public int StartingPage { get; }
        public int CurrentPage { get; private set; }

        public bool CanMovePrevious { get { return CurrentPage > 1; } }
        public bool CanMoveNext { get; private set; }

        public FeedAsyncEnumerator(IFeed feed, int startingPage = 1)
        {
            _lastPage = 0;
            if (startingPage < 1) startingPage = 1;
            Feed = feed;
            StartingPage = startingPage;
            CurrentPage = startingPage;
            CanMoveNext = true;
        }

        public async Task<PageReadResult> MoveNextAsync(CancellationToken cancellationToken)
        {
            int page;
            lock (_pageLock)
            {
                page = CurrentPage;
                CurrentPage++;
            }
            var result = await Feed.GetSongsFromPageAsync(page, cancellationToken).ConfigureAwait(false);
            if (result.IsLastPage)
            {
                CanMoveNext = false;
                _lastPage = Math.Min(_lastPage, page);
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
                    return new PageReadResult(null, new List<ScrapedSong>(), page, new IndexOutOfRangeException($"Page {page} is below the minimum of 1."), PageErrorType.PageOutOfRange);
                CurrentPage--;
            }
            return await Feed.GetSongsFromPageAsync(page, cancellationToken).ConfigureAwait(false);
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
