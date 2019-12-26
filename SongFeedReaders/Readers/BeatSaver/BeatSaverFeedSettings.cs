using System;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class BeatSaverFeedSettings : IFeedSettings
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
        public string FeedName { get { return BeatSaverFeed.Feeds[Feed].Name; } }

        /// <summary>
        /// Index of the feed defined by <see cref="BeatSaverFeedName"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a value that is not a valid <see cref="BeatSaverFeedName"/></exception>
        public int FeedIndex
        {
            get { return _feedIndex; }
            set
            {
                if (!Enum.IsDefined(typeof(BeatSaverFeedName), value))
                    throw new ArgumentOutOfRangeException($"Failed to set FeedIndex: No BeatSaverFeed defined for an index of {value}.");
                _feedIndex = value;
            }
        }

        /// <summary>
        /// Number of songs per page for this feed.
        /// </summary>
        public int SongsPerPage { get { return BeatSaverReader.SongsPerPage; } }

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
            return new BeatSaverFeedSettings(Feed)
            {
                MaxPages = MaxPages,
                MaxSongs = MaxSongs,
                StartingPage = StartingPage,
                SearchQuery = SearchQuery.GetValueOrDefault(),
                Filter = (Func<ScrapedSong, bool>)Filter?.Clone(),
                StopWhenAny = (Func<ScrapedSong, bool>)StopWhenAny?.Clone(),
            };
        }
        #endregion


        public BeatSaverFeedName Feed
        {
            get { return (BeatSaverFeedName)FeedIndex; }
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

        public BeatSaverSearchQuery? SearchQuery { get; set; }

        /// <summary>
        /// Type of search to perform, only used for Search and Author feeds.
        /// Default is 'song' (song name, song subname, author)
        /// </summary>
        public BeatSaverSearchType? SearchType { get { return SearchQuery?.SearchType; } }

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="feedIndex"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="feedIndex"/> is not a valid <see cref="BeatSaverFeedName"/></exception>
        public BeatSaverFeedSettings(int feedIndex)
        {
            if (!Enum.IsDefined(typeof(BeatSaverFeedName), feedIndex))
                throw new ArgumentOutOfRangeException(nameof(feedIndex), $"No BeatSaverFeed defined for an index of {feedIndex}.");
            FeedIndex = feedIndex;
            MaxPages = 0;
            StartingPage = 1;
        }

        public BeatSaverFeedSettings(BeatSaverFeedName feed)
        {
            Feed = feed;
            MaxPages = 0;
            StartingPage = 1;
        }
        #endregion
    }

    public enum BeatSaverFeedName
    {
        Author = 0,
        Latest = 1,
        Hot = 2,
        Plays = 3,
        Downloads = 4,
        Search = 98,
    }

    // enum names are lowercase because they are used directly in the URL building
    public enum BeatSaverSearchType
    {
        author, // author name (not necessarily uploader), ?q=metadata.levelAuthorName:<CRITERIA>
        name, // song name only, ?q=metadata.songName:<CRITERIA>
        user, // user (uploader) name, ?q=uploader.username:<CRITERIA>
        hash, // -MD5 Hash
        song, // song name, song subname, artist 
        key, // -BeatSaver hex key
        custom, // Custom query (BeatSaverSettings.Criteria is used directly)
        all // Default search: name, uploader.username, song name, songSubName, songAuthorName, metadata.levelAuthorName, hash
    }

    /// BeatSaver advanced search uses (<jsonField>:<query>), where query is a lucene query.
    /// Ex: Maps uploaded on the 1st of January with an easy difficulty and a name that contains the letter 'e'
    /// https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND name:*e*
    /// Rate limit is 15 every 5 seconds that all advanced searches share.
}
