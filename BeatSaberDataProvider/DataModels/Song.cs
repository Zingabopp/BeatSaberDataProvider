using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.Util;

namespace BeatSaberDataProvider.DataModels
{
    [Table("songs")]
    public class Song : IEquatable<Song>, IEquatable<ScoreSaberDifficulty>, IEquatable<BeatSaverSong>
    {
        #region Main
        [NotMapped]
        private string _songId;
        [Key]
        public string SongId { get { return _songId; } set { _songId = value.ToLower(); } }
        [NotMapped]
        private string _key;
        public string Key { get { return _key; } set { _key = value.ToLower(); } }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DeletedAt { get; set; }
        [Key]
        public string Hash { get; set; }
        public DateTime Uploaded { get; set; }
        public string DownloadUrl { get; set; }
        public string CoverUrl { get; set; }
        public DateTime ScrapedAt { get; set; }
        #endregion
        #region Metadata
        public virtual ICollection<SongDifficulty> Difficulties { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public string SongAuthorName { get; set; }
        public string LevelAuthorName { get; set; }
        public double BeatsPerMinute { get; set; }
        #endregion
        #region Stats
        public int Downloads { get; set; }
        public int Plays { get; set; }
        public int DownVotes { get; set; }
        public int UpVotes { get; set; }
        public double Heat { get; set; }
        public double Rating { get; set; }
        #endregion



        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }

        [ForeignKey("SongHash")]
        public virtual ICollection<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }
        public string UploaderRefId { get; set; }
        public Uploader Uploader { get; set; }


        public bool Equals(Song other)
        {
            return SongId.ToLower() == other.SongId.ToLower();
        }

        public bool Equals(ScoreSaberDifficulty other)
        {
            return Hash == other.SongHash;
        }

        public bool Equals(BeatSaverSong other)
        {
            return SongId.ToLower() == other._id;
        }

        public Song()
        {
            //Metadata = new JsonMetaData() { Song = this };
            //Stats = new JsonStats() { Song = this };
            //ScoreSaberDifficulties = new List<ScoreSaberDifficulty>();
        }

        public Song(BeatSaverSong s, IEnumerable<ScoreSaberDifficulty> scoreSaberDifficulties = null)
            : this()
        {
            SongId = s._id;
            Key = s.key.ToLower();
            Name = s.name;
            Description = s.description;
            Hash = s.hash.ToUpper();
            Uploaded = s.uploaded;
            DownloadUrl = s.downloadURL;
            CoverUrl = s.coverURL;

            SongName = s.metadata.songName;
            SongSubName = s.metadata.songSubName;
            SongAuthorName = s.metadata.songAuthorName;
            LevelAuthorName = s.metadata.levelAuthorName;
            BeatsPerMinute = s.metadata.bpm;

            Downloads = s.stats.downloads;
            Plays = s.stats.plays;
            DownVotes = s.stats.downVotes;
            UpVotes = s.stats.upVotes;
            Heat = s.stats.heat;
            Rating = s.stats.rating;

            ScrapedAt = s.ScrapedAt;

            Difficulties = Difficulty.DictionaryToDifficulties(s.metadata.difficulties).
                Select(d => new SongDifficulty() { Difficulty = d, Song = this, SongId = s._id }).ToList();
            BeatmapCharacteristics = Characteristic.ConvertCharacteristics(s.metadata.characteristics).
                Select(c => new BeatmapCharacteristic() { SongId = s._id, Song = this, Characteristic = c }).ToList();
            UploaderRefId = s.uploader.id;
            Uploader = new Uploader() { UploaderId = UploaderRefId, UploaderName = s.uploader.username };
            if (scoreSaberDifficulties != null)
            {
                ScoreSaberDifficulties = scoreSaberDifficulties.ToList();
            }
            else
                ScoreSaberDifficulties = new List<ScoreSaberDifficulty>();
            //
        }

