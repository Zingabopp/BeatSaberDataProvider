using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberDataProvider.DataModels
{
    [Table("scoresaberdifficulties")]
    public class ScoreSaberDifficulty :
        DatabaseDataType,
        IEquatable<ScoreSaberDifficulty>,
        IEquatable<Song>
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { ScoreSaberDifficultyId }; } }

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
        [Updatable]
        [JsonProperty("scores")]
        [JsonConverter(typeof(IntegerWithCommasConverter))]
        public int Scores { get; set; }
        [Updatable]
        [JsonProperty("scores_day")]
        public int ScoresPerDay { get; set; }
        [Updatable]
        [JsonProperty("ranked")]
        public bool Ranked { get; set; }
        [Updatable]
        [JsonProperty("stars")]
        public float Stars { get; set; }
        [Updatable]
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

        [Updatable]
        [JsonProperty("ScrapedAt")]
        public DateTime ScrapedAt { get; set; }

        public Song Song { get; set; }

        public ScoreSaberDifficulty() { }

        public ScoreSaberDifficulty(JToken token, bool justScraped = false)
        {
            if (token["uid"] == null)
                throw new ArgumentException("Unable to create ScoreSaberDifficulty from JToken, 'uid' field doesn't exist.");
            ScoreSaberDifficultyId = token["uid"].Value<int>();
            SongHash = token["id"]?.Value<string>();
            SongName = token["name"]?.Value<string>();
            SongSubName = token["songSubName"]?.Value<string>();
            SongAuthorName = token["songAuthorName"]?.Value<string>();
            LevelAuthorName = token["levelAuthorName"]?.Value<string>();
            BeatsPerMinute = token["bpm"]?.Value<double>() ?? 0;
            DifficultyName = token["diff"]?.Value<string>();
            Scores = token["scores"]?.Value<int>() ?? 0;
            ScoresPerDay = token["scores_day"]?.Value<int>() ?? 0;
            Ranked = token["ranked"]?.Value<bool>() ?? false;
            string starsStr = token["stars"]?.Value<string>();
            Stars = string.IsNullOrEmpty(starsStr) ? 0f : float.Parse(starsStr);
            Image = token["image"]?.Value<string>();
        }

        private const string EASYKEY = "_easy_solostandard";
        private const string NORMALKEY = "_normal_solostandard";
        private const string HARDKEY = "_hard_solostandard";
        private const string EXPERTKEY = "_expert_solostandard";
        private const string EXPERTPLUSKEY = "_expertplus_solostandard";
        public static string ConvertDiff(string _diffString)
        {
            string diffString = _diffString.ToLower();
            if (!diffString.Contains("solostandard"))
                return _diffString;
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
                    return _diffString;
            }
        }

        public void UpdateFromJson(JToken token)
        {

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

//    public static bool TryParseScoreSaberSong(JToken token, ref ScoreSaberSong song)
//    {
//        string songName = token["name"]?.Value<string>();
//        if (songName == null)
//            songName = "";
//        bool successful = true;
//        try
//        {
//            song = token.ToObject<ScoreSaberSong>(new JsonSerializer() {
//                NullValueHandling = NullValueHandling.Ignore,
//                MissingMemberHandling = MissingMemberHandling.Ignore
//            });
//            //Logger.Debug(song.ToString());
//        }
//        catch (Exception ex)
//        {
//            Logger.Exception($"Unable to create a ScoreSaberSong from the JSON for {songName}\n", ex);
//            successful = false;
//            song = null;
//        }
//        return successful;
//    }

//    public JsonSong GenerateSongInfo()
//    {
//        var newSong = new JsonSong(hash);
//        /*
//        var newSong = new SongInfo() {
//            songName = name,
//            songSubName = songSubName,
//            authorName = levelAuthorName,
//            bpm = bpm
//        };
//        */
//        //newSong.ScoreSaberInfo.Add(uid, this);
//        return newSong;
//    }
