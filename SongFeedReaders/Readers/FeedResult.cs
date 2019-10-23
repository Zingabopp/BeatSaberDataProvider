﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    /// <summary>
    /// Wraps the result of a feed. Can contain Exceptions that occurred in the process of getting the results. Songs is never null.
    /// The Exception will either be a FeedReaderException or an AggregateException containing multiple FeedReaderExceptions. FeedReaderExceptions may contain a more specific Exception in InnerExcepion.
    /// </summary>
    public class FeedResult
    {
        public IReadOnlyDictionary<string, ScrapedSong> Songs { get; private set; }
        public int Count { get { return Songs?.Count ?? 0; } }
        public FeedResultErrorLevel ErrorLevel { get; private set; }
        private bool _successful;
        public bool Successful { get { return _successful && Exception == null; } }
        /// <summary>
        /// Exception when something goes wrong in the feed readers. More specific exceptions may be stored in InnerException.
        /// </summary>
        public FeedReaderException Exception { get; private set; }

        public FeedResult(Dictionary<string, ScrapedSong> songs)
        {
            if (songs == null)
            {
                _successful = false;
                songs = new Dictionary<string, ScrapedSong>();
            }
            else
                _successful = true;
            Songs = new ReadOnlyDictionary<string, ScrapedSong>(songs);
        }
        public FeedResult(Dictionary<string, ScrapedSong> songs, Exception exception, FeedResultErrorLevel errorLevel)
            : this(songs)
        {
            ErrorLevel = errorLevel;
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
    public enum FeedResultErrorLevel
    {
        None,
        Warning,
        Error
    }
}
