using System;
using System.Collections.Generic;
using System.Text;

namespace SongFeedReaders
{
    public class ScrapedSong
    {
        private string _hash;
        public string Hash
        {
            get { return _hash; }
            set { _hash = value?.ToUpper(); }
        }
        /// <summary>
        /// Full URL to download song.
        /// </summary>
        public Uri DownloadUri { get; set; }
        /// <summary>
        /// What web page this song was scraped from.
        /// </summary>
        public Uri SourceUri { get; set; }
        public string SongName { get; set; }
        public string SongKey { get; set; }
        public string MapperName { get; set; }
        /// <summary>
        /// Data this song was scraped from in JSON form.
        /// </summary>
        public string RawData { get; set; }

        public ScrapedSong() { }
        public ScrapedSong(string hash)
        {
            Hash = hash;
        }

        /// <summary>
        /// Copies the values from another ScrapedSong into this one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the other song is null.</exception>
        public ScrapedSong UpdateFrom(ScrapedSong other, bool changeSourceUri = true)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other), "Other song cannot be null for ScrapedSong.UpdateFrom.");
            Hash = other.Hash;
            DownloadUri = other.DownloadUri;
            if(changeSourceUri)
                SourceUri = other.SourceUri;
            SongName = other.SongName;
            SongKey = other.SongKey;
            MapperName = other.MapperName;
            RawData = other.RawData;
            return this;
        }
    }
}