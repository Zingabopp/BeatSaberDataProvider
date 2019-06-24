﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberDataProvider.DataModels
{
    [Table("scoresaberdifficulties")]
    public class ScoreSaberDifficulty : IEquatable<ScoreSaberDifficulty>, IEquatable<Song>
    {
        [Key]
        [JsonProperty("uid")]
        public int ScoreSaberDifficultyId { get; set; }

        [NotMapped]
        [JsonIgnore]
        private string _hash;
        [JsonProperty("id")]
        public string SongHash { get { return _hash; } set { _hash = value.ToUpper(); } }
        [JsonProperty("diff")]
        public string DifficultyName { get { return _diff; } set { _diff = ConvertDiff(value); } }
        [NotMapped]
        [JsonIgnore]
        private string _diff;
        [JsonProperty("scores")]
        [JsonConverter(typeof(IntegerWithCommasConverter))]
        public int Scores { get; set; }
        [JsonProperty("scores_day")]
        public int ScoresPerDay { get; set; }
        [JsonProperty("ranked")]
        public bool Ranked { get; set; }
        [JsonProperty("stars")]
        public float Stars { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }
        [JsonProperty("name")]
        public string SongName { get; set; }
        [JsonProperty("songSubName")]
        public string SongSubName { get; set; }
        [JsonProperty("songAuthorName")]
        public string SongAuthorName { get; set; }
        [JsonProperty("levelAuthorName")]
        public string LevelAuthorName { get; set; }
        [JsonProperty("bpm")]
        public double BeatsPerMinute { get; set; }

        [JsonProperty("ScrapedAt")]
        public DateTime ScrapedAt { get; set; }

        public Song Song { get; set; }

        public ScoreSaberDifficulty() { }

        private const string EASYKEY = "_easy_solostandard";
        private const string NORMALKEY = "_normal_solostandard";
        private const string HARDKEY = "_hard_solostandard";
        private const string EXPERTKEY = "_expert_solostandard";
        private const string EXPERTPLUSKEY = "_expertplus_solostandard";
        public static string ConvertDiff(string diffString)
        {
            diffString = diffString.ToLower();
            if (!diffString.Contains("solostandard"))
                return diffString;
            switch (diffString)
            {
                case EXPERTPLUSKEY:
                    return "ExpertPlus";
                case EXPERTKEY:
                    return "Expert";
                case HARDKEY:
                    return "Hard";
                case NORMALKEY:
                    return "Normal";
                case EASYKEY:
                    return "Easy";
                default:
                    return diffString;
            }
        }

        public bool Equals(ScoreSaberDifficulty other)
        {
            return ScoreSaberDifficultyId == other.ScoreSaberDifficultyId;
        }

        public bool Equals(Song other)
        {
            return SongHash == other.Hash;
        }
    }
}
