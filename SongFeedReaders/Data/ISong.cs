using System;
using Newtonsoft.Json.Linq;


namespace SongFeedReaders.Data
{
    public interface ISong
    {
        /// <summary>
        /// Beat Saver hash of the song, always uppercase.
        /// </summary>
        string? Hash { get; set; }
        /// <summary>
        /// Song name.
        /// </summary>
        string? Name { get; set; }
        /// <summary>
        /// Beat Saver song key.
        /// </summary>
        string? Key { get; set; }
        /// <summary>
        /// Username of the mapper.
        /// </summary>
        string? LevelAuthorName { get; set; }
    }
}
