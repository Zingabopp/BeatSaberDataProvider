﻿using System;
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
        /// Retrieves the songs from a feed and returns them as a FeedResult. Non-critical exceptions are returned in the FeedResult (AggregateException if there are multiple).
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the IFeedSettings doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
        /// <returns></returns>
        FeedResult GetSongsFromFeed(IFeedSettings settings);

        /// <summary>
        /// Retrieves the songs from a feed and returns them as a FeedResult. Non-critical exceptions are returned in the FeedResult (AggregateException if there are multiple).
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="InvalidCastException">Thrown when the IFeedSettings doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
        /// <returns></returns>
        Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings);
        /// <summary>
        /// Retrieves the songs from a feed and returns them as a FeedResult. Non-critical exceptions are returned in the FeedResult (AggregateException if there are multiple).
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidCastException">Thrown when the IFeedSettings doesn't match the reader type.</exception>
        /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
        /// <returns></returns>
        Task<FeedResult> GetSongsFromFeedAsync(IFeedSettings settings, CancellationToken cancellationToken);
    }

    public interface IFeedSettings
    {
        string FeedName { get; } // Name of the feed
        int FeedIndex { get; } // Index of the feed 

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
        public FeedInfo(string name, string displayName, string baseUrl)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            Name = name;
            DisplayName = displayName;
            BaseUrl = baseUrl;
        }
#pragma warning disable CA1056 // Uri properties should not be strings
        public string BaseUrl { get; set; } // Base URL for the feed, has string keys to replace with things like page number/bsaber username
#pragma warning restore CA1056 // Uri properties should not be strings
        public string Name { get; set; } // Name of the feed
        public string DisplayName { get; set; }

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
