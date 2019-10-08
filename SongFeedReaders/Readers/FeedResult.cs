using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    /// <summary>
    /// Wraps the result of a feed. Can contain Exceptions that occurred in the process of getting the results. Songs is never null.
    /// </summary>
    public class FeedResult
    {
        public IReadOnlyDictionary<string, ScrapedSong> Songs { get; private set; }
        public int Count { get { return Songs?.Count ?? 0; } }
        public Exception Exception { get; private set; }

        public FeedResult(Dictionary<string, ScrapedSong> songs)
        {
            if (songs == null)
                songs = new Dictionary<string, ScrapedSong>();
            Songs = new ReadOnlyDictionary<string, ScrapedSong>(songs);
        }
        public FeedResult(Dictionary<string, ScrapedSong> songs, Exception exception)
            : this(songs)
        {
            Exception = exception;
        }


    }
}
