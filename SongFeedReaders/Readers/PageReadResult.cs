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
        public FeedReaderException Exception { get; private set; }
        private bool _successful;
        public bool Successful { get { return _successful && Exception == null; } }
        public PageReadResult(Uri uri, List<ScrapedSong> songs)
        {
            Uri = uri;
            if (songs == null)
            {
                _successful = false;
                songs = new List<ScrapedSong>();
            }
            else
                _successful = true;
            Songs = songs;
        }
        public PageReadResult(Uri uri, List<ScrapedSong> songs, Exception exception)
            : this(uri, songs)
        {
            if (exception != null)
            {
                if (exception is FeedReaderException frException)
                {
                    Exception = frException;
                }
                else
                    Exception = new FeedReaderException(exception.Message, exception);
            }
        }
    }
}
