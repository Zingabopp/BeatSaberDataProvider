using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
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
        /// <returns></returns>
        FeedResult GetSongsFromFeed(IFeedSettings settings, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the songs from a feed and returns them in a <see cref="FeedResult"/>.
        /// Non-critical exceptions are returned in the <see cref="FeedResult"/> as a <see cref="FeedReaderException"/> more specific exceptions can be found in <see cref="FeedReaderException">FeedReaderException.InnerException</see>.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the <see cref="IFeedSettings"/> doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
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
        /// <returns></returns>
        Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken);
    }

    public interface IFeedSettings
    {
        string FeedName { get; } // Name of the feed
        int FeedIndex { get; } // Index of the feed 

        /// <summary>
        /// Number of songs per page.
        /// </summary>
        int SongsPerPage { get; }

        /// <summary>
        /// Max number of songs to retrieve, 0 for unlimited.
        /// </summary>
        int MaxSongs { get; set; }

        /// <summary>
        /// Page of the feed to start on, default is 1. For all feeds, setting '1' here is the same as starting on the first page.
        /// </summary>
        int StartingPage { get; set; }
    }

    /// <summary>
    /// Data for a feed.
    /// </summary>
    public struct FeedInfo : IEquatable<FeedInfo>
    {
#pragma warning disable CA1054 // Uri parameters should not be strings
        public FeedInfo(string name, string displayName, string baseUrl, string description)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            Name = name;
            _displayName = displayName;
            BaseUrl = baseUrl;
            Description = description;
        }
#pragma warning disable CA1056 // Uri properties should not be strings
        public string BaseUrl { get; private set; } // Base URL for the feed, has string keys to replace with things like page number/bsaber username
#pragma warning restore CA1056 // Uri properties should not be strings
        public string Name { get; private set; } // Name of the feed
        private string _displayName;
        public string DisplayName { get { return string.IsNullOrEmpty(_displayName) ? Name : _displayName; } set { _displayName = value; } }
        public string Description { get; set; }
        #region EqualsOperators
        public override bool Equals(object obj)
        {
            if (!(obj is FeedInfo))
                return false;
            return Equals((FeedInfo)obj);
        }
        public bool Equals(FeedInfo other)
        {
            if (Name != other.Name)
                return false;
            return BaseUrl == other.BaseUrl;
        }

        public static bool operator ==(FeedInfo feedInfo1, FeedInfo feedInfo2)
        {
            return feedInfo1.Equals(feedInfo2);
        }
        public static bool operator !=(FeedInfo feedInfo1, FeedInfo feedInfo2)
        {
            return !feedInfo1.Equals(feedInfo2);
        }

        public override int GetHashCode()
        {
            return new HashablePair(Name, BaseUrl).GetHashCode();
        }

        struct HashablePair
        {
            public HashablePair(string name, string baseUrl)
            {
                Name = name;
                BaseUrl = baseUrl;
            }
            string Name;
            string BaseUrl;
        }
        #endregion
    }

}
