using System;

namespace SongFeedReaders.Readers.ScoreSaber
{
    public class ScoreSaberFeedSettings : IFeedSettings
    {

        #region Property Fields
        private int _feedIndex;
        private int _startingPage;
        private int _maxSongs;
        private int _maxPages;
        private int _songsPerPage;
        #endregion

        #region IFeedSettings
        /// <summary>
        /// Name of the chosen feed.
        /// </summary>
        public string FeedName { get { return ScoreSaberFeed.Feeds[Feed].Name; } }

        /// <summary>
        /// Index of the feed defined by <see cref="ScoreSaberFeedName"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a value that is not a valid <see cref="ScoreSaberFeedName"/></exception>
        public int FeedIndex
        {
            get { return _feedIndex; }
            set
            {
                if (!Enum.IsDefined(typeof(ScoreSaberFeedName), value))
                    throw new ArgumentOutOfRangeException($"Failed to set FeedIndex: No ScoreSaberFeed defined for an index of {value}.");
                _feedIndex = value;
            }
        }

        /// <summary>
        /// Number of songs shown on a page. 100 is default.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> when set to less than 1.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 1.</exception>
        public int SongsPerPage
        {
            get { return _songsPerPage; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(SongsPerPage), "SongsPerPage cannot be less than 1.");
                _songsPerPage = value;
            }
        }

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
            return new ScoreSaberFeedSettings(Feed)
            {
                MaxPages = MaxPages,
                MaxSongs = MaxSongs,
                StartingPage = StartingPage,
                SongsPerPage = SongsPerPage,
                SearchQuery = SearchQuery,
                RankedOnly = RankedOnly,
                Filter = (Func<ScrapedSong, bool>)Filter?.Clone(),
                StopWhenAny = (Func<ScrapedSong, bool>)Filter?.Clone(),
            };
        }
        #endregion
        public ScoreSaberFeedName Feed
        {
            get { return (ScoreSaberFeedName)FeedIndex; }
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
        /// String to search ScoreSaber with (only used for the Search feed).
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        /// Only get ranked songs. Forced true for <see cref="ScoreSaberFeedName.TopRanked"/> and <see cref="ScoreSaberFeedName.LatestRanked"/> feeds.
        /// </summary>
        public bool RankedOnly { get; set; }


        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="feedIndex"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="feedIndex"/> is not a valid <see cref="ScoreSaberFeedName"/></exception>
        public ScoreSaberFeedSettings(int feedIndex)
        {
            if (!Enum.IsDefined(typeof(ScoreSaberFeedName), feedIndex))
                throw new ArgumentOutOfRangeException(nameof(feedIndex), $"No ScoreSaberFeed defined for an index of {feedIndex}.");
            FeedIndex = feedIndex;
            SongsPerPage = 100;
            StartingPage = 1;
        }

        public ScoreSaberFeedSettings(ScoreSaberFeedName feed)
        {
            Feed = feed;
            SongsPerPage = 100;
            StartingPage = 1;
        }
        #endregion
    }

    public enum ScoreSaberFeedName
    {
        Trending = 0,
        LatestRanked = 1,
        TopPlayed = 2,
        TopRanked = 3,
        Search = 99
    }
}
