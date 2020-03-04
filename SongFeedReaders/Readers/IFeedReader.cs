using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public struct ReaderProgress
    {
        public readonly int CurrentPage;
        public readonly int SongCount;
        public ReaderProgress(int currentPage, int totalSongs)
        {
            CurrentPage = currentPage;
            SongCount = totalSongs;
        }
    }
    public interface IFeedReader
    {
        string Name { get; } // Name of the reader
        string Source { get; } // Name of the site
        Uri RootUri { get; }
        bool Ready { get; } // Reader is ready
        bool StoreRawData { get; set; } // Save the raw data in ScrapedSong

        
        /// <summary>
        /// Anything that needs to happen before the Reader is ready.
        /// </summary>
        void PrepareReader();

        string GetFeedName(IFeedSettings settings);

        /// <summary>
        /// Retrieves the songs from a feed and returns them in a <see cref="FeedResult"/>.
        /// Non-critical exceptions are returned in the <see cref="FeedResult"/> as a <see cref="FeedReaderException"/> more specific exceptions can be found in <see cref="FeedReaderException">FeedReaderException.InnerException</see>.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the <see cref="IFeedSettings"/> doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        /// <exception cref="InvalidFeedSettingsException">Throw when <paramref name="settings"/> is not valid for the feed.</exception>
        /// <returns></returns>
        FeedResult GetSongsFromFeed(IFeedSettings settings);

        /// <summary>
        /// Retrieves the songs from a feed and returns them in a <see cref="FeedResult"/>.
        /// Cancellation does not throw an exception. If aborted early, the <see cref="FeedResult.ErrorCode"/> will indicate cancellation.
        /// Non-critical exceptions are returned in the <see cref="FeedResult"/> as a <see cref="FeedReaderException"/> more specific exceptions can be found in <see cref="FeedReaderException">FeedReaderException.InnerException</see>.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidCastException">Thrown when the <see cref="IFeedSettings"/> doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        /// <exception cref="InvalidFeedSettingsException">Throw when <paramref name="settings"/> is not valid for the feed.</exception>
        /// <returns></returns>
        FeedResult GetSongsFromFeed(IFeedSettings settings, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the songs from a feed and returns them in a <see cref="FeedResult"/>.
        /// Non-critical exceptions are returned in the <see cref="FeedResult"/> as a <see cref="FeedReaderException"/> more specific exceptions can be found in <see cref="FeedReaderException">FeedReaderException.InnerException</see>.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the <see cref="IFeedSettings"/> doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        /// <exception cref="InvalidFeedSettingsException">Throw when <paramref name="settings"/> is not valid for the feed.</exception>
        /// <returns></returns>
        Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings);
        /// <summary>
        /// Retrieves the songs from a feed and returns them in a <see cref="FeedResult"/>.
        /// Cancellation does not throw an exception. If aborted early, the <see cref="FeedResult.ErrorCode"/> will indicate cancellation.
        /// Non-critical exceptions are returned in the <see cref="FeedResult"/> as a <see cref="FeedReaderException"/> more specific exceptions can be found in <see cref="FeedReaderException">FeedReaderException.InnerException</see>.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidCastException">Thrown when the <see cref="IFeedSettings"/> doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        /// <exception cref="InvalidFeedSettingsException">Throw when <paramref name="settings"/> is not valid for the feed.</exception>
        /// <returns></returns>
        Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, IProgress<ReaderProgress> progress, CancellationToken cancellationToken);
    }
}
