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
        #region Properties
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { Difficulty, SongId, CharacteristicName }; } }

        //[Key]
        //public int? _cdId { get; set; }


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
        public float NoteJumpSpeedOffset { get; set; }

        public string SongId { get; set; }
        public string CharacteristicName { get; set; }
        [NotMapped]
        private BeatmapCharacteristic _beatmapCharacteristic;
        public BeatmapCharacteristic BeatmapCharacteristic
        {
            get { return _beatmapCharacteristic; }
            set
            {
                _beatmapCharacteristic = value;
                SongId = value.SongId;
                CharacteristicName = value.CharacteristicName;
            }
        }
        #endregion

        public CharacteristicDifficulty() { }

        public CharacteristicDifficulty(DifficultyCharacteristic diff, string diffName, BeatmapCharacteristic bmChar)
        {
            Difficulty = diffName;
            Duration = diff.duration;
            Length = diff.length;
            Bombs = diff.bombs;
            Notes = diff.notes;
            Obstacles = diff.obstacles;
            NoteJumpSpeed = diff.njs;
            NoteJumpSpeedOffset = diff.njsOffset;

            BeatmapCharacteristic = bmChar;
        }

        public CharacteristicDifficulty(DifficultyCharacteristic diff, string diffName, string charName, string songId)
        {
            Difficulty = diffName;
            Duration = diff.duration;
            Length = diff.length;
            Bombs = diff.bombs;
            Notes = diff.notes;
            Obstacles = diff.obstacles;
            NoteJumpSpeed = diff.njs;
            NoteJumpSpeedOffset = diff.njsOffset;

            SongId = songId;
            CharacteristicName = charName;
        }

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
                NoteJumpSpeedOffset = diffStats["njsOffset"]?.Value<float>() ?? 0
            };
        }
        public override string ToString()
        {
            return $"{Difficulty} {CharacteristicName}: {SongId}";
        }
    }

}
