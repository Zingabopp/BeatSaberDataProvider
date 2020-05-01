using System;
using Newtonsoft.Json.Linq;


namespace SongFeedReaders.Data
{
    public interface ISong
    {
        /// <summary>
        /// Beat Saver hash of the song, always uppercase.
        /// </summary>
        string Hash { get; set; }
        string? Name { get; set; }
        string? Key { get; set; }
        string? LevelAuthorName { get; set; }
    }
}
