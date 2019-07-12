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
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { BeatmapCharacteristicId }; } }

        public int? BeatmapCharacteristicId { get; set; }

        public string SongId { get; set; }
        public virtual Song Song { get; set; }

        public string CharacteristicName { get; set; }
        public virtual Characteristic Characteristic { get; set; }

        public virtual ICollection<CharacteristicDifficulty> CharacteristicDifficulties { get; set; }

        public static ICollection<BeatmapCharacteristic> ConvertFromJson(JToken jChars, string _songId)
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
                    newCharDiff.BeatmapCharacteristicId = newBChar.BeatmapCharacteristicId;
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
