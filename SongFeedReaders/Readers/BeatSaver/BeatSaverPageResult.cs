using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class BeatSaverPageResult : PageReadResult
    {
        public int LastPage { get; }
        public BeatSaverPageResult(Uri uri, List<ScrapedSong> songs, int page, int lastPage)
            : base(uri, songs, page, page > lastPage)
        {
            LastPage = lastPage;
        }

        public BeatSaverPageResult(Uri uri, List<ScrapedSong> songs, int page, Exception exception, PageErrorType pageError, int lastPage)
            : this(uri, songs, page, lastPage)
        { }
    }
}
