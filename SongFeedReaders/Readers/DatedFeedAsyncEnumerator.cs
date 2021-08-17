using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public class DatedFeedAsyncEnumerator : FeedAsyncEnumerator
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public IDatedFeed DatedFeed { get; }
        public DateTime CurrentEarliest { get; protected set; }
        public DateTime CurrentLatest { get; protected set; }

        public DatedFeedAsyncEnumerator(IDatedFeed datedFeed, bool cachePages = false)
            : this(datedFeed, FeedDate.Default, cachePages) { }

        public DatedFeedAsyncEnumerator(IDatedFeed datedFeed, FeedDate feedDate, bool cachePages = false)
            : base(datedFeed, cachePages)
        {
            CurrentEarliest = feedDate.Date;
            CurrentLatest = feedDate.Date;
            DatedFeed = datedFeed;
        }

        /// <summary>
        /// Process the result to update enumerator's state.
        /// </summary>
        /// <param name="result"></param>
        private void ProcessResult(PageReadResult? result, DateDirection dateDirection)
        {
            if(result == null || !result.Successful)
            {
                CanMoveNext = false;
                CanMovePrevious = false;
            }
            else if(result.Count > 0)
            {
                CurrentLatest = result.Songs.First().UploadDate;
                CurrentEarliest = result.Songs.Last().UploadDate;
                if (dateDirection == DateDirection.Before)
                    CanMoveNext = true;
                else
                    CanMovePrevious = true;
            }
            else
            {
                if (dateDirection == DateDirection.Before)
                    CanMoveNext = false;
                else
                    CanMovePrevious = false;
            }
        }

        public override async Task<PageReadResult> MoveNextAsync(CancellationToken cancellationToken)
        {
            PageReadResult? result = null;
            Uri? pageUri = null;
            FeedDate feedDate = default;
            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                feedDate = new FeedDate(CurrentEarliest, DateDirection.Before);
                pageUri = DatedFeed.GetUriForDate(feedDate);
                result = await DatedFeed.GetSongsAsync(pageUri, cancellationToken).ConfigureAwait(false);
            }
            catch(OperationCanceledException ex)
            {
                result = PageReadResult.CancelledResult(pageUri, ex);
            }
            catch (Exception ex)
            {
                result = new PageReadResult(pageUri, null, ex, PageErrorType.Unknown);
            }
            finally
            {
                ProcessResult(result, feedDate.Direction);
                _semaphore.Release();
            }

            return result;
        }

        public override Task<PageReadResult> MovePreviousAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}
