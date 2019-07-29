using System;
using System.Text;
using System.Text.RegularExpressions;
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
        private static readonly Regex _digitRegex = new Regex("^(?:0[xX])?([0-9a-fA-F]+)$", RegexOptions.Compiled);
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { SongId }; } }
        [NotMapped]
        private int _keyAsInt;
        [NotMapped]
        public int KeyAsInt {
            get
            {
                var match = _digitRegex.Match(Key);
                if(_keyAsInt == 0)
                    _keyAsInt = match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber) : 0;
                return _keyAsInt;
            }
                
        }


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
        public string Converted { get; set; }
        [Updatable]
        public string DownloadUrl { get; set; }
        [Updatable]
        public string CoverUrl { get; set; }
        [Updatable]
        public DateTime ScrapedAt { get; set; }
        #endregion
        #region Metadata
        //public virtual ICollection<SongDifficulty> SongDifficulties { get; set; }
        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
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

        [NotMapped]
        private ICollection<ScoreSaberDifficulty> _scoreSaberDifficulties;
        [ForeignKey("SongHash")]
        public virtual ICollection<ScoreSaberDifficulty> ScoreSaberDifficulties
        {
            get
            {
                if (_scoreSaberDifficulties == null)
                    _scoreSaberDifficulties = new List<ScoreSaberDifficulty>();
                return _scoreSaberDifficulties;
            }
            set { _scoreSaberDifficulties = value; }
        }
        public string UploaderRefId { get; set; }
        [ForeignKey("UploaderRefId")]
        public virtual Uploader Uploader { get; set; }


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
        [Obsolete("Need to fix this for new BeatSaver metadata")]
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
            Converted = s.converted;
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

            //SongDifficulties = Difficulty.DictionaryToDifficulties(s.metadata.difficulties).
            //    Select(d => new SongDifficulty() { Difficulty = d, Song = this, SongId = s._id }).ToList();
            BeatmapCharacteristics = BeatmapCharacteristic.ConvertFrom(s.metadata.characteristics, this);
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
            JToken jCharacteristics = jMetadata["characteristics"];
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
            Converted = jSong["converted"]?.Value<string>();
            DownloadUrl = jSong["downloadURL"]?.Value<string>();
            CoverUrl = jSong["coverURL"]?.Value<string>();

            // Metadata
            //var characteristics = JsonConvert.DeserializeObject<List<string>>(jMetadata["characteristics"]?.ToString());// Change
            var diffs = JsonConvert.DeserializeObject<Dictionary<string, bool>>(jMetadata["difficulties"]?.ToString());
            //SongDifficulties = Difficulty.DictionaryToDifficulties(diffs)
            //        .Select(d => new SongDifficulty() { Difficulty = d, Song = this, SongId = SongId }).ToList();

            BeatmapCharacteristics = BeatmapCharacteristic.ConvertFrom(jCharacteristics, SongId);

            SongName = jMetadata["songName"]?.Value<string>();
            SongSubName = jMetadata["songSubName"]?.Value<string>();
            SongAuthorName = jMetadata["songAuthorName"]?.Value<string>();
            LevelAuthorName = jMetadata["levelAuthorName"]?.Value<string>();
            BeatsPerMinute = jMetadata["bpm"]?.Value<float>() ?? default(float);
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

        public override string ToString()
        {
            return $"{KeyAsInt} {SongName}";
        }


    }

}
