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
    [Table("CharacteristicDifficulty")]
    public class CharacteristicDifficulty : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { _cdId }; } }

        [Key]
        public int? _cdId { get; set; }


        public string Difficulty
        {
            get { return DifficultyToString(DifficultyLevel); }
            set { DifficultyLevel = StringToDifficulty(value); }
        }
        public int DifficultyLevel { get; set; }
        public double Duration { get; set; }
        public int Length { get; set; }
        public int Bombs { get; set; }
        public int Notes { get; set; }
        public int Obstacles { get; set; }
        public float NoteJumpSpeed { get; set; }

        public static CharacteristicDifficulty ConvertFromJson(JProperty jDifficulty)
        {
            var diffStats = jDifficulty.First;
            return new CharacteristicDifficulty()
            {
                Difficulty = jDifficulty.Name,
                Duration = diffStats["duration"]?.Value<double>() ?? 0,
                Length = diffStats["length"]?.Value<int>() ?? 0,
                Bombs = diffStats["bombs"]?.Value<int>() ?? 0,
                Notes = diffStats["notes"]?.Value<int>() ?? 0,
                Obstacles = diffStats["obstacles"]?.Value<int>() ?? 0,
                NoteJumpSpeed = diffStats["njs"]?.Value<float>() ?? 0,
            };
        }

        public int? BeatmapCharacteristicId { get; set; }
        public BeatmapCharacteristic BeatmapCharacteristic { get; set; }

        public string DifficultyToString(int level)
        {
            switch (level)
            {
                case 1:
                    return "Easy";
                case 2:
                    return "Normal";
                case 3:
                    return "Hard";
                case 4:
                    return "Expert";
                case 5:
                    return "ExpertPlus";
            }
            return "Other";
        }

        public int StringToDifficulty(string difficulty)
        {
            switch (difficulty.ToLower())
            {
                case "easy":
                    return 1;
                case "normal":
                    return 2;
                case "hard":
                    return 3;
                case "expert":
                    return 4;
                case "expertplus":
                    return 5;
            }
            return -1;
        }

    }

}
