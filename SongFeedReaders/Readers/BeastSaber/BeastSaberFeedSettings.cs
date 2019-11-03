using System;

namespace SongFeedReaders.Readers.BeastSaber
{
    public class BeastSaberFeedSettings : IFeedSettings
    {
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return BeastSaberReader.Feeds[Feed].Name; } }
        private int _feedIndex;
        private int _startingPage;
        private int _maxSongs;
        private int _maxPages;

        /// <summary>
        /// Index of the feed defined by <see cref="BeastSaberFeed"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a value that is not a valid <see cref="BeastSaberFeed"/></exception>
        public int FeedIndex
        {
            get { return _feedIndex; }
            set
            {
                if (!Enum.IsDefined(typeof(BeastSaberFeed), value))
                    throw new ArgumentOutOfRangeException($"Failed to set FeedIndex: No BeastSaberFeed defined for an index of {value}.");
                _feedIndex = value;
            }
        }

        public BeastSaberFeed Feed
        {
            get { return (BeastSaberFeed)FeedIndex; }
            set
            {
                FeedIndex = (int)value;
            }
        }

        public int SongsPerPage { get { return FeedIndex == 0 ? BeastSaberReader.SongsPerXmlPage : BeastSaberReader.SongsPerJsonPage; } }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="feedIndex"></param>
        /// <param name="maxPages"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="feedIndex"/> is not a valid <see cref="BeastSaberFeed"/></exception>
        public BeastSaberFeedSettings(int feedIndex)
        {
            if (!Enum.IsDefined(typeof(BeastSaberFeed), feedIndex))
                throw new ArgumentOutOfRangeException(nameof(feedIndex), $"No BeastSaberFeed defined for an index of {feedIndex}.");
            FeedIndex = feedIndex;
            MaxPages = 0;
            StartingPage = 1;
        }

        public BeastSaberFeedSettings(BeastSaberFeed feed)
        {
            Feed = feed;
            MaxPages = 0;
            StartingPage = 1;
        }
    }

    public enum BeastSaberFeed
    {
        Following = 0,
        Bookmarks = 1,
        CuratorRecommended = 2
    }
}
