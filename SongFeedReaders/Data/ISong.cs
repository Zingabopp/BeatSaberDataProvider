using System;
using Newtonsoft.Json.Linq;


namespace SongFeedReaders.Data
{
    public interface ISong
    {
        /// <summary>
        /// Beat Saver hash of the song, always uppercase.
        /// </summary>
        string Hash { get; }
        /// <summary>
        /// Full URL to download song.
        /// </summary>
        Uri DownloadUri { get; }
        /// <summary>
        /// What web page this song was scraped from.
        /// </summary>
        Uri SourceUri { get; }
        string SongName { get; }
        string SongKey { get; }
        string MapperName { get; }
        /// <summary>
        /// Data this song was scraped from in JSON form.
        /// </summary>
        string RawData { get; }

        JObject JsonData { get; }
    }
}
