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
    [Table("characteristics")]
    public class Characteristic : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { CharacteristicName }; } }
        [NotMapped]
        public static Dictionary<string, Characteristic> AvailableCharacteristics = new Dictionary<string, Characteristic>();
        static Characteristic()
        {
            AvailableCharacteristics = new Dictionary<string, Characteristic>
            {
                //{ "Standard", new Characteristic() { CharacteristicId = 1, CharacteristicName = "Standard" } },
                //{ "NoArrows", new Characteristic() { CharacteristicId = 2, CharacteristicName = "NoArrows" } },
                //{ "OneSaber", new Characteristic() { CharacteristicId = 3, CharacteristicName = "OneSaber" } },
                //{ "Lightshow", new Characteristic() { CharacteristicId = 4, CharacteristicName = "Lightshow" } }
            };
        }

        public Characteristic() { }

        public Characteristic(JsonBeatmapCharacteristic c)
        {
            CharacteristicName = c.name;
        }

        public static Characteristic GetOrAddCharacteristic(string name)
        {
            var existing = AvailableCharacteristics.Where(c => c.Key.ToLower() == name.ToLower())?.FirstOrDefault().Value;
            if (existing != null)
            {
                return existing;
            }
            else
            {
                var newChar = new Characteristic() { CharacteristicName = name };
                AvailableCharacteristics.Add(name, newChar);
                return newChar;
            }

        }

        public override string ToString()
        {
            return $"{CharacteristicName}";
        }

        //[Key]
        //public int? CharacteristicId { get; set; }
        [Key]
        public string CharacteristicName { get; set; }
        public virtual ICollection<BeatmapCharacteristic> BeatmapCharacteristics { get; set; }
    }

}
