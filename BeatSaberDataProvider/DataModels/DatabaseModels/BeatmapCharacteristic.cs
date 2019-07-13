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
    [Table("BeatmapCharacteristics")]
    public class BeatmapCharacteristic : DatabaseDataType
    {
        #region Properties
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { SongId, CharacteristicName }; } }

        public string SongId { get; set; }
        public virtual Song Song { get; set; }

        public virtual ICollection<CharacteristicDifficulty> CharacteristicDifficulties { get; set; }

        public string CharacteristicName { get; set; }
        public virtual Characteristic Characteristic { get; set; }
        #endregion

        public static ICollection<BeatmapCharacteristic> ConvertFrom(ICollection<JsonBeatmapCharacteristic> chars, Song song)
        {
            var bcList = new List<BeatmapCharacteristic>();
            foreach (var c in chars)
            {
                bcList.Add(new BeatmapCharacteristic()
                {
                    Song = song,
                    Characteristic = new Characteristic(c),
                    CharacteristicDifficulties = c.difficulties.Where(d => d.Value != null).Select(pair => new CharacteristicDifficulty(pair.Value, pair.Key, c.name, song.SongId)).ToList()
                });
            }

            return bcList;
        }

        public static ICollection<BeatmapCharacteristic> ConvertFrom(JToken jChars, string _songId)
        {
            var bcList = new List<BeatmapCharacteristic>();
            foreach (var jChar in jChars.Children())
            {
                var newChar = Characteristic.GetOrAddCharacteristic(jChar["name"]?.Value<string>());
                var charDiffs = new List<CharacteristicDifficulty>();
                var newBChar = new BeatmapCharacteristic() { SongId = _songId, CharacteristicName = newChar.CharacteristicName, Characteristic = newChar };
                foreach (JProperty diff in jChar["difficulties"].Children())
                {
                    if (string.IsNullOrEmpty(diff.First.ToString()))
                        continue;
                    var newCharDiff = CharacteristicDifficulty.ConvertFromJson(diff);
                    //newCharDiff.BeatmapCharacteristicId = newBChar.BeatmapCharacteristicId;
                    newCharDiff.BeatmapCharacteristic = newBChar;
                    charDiffs.Add(newCharDiff);
                }
                newBChar.CharacteristicDifficulties = charDiffs;
                bcList.Add(newBChar);
            }

            return bcList;
        }

        public override string ToString()
        {
            return $"{Characteristic.CharacteristicName}, {SongId}";
        }
    }

}
