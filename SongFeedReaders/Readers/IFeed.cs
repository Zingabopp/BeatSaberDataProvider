using System;
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

        Task<PageReadResult> GetSongsAsync(Uri uri, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a <see cref="FeedAsyncEnumerator"/> to handle page navigation for the feed.
        /// </summary>
        /// <returns></returns>
        FeedAsyncEnumerator GetEnumerator();
        /// <summary>
        /// Returns a <see cref="FeedAsyncEnumerator"/> to handle page navigation for the feed. 
        /// If cachePages is true, the <see cref="FeedAsyncEnumerator"/> will store the pages that were fetched, if supported.
        /// </summary>
        /// <returns></returns>
        FeedAsyncEnumerator GetEnumerator(bool cachePages);
    }
}
