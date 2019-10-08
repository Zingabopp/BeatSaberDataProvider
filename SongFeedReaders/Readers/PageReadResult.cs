using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public class PageReadResult
    {
        public Uri Uri { get; private set; }
        public List<ScrapedSong> Songs { get; private set; }
        public int Count { get { return Songs?.Count ?? 0; } }
        public Exception Exception { get; private set; }
        public PageReadResult(Uri uri, List<ScrapedSong> songs)
        {
            Uri = uri;
            if (songs == null)
                songs = new List<ScrapedSong>();
            Songs = songs;
        }
        public PageReadResult(Uri uri, List<ScrapedSong> songs, Exception exception)
            : this(uri, songs)
        {
            Exception = exception;
        }
    }
}
