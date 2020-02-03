using System;
using SongFeedReaders.Data;

namespace SongFeedReaders.Readers.BeastSaber
{
    public class BeastSaberFeedSettings : IFeedSettings
    {
        #region Property Fields
        private int _feedIndex;
        private int _startingPage;
        private int _maxSongs;
        private int _maxPages;
        #endregion

        #region IFeedSettings
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return BeastSaberFeed.Feeds[Feed].Name; } }

        /// <summary>
        /// Index of the feed defined by <see cref="BeastSaberFeedName"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a value that is not a valid <see cref="BeastSaberFeedName"/></exception>
        public int FeedIndex
        {
            get { return _feedIndex; }
            set
            {
                if (!Enum.IsDefined(typeof(BeastSaberFeedName), value))
                    throw new ArgumentOutOfRangeException($"Failed to set FeedIndex: No BeastSaberFeed defined for an index of {value}.");
                _feedIndex = value;
            }
        }

        /// <summary>
        /// Number of songs per page for this feed.
        /// </summary>
        public int SongsPerPage { get { return FeedIndex == 0 ? BeastSaberFeed.SongsPerXmlPage : BeastSaberFeed.SongsPerJsonPage; } }

        /// <summary>
        /// Maximum songs to retrieve, will stop the reader before MaxPages is met. Use 0 for unlimited.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 0.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 0.</exception>
        public int MaxSongs
        {
            get { return _maxSongs; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxSongs), "MaxSongs cannot be less than 0.");
                _maxSongs = value;
            }
        }

        /// <summary>
        /// Page of the feed to start on, default is 1. Setting '1' here is the same as starting on the first page.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 1.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 1.</exception>
        public int StartingPage
        {
            get { return _startingPage; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(StartingPage), "StartingPage cannot be less than 1.");
                _startingPage = value;
            }
        }
        public Func<ScrapedSong, bool> Filter { get; set; }
        public Func<ScrapedSong, bool> StopWhenAny { get; set; }

        public object Clone()
        {
            return new BeastSaberFeedSettings(Feed)
            {
                MaxPages = MaxPages,
                MaxSongs = MaxSongs,
                StartingPage = StartingPage,
                Username = Username,
                Filter = (Func<ScrapedSong, bool>)Filter?.Clone(),
                StopWhenAny = (Func<ScrapedSong, bool>)StopWhenAny?.Clone(),
            };
        }
        #endregion

        public BeastSaberFeedName Feed
        {
            get { return (BeastSaberFeedName)FeedIndex; }
            set
            {
                FeedIndex = (int)value;
            }
        }

        /// <summary>
        /// Maximum pages to check, will stop the reader before MaxSongs is met. Use 0 for unlimited.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 0.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 0.</exception>
        public int MaxPages
        {
            get { return _maxPages; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxPages), "MaxPages cannot be less than 0.");
                _maxPages = value;
            }
        }

        /// <summary>
        /// Username to use when fetching the Following and Bookmarks feeds.
        /// </summary>
        public string Username { get; set; }

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="feedIndex"></param>
        /// <param name="maxPages"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="feedIndex"/> is not a valid <see cref="BeastSaberFeedName"/></exception>
        public BeastSaberFeedSettings(int feedIndex, string username = null)
        {
            if (!Enum.IsDefined(typeof(BeastSaberFeedName), feedIndex))
                throw new ArgumentOutOfRangeException(nameof(feedIndex), $"No BeastSaberFeed defined for an index of {feedIndex}.");
            FeedIndex = feedIndex;
            MaxPages = 0;
            StartingPage = 1;
            Username = username ?? string.Empty;
        }

        public BeastSaberFeedSettings(BeastSaberFeedName feed, string username = null)
        {
            Feed = feed;
            MaxPages = 0;
            StartingPage = 1;
            Username = username ?? string.Empty;
        }
        #endregion
    }

    public enum BeastSaberFeedName
    {
        Following = 0,
        Bookmarks = 1,
        CuratorRecommended = 2
    }
}
