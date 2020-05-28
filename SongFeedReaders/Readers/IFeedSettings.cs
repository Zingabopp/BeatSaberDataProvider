using System;
using SongFeedReaders.Data;

namespace SongFeedReaders.Readers
{
    public interface IFeedSettings : ICloneable
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

        /// <summary>
        /// Only return songs that return true for this function.
        /// </summary>
        Func<ScrapedSong, bool>? Filter { get; set; }

        /// <summary>
        /// If this returns true for any <see cref="ScrapedSong"/>, treat that page as the last.
        /// </summary>
        Func<ScrapedSong, bool>? StopWhenAny { get; set; }
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
