using Newtonsoft.Json;
using SongFeedReaders.Data;
using SongFeedReaders.Readers.ScoreSaber.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.ScoreSaber
{
    public class ScoreSaberSong : IScrapedSong
    {
        protected static readonly string ScoreSaberUrl = "https://scoresaber.com";
        private string? _hash;
        private string? _diffString;
        private string? _image;

        [JsonProperty("uid")]
        public long Uid { get; set; }

        [JsonProperty("id")]
        public string? Hash
        { 
            get => _hash;
            set
            {
                value = value?.ToUpper();
                if (value == _hash)
                    return;
                _hash = value;
            } 
        }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("songSubName")]
        public string? SongSubName { get; set; }

        [JsonProperty("songAuthorName")]
        public string? SongAuthorName { get; set; }

        [JsonProperty("levelAuthorName")]
        public string? LevelAuthorName { get; set; }

        [JsonProperty("bpm")]
        public long Bpm { get; set; }

        [JsonProperty("diff")]
        public string? DifficultyString 
        {
            get => _diffString;
            set
            {
                if (value == _diffString)
                    return;
                _diffString = value;
                if(_diffString != null)
                {

                }
            }
        }

        [JsonIgnore]
        public SongDifficulty Difficulty { get; protected set; }
        [JsonIgnore]
        public SongCharacteristics Characteristics { get; protected set; }

        [JsonProperty("scores")]
        [JsonConverter(typeof(ScoresConverter))]
        public int Scores { get; set; }

        [JsonProperty("scores_day")]
        public long ScoresDay { get; set; }

        [JsonProperty("ranked")]
        public long Ranked { get; set; }

        [JsonProperty("stars")]
        public double Stars { get; set; }

        [JsonProperty("image")]
        public string? Image 
        {
            get => _image;
            set
            {
                if(value != null)
                {
                    if (value.StartsWith("/imports", StringComparison.OrdinalIgnoreCase))
                        value = ScoreSaberUrl + value;

                    if (value.Equals(_image, StringComparison.OrdinalIgnoreCase))
                        return;
                }
                _image = value;
            }
        }

        protected static SongDifficulty GetSongDifficulty(string[]? diffAry)
        {
            if (diffAry == null) return SongDifficulty.Unknown;
            for(int i = 0; i < diffAry.Length; i++)
            {
                for(int j = 0; j < SongCharPairs.Length; j++)
                {
                    if (SongCharPairs[j].Key.Equals(diffAry[i], StringComparison.OrdinalIgnoreCase))
                        return SongCharPairs[j].Value;
                }
            }
            return SongDifficulty.Unknown;
        }

        protected static SongCharacteristics GetSongCharacteristics(string[]? diffAry)
        {
            SongCharacteristics c = SongCharacteristics.None;
            if (diffAry == null) return c;
            for (int i = 0; i < diffAry.Length; i++)
            {
                for (int j = 0; j < SongCharPairs.Length; j++)
                {
                    string thing;
                    thing.
                    if (diffAry[i].Contains(SongCharPairs[j].Key, StringComparison.OrdinalIgnoreCase))
                        return SongCharPairs[j].Value;
                }
            }
            return c;
        }

        protected static KeyValuePair<string, SongDifficulty>[] SongCharPairs = new KeyValuePair<string, SongDifficulty>[]
        {
            new KeyValuePair<string, SongDifficulty>("Easy", SongDifficulty.Easy ),
            new KeyValuePair<string, SongDifficulty>("Normal", SongDifficulty.Normal),
            new KeyValuePair<string, SongDifficulty>("Hard", SongDifficulty.Hard),
            new KeyValuePair<string, SongDifficulty>("Expert", SongDifficulty.Expert),
            new KeyValuePair<string, SongDifficulty>("ExpertPlus", SongDifficulty.ExpertPlus)
        };
        protected static KeyValuePair<string, SongCharacteristics>[] SongCharacteristicPairs = new KeyValuePair<string, SongCharacteristics>[]
        {
            new KeyValuePair<string, SongCharacteristics>("Standard", SongCharacteristics.Standard ),
            new KeyValuePair<string, SongCharacteristics>("OneSaber", SongCharacteristics.OneSaber),
            new KeyValuePair<string, SongCharacteristics>("NoArrows", SongCharacteristics.NoArrows),
            new KeyValuePair<string, SongCharacteristics>("Lightshow", SongCharacteristics.Lightshow),
            new KeyValuePair<string, SongCharacteristics>("Lawless", SongCharacteristics.Lawless),
            new KeyValuePair<string, SongCharacteristics>("360Degree", SongCharacteristics.ThreeSixtyDegree),
            new KeyValuePair<string, SongCharacteristics>("90Degree", SongCharacteristics.NinetyDegree)
        };
    }
}
