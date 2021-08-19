using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class OldAndruzzScrapedSong : ScrapedSong
    {
        [JsonProperty("key")]
        private string? _key
        {
            get => Key;
            set => Key = value;
        }

        [JsonProperty("hash")]
        private string? _hash
        {
            get => Hash;
            set => Hash = value;
        }



        private AndruzzMeta? _meta;
        [JsonProperty("metadata")]
        private AndruzzMeta? meta
        {
            get => _meta;
            set
            {
                _meta = value;
                Name = _meta?.SongName;
            }
        }

        private AndruzzUploader? _uploader;
        [JsonProperty("uploader")]
        private AndruzzUploader? uploader
        {
            get => _uploader;
            set
            {
                _uploader = value;
                LevelAuthorName = _uploader?.Username;
            }
        }
        
        [JsonObject]
        private class AndruzzUploader
        {
            [JsonProperty("_id")]
            public string? UploaderId { get; set; }

            [JsonProperty("username")]
            public string? Username { get; set; }

        }
        [JsonObject]
        private class AndruzzMeta
        {
            [JsonProperty("songName")]
            public string? SongName { get; set; }
        }
    }


}