        public static Song CreateFromJson(JToken token)
        {

            var test = JsonConvert.DeserializeObject<List<string>>(token["metadata"]?["characteristics"]?.ToString());
            var uploaderId = token["uploader"]?["_id"]?.Value<string>().ToLower();
            var songId = token["_id"]?.Value<string>();
            Song song = null;
            song = new Song()
            {
                // Root
                SongId = token["_id"]?.Value<string>(),
                Key = token["key"]?.Value<string>(),
                Name = token["name"]?.Value<string>(),
                Description = token["description"]?.Value<string>(),
                Hash = token["hash"]?.Value<string>().ToUpper(),
                Uploaded = token["uploaded"]?.Value<DateTime>() ?? DateTime.MinValue,
                DownloadUrl = token["downloadURL"]?.Value<string>(),
                CoverUrl = token["coverURL"]?.Value<string>(),

                // Metadata
                //Difficulties = Difficulty.DictionaryToDifficulties(token["metadata"]?["difficulties"]?.ToDictionary(t => t.).Select(d =>
                //new SongDifficulty() { Difficulty = d, Song = song, SongId = s.BeatSaverInfo._id }).ToList(),

                
                BeatmapCharacteristics = Characteristic.ConvertCharacteristics(test).Select(c =>
                    new BeatmapCharacteristic() { SongId = songId, Song = song, Characteristic = c }).ToList(), // Check if Song = song works

                SongName = token["metadata"]?["songName"]?.Value<string>(),
                SongSubName = token["metadata"]?["songSubName"]?.Value<string>(),
                SongAuthorName = token["metadata"]?["songAuthorName"]?.Value<string>(),
                LevelAuthorName = token["metadata"]?["levelAuthorName"]?.Value<string>(),
                BeatsPerMinute = token["metadata"]?["bpm"]?.Value<double>() ?? 0d,
                //Stats
                Downloads = token["stats"]?["downloads"]?.Value<int>() ?? 0,
                Plays = token["stats"]?["plays"]?.Value<int>() ?? 0,
                DownVotes = token["stats"]?["downVotes"]?.Value<int>() ?? 0,
                UpVotes = token["stats"]?["upVotes"]?.Value<int>() ?? 0,
                Heat = token["stats"]?["heat"]?.Value<double>() ?? 0,
                Rating = token["stats"]?["rating"]?.Value<double>() ?? 0,
                // Uploader
                UploaderRefId = token["uploader"]?["_id"]?.Value<string>().ToLower(),
                Uploader = new Uploader() { UploaderId = uploaderId, UploaderName = token["uploader"]?["username"]?.Value<string>() },

                ScrapedAt = token[""]?.Value<DateTime>() ?? DateTime.MinValue,

                //ScoreSaberDifficulties = s.ScoreSaberInfo.Values.Select(d => new ScoreSaberDifficulty(d)).ToList(),
            };
            
            return song;
        }
    }

    [Table("characteristics")]
    public class Characteristic
    {
        [NotMapped]
        public static Dictionary<string, Characteristic> AvailableCharacteristics = new Dictionary<string, Characteristic>();
        static Characteristic()
        {
            AvailableCharacteristics = new Dictionary<string, Characteristic>
            {
                { "Standard", new Characteristic() { CharacteristicId = 0, CharacteristicName = "Standard" } },
                { "NoArrows", new Characteristic() { CharacteristicId = 1, CharacteristicName = "NoArrows" } },
                { "OneSaber", new Characteristic() { CharacteristicId = 2, CharacteristicName = "OneSaber" } },
                { "Lightshow", new Characteristic() { CharacteristicId = 3, CharacteristicName = "Lightshow" } }
            };
        }
        public static ICollection<Characteristic> ConvertCharacteristics(ICollection<string> characteristics)
        {
            List<Characteristic> retList = new List<Characteristic>();
            foreach (var c in characteristics)
            {

                if (!AvailableCharacteristics.ContainsKey(c))
                    AvailableCharacteristics.Add(c, new Characteristic() { CharacteristicName = c });
                retList.Add(AvailableCharacteristics[c]);
            }

            return retList;
        }

        public override string ToString()
        {
            return CharacteristicName;
        }

        [Key]
        public int CharacteristicId { get; set; }
        [Key]
        public string CharacteristicName { get; set; }
        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
    }

    [Table("BeatmapCharacteristics")]
    public class BeatmapCharacteristic
    {

        public int CharactersticId { get; set; }
        public Characteristic Characteristic { get; set; }

        public string SongId { get; set; }
        public Song Song { get; set; }

        public override string ToString()
        {
            return Characteristic.CharacteristicName;
        }
    }

    [Table("songdifficulties")]
    public class SongDifficulty
    {
        public int DifficultyId { get; set; }
        public Difficulty Difficulty { get; set; }

        public string SongId { get; set; }
        public Song Song { get; set; }

        public override string ToString()
        {
            return Difficulty?.DifficultyName;
        }
    }

    [Table("difficulties")]
    public class Difficulty
    {
        /// <summary>
        /// Use a dictionary of created Difficulties so it doesn't keep creating the same ones.
        /// </summary>
        [NotMapped]
        public static Dictionary<int, Difficulty> AvailableDifficulties;
        /// <summary>
        /// Initialize the standard Difficulties so they have the right ID.
        /// </summary>
        static Difficulty()
        {
            AvailableDifficulties = new Dictionary<int, Difficulty>
            {
                { 0, new Difficulty() { DifficultyId = 0, DifficultyName = "Easy" } },
                { 1, new Difficulty() { DifficultyId = 1, DifficultyName = "Normal" } },
                { 2, new Difficulty() { DifficultyId = 2, DifficultyName = "Hard" } },
                { 3, new Difficulty() { DifficultyId = 3, DifficultyName = "Expert" } },
                { 4, new Difficulty() { DifficultyId = 4, DifficultyName = "ExpertPlus" } }
            };
        }
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; }
        public virtual ICollection<SongDifficulty> SongDifficulties { get; set; }


        public static ICollection<Difficulty> DictionaryToDifficulties(Dictionary<string, bool> diffs)
        {
            List<Difficulty> difficulties = new List<Difficulty>();
            for (int i = 0; i < diffs.Count; i++)
            {
                if (diffs.Values.ElementAt(i))
                {
                    if (!AvailableDifficulties.ContainsKey(i))
                        AvailableDifficulties.Add(i, new Difficulty() { DifficultyId = i, DifficultyName = diffs.Keys.ElementAt(i) });
                    difficulties.Add(AvailableDifficulties[i]);
                }
            }
            return difficulties;
        }

        public override string ToString()
        {
            return DifficultyName;
        }
    }

    [Table("uploaders")]
    public class Uploader
    {
        [Key]
        [JsonProperty("_id")]
        public string UploaderId { get; set; }
        [Key]
        [JsonProperty("username")]
        public string UploaderName { get; set; }
        [ForeignKey("UploaderRefId")]
        [JsonIgnore]
        public virtual ICollection<Song> Songs { get; set; }

        public override string ToString()
        {
            return UploaderName;
        }
    }

    #region Old Json Stuff

    //[OnDeserialized]
    //private void OnDeserialized(StreamingContext context)
    //{
    //    if (Uploader != null)
    //        UploaderRefId = Uploader.UploaderId;
    //    Metadata.SongId = SongId;
    //    Stats.SongId = SongId;
    //}
    //[NotMapped]
    //[JsonProperty("metadata")]
    //public JsonMetaData Metadata { get; set; }
    //[NotMapped]
    //[JsonProperty("stats")]
    //public JsonStats Stats { get; set; }
    //[Serializable]
    //public class JsonMetaData
    //{
    //    public virtual ICollection<SongDifficulty> Difficulties { get; set; }

    //    [JsonProperty("difficulties")]
    //    private Dictionary<string, bool> JsonDiffs
    //    {
    //        get
    //        {
    //            return Difficulties?.Select(d => d.Difficulty.DifficultyName).ToDictionary(d => d, d => true);
    //        }
    //        set
    //        {
    //            Difficulties = Difficulty.DictionaryToDifficulties(value).
    //                Select(d => new SongDifficulty() { Difficulty = d, Song = Song, SongId = Song.SongId }).ToList();
    //        }
    //    }

    //    [JsonIgnore]
    //    public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }

    //    [NotMapped]
    //    [JsonProperty("characteristics")]
    //    private List<string> Characteristics
    //    {
    //        get { return BeatmapCharacteristics?.Select(c => c.Characteristic.CharacteristicName).ToList(); }
    //        set
    //        {
    //            Characteristic.ConvertCharacteristics(value).
    //            Select(c => new BeatmapCharacteristic() { SongId = Song.SongId, Song = Song, Characteristic = c }).ToList();
    //        }
    //    }

    //    [JsonProperty("songName")]
    //    public string SongName { get; set; }

    //    [JsonProperty("songSubName")]
    //    public string SongSubName { get; set; }

    //    [JsonProperty("songAuthorName")]
    //    public string SongAuthorName { get; set; }

    //    [JsonProperty("levelAuthorName")]
    //    public string LevelAuthorName { get; set; }

    //    [JsonProperty("bpm")]
    //    public float BeatsPerMinute { get; set; }

    //    [JsonIgnore]
    //    public string SongId { get; set; }
    //    [JsonIgnore]
    //    public Song Song { get; set; }
    //}

    //[Serializable]
    //public class JsonStats
    //{
    //    [JsonIgnore]
    //    public string SongId { get; set; }
    //    [JsonIgnore]
    //    public Song Song { get; set; }


    //    [JsonProperty("downloads")]
    //    public int Downloads { get; set; }

    //    [JsonProperty("plays")]
    //    public int Plays { get; set; }

    //    [JsonProperty("downVotes")]
    //    public int DownVotes { get; set; }

    //    [JsonProperty("upVotes")]
    //    public int UpVotes { get; set; }

    //    [JsonProperty("heat")]
    //    public double Heat { get; set; }

    //    [JsonProperty("rating")]
    //    public double Rating { get; set; }
    //}
    #endregion

}
