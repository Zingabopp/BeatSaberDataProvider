using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.Util;

namespace BeatSaberDataProvider.DataModels
{
    [Table("songs")]
    public class Song :
        DatabaseDataType,
        IEquatable<Song>,
        IEquatable<ScoreSaberDifficulty>,
        IEquatable<BeatSaverSong>
    {
        
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { SongId }; } }



        #region Main
        [NotMapped]
        private string _songId;
        [Key]
        public string SongId { get { return _songId; } set { _songId = value.ToLower(); } }
        [NotMapped]
        private string _key;
        public string Key { get { return _key; } set { _key = value.ToLower(); } }
        public string Name { get; set; }
        [Updatable]
        public string Description { get; set; }
        [Updatable]
        public DateTime? DeletedAt { get; set; }
        [Key]
        public string Hash { get; set; }
        [Updatable]
        public DateTime Uploaded { get; set; }
        [Updatable]
        public string DownloadUrl { get; set; }
        [Updatable]
        public string CoverUrl { get; set; }
        [Updatable]
        public DateTime ScrapedAt { get; set; }
        #endregion
        #region Metadata
        public ICollection<SongDifficulty> Difficulties { get; set; }
        public ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public string SongAuthorName { get; set; }
        public string LevelAuthorName { get; set; }
        public float BeatsPerMinute { get; set; }
        #endregion
        #region Stats
        [Updatable]
        public int Downloads { get; set; }
        [Updatable]
        public int Plays { get; set; }
        [Updatable]
        public int DownVotes { get; set; }
        [Updatable]
        public int UpVotes { get; set; }
        [Updatable]
        public double Heat { get; set; }
        [Updatable]
        public double Rating { get; set; }
        #endregion


        [ForeignKey("SongHash")]
        public ICollection<ScoreSaberDifficulty> ScoreSaberDifficulties { get; set; }
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
            DeletedAt = s.deletedAt;
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
                ScoreSaberDifficulties = null; // new List<ScoreSaberDifficulty>();
        }

        /// <summary>
        /// Creates a new Song from a Beat Saver song JSON token.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the passed JToken doesn't contain the nested metadata, stats, or uploader objects.</exception>
        /// <param name="jSong"></param>
        public Song(JToken jSong)
        {
            JToken jMetadata = jSong["metadata"];
            JToken jStats = jSong["stats"];
            JToken jUploader = jSong["uploader"];
            if (jMetadata == null || jStats == null || jUploader == null)
            {

                var nullTokens = new string[]{
                    jMetadata == null ? "metadata" : string.Empty,
                    jStats == null ? "stats" : string.Empty,
                    jUploader == null ? "uploader" : string.Empty};
                throw new ArgumentException($"Unable to parse Song from jSong: {(string.Join(", ", nullTokens.Where(s => !string.IsNullOrEmpty(s))))} cannot be null.");
            }


            // Root
            SongId = jSong["_id"]?.Value<string>();
            Key = jSong["key"]?.Value<string>();
            Name = jSong["name"]?.Value<string>();
            Description = jSong["description"]?.Value<string>();
            DeletedAt = jSong["deletedAt"]?.Value<DateTime?>();
            Hash = jSong["hash"]?.Value<string>().ToUpper();
            Uploaded = jSong["uploaded"]?.Value<DateTime?>() ?? DateTime.MinValue;
            DownloadUrl = jSong["downloadURL"]?.Value<string>();
            CoverUrl = jSong["coverURL"]?.Value<string>();

            // Metadata
            var characteristics = JsonConvert.DeserializeObject<List<string>>(jMetadata["characteristics"]?.ToString());
            var diffs = JsonConvert.DeserializeObject<Dictionary<string, bool>>(jMetadata["difficulties"]?.ToString());
            Difficulties = Difficulty.DictionaryToDifficulties(diffs)
                    .Select(d => new SongDifficulty() { Difficulty = d, Song = this, SongId = SongId }).ToList();

            BeatmapCharacteristics = Characteristic.ConvertCharacteristics(characteristics).Select(c =>
                            new BeatmapCharacteristic() { SongId = SongId, Song = this, Characteristic = c }).ToList();

            SongName = jMetadata["songName"]?.Value<string>();
            SongSubName = jMetadata["songSubName"]?.Value<string>();
            SongAuthorName = jMetadata["songAuthorName"]?.Value<string>();
            LevelAuthorName = jMetadata["levelAuthorName"]?.Value<string>();
            BeatsPerMinute = jMetadata["bpm"]?.Value<float>() ?? default;
            //Stats
            Downloads = jStats["downloads"]?.Value<int>() ?? 0;
            Plays = jStats["plays"]?.Value<int>() ?? 0;
            DownVotes = jStats["downVotes"]?.Value<int>() ?? 0;
            UpVotes = jStats["upVotes"]?.Value<int>() ?? 0;
            Heat = jStats["heat"]?.Value<double>() ?? 0;
            Rating = jStats["rating"]?.Value<double>() ?? 0;
            // Uploader
            UploaderRefId = jUploader["_id"]?.Value<string>().ToLower();
            Uploader = new Uploader() { UploaderId = UploaderRefId, UploaderName = jUploader["username"]?.Value<string>() };

            ScrapedAt = jSong["ScrapedAt"]?.Value<DateTime>() ?? DateTime.MinValue;

            //ScoreSaberDifficulties = s.ScoreSaberInfo.Values.Select(d => new ScoreSaberDifficulty(d)).ToList(),
        }

        public static Song CreateFromBeatSaverSong(BeatSaverSong s, IEnumerable<ScoreSaberDifficulty> scoreSaberDifficulties = null)
        {
            Song song = new Song(s, scoreSaberDifficulties);
            return song;
        }

        [Obsolete("Not sure this works")]
        public static Song CreateFromJson(JObject token)
        {
            return CreateFromJson((JToken)token);
        }

        public static Song CreateFromJson(JToken token)
        {
            return new Song(token);
        }


    }

    [Table("characteristics")]
    public class Characteristic : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { CharacteristicId }; } }
        [NotMapped]
        public static Dictionary<string, Characteristic> AvailableCharacteristics = new Dictionary<string, Characteristic>();
        static Characteristic()
        {
            AvailableCharacteristics = new Dictionary<string, Characteristic>
            {
                { "Standard", new Characteristic() { CharacteristicId = 1, CharacteristicName = "Standard" } },
                { "NoArrows", new Characteristic() { CharacteristicId = 2, CharacteristicName = "NoArrows" } },
                { "OneSaber", new Characteristic() { CharacteristicId = 3, CharacteristicName = "OneSaber" } },
                { "Lightshow", new Characteristic() { CharacteristicId = 4, CharacteristicName = "Lightshow" } }
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
            return $"{CharacteristicId}, {CharacteristicName}";
        }

        [Key]
        public int? CharacteristicId { get; set; }
        [Key]
        public string CharacteristicName { get; set; }
        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }


    }

    [Table("BeatmapCharacteristics")]
    public class BeatmapCharacteristic : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { CharacteristicId, SongId }; } }

        public string SongId { get; set; }
        public Song Song { get; set; }

        public int? CharacteristicId { get; set; }
        public Characteristic Characteristic { get; set; }



        public override string ToString()
        {
            return $"{CharacteristicId}: {Characteristic.CharacteristicName}, {SongId}";
        }
    }

    [Table("songdifficulties")]
    public class SongDifficulty : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { DifficultyId, SongId }; } }

        public int? DifficultyId { get; set; }
        public Difficulty Difficulty { get; set; }

        public string SongId { get; set; }
        public Song Song { get; set; }

        public override string ToString()
        {
            return $"{DifficultyId}: {Difficulty?.DifficultyName}, {SongId}";
        }
    }

    [Table("difficulties")]
    public class Difficulty : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { DifficultyId }; } }

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
                { 1, new Difficulty() { DifficultyId = 1, DifficultyName = "Easy" } },
                { 2, new Difficulty() { DifficultyId = 2, DifficultyName = "Normal" } },
                { 3, new Difficulty() { DifficultyId = 3, DifficultyName = "Hard" } },
                { 4, new Difficulty() { DifficultyId = 4, DifficultyName = "Expert" } },
                { 5, new Difficulty() { DifficultyId = 5, DifficultyName = "ExpertPlus" } }
            };
        }
        [Key]
        public int? DifficultyId { get; set; }
        [Key]
        public string DifficultyName { get; set; }
        public ICollection<SongDifficulty> SongDifficulties { get; set; }


        public static ICollection<Difficulty> DictionaryToDifficulties(Dictionary<string, bool> diffs)
        {
            List<Difficulty> difficulties = new List<Difficulty>();
            for (int i = 0; i < diffs.Count; i++)
            {
                if (diffs.Values.ElementAt(i))
                {
                    // 
                    if (!AvailableDifficulties.ContainsKey(i))
                        AvailableDifficulties.Add(i, new Difficulty() { DifficultyId = i, DifficultyName = diffs.Keys.ElementAt(i) });
                    difficulties.Add(AvailableDifficulties[i]);
                }
            }
            return difficulties;
        }

        public override string ToString()
        {
            return $"{DifficultyId}: {DifficultyName}";
        }
    }

    [Table("uploaders")]
    public class Uploader : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { UploaderId }; } }

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
