using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SongFeedReaders.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IScrapedSong : ISong
    {
        /// <summary>
        /// What web page this song was scraped from.
        /// </summary>
        Uri? SourceUri { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData { get; set; }

        //public override string ToString()
        //{
        //    string keyStr;
        //    if (!string.IsNullOrEmpty(Key))
        //        keyStr = $"({Key}) ";
        //    else
        //        keyStr = string.Empty;
        //    return $"{keyStr}{Name} by {LevelAuthorName}";
        //}
    }
}