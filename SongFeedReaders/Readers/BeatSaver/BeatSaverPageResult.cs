using System;
using System.Collections.Generic;
using SongFeedReaders.Data;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class BeatSaverPageResult : PageReadResult
    {
        public int Page { get; }
        /// <summary>
        /// Last Beat Saver page, starting from page 1.
        /// </summary>
        public int LastPage { get; }
        public BeatSaverPageResult(Uri uri, List<ScrapedSong> songs, int page, int lastPage)
            : base(uri, songs, page > lastPage)
        {
            Page = page;
            LastPage = lastPage;
        }
    }
}
