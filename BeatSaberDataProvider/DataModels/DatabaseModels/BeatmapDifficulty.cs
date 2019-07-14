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
    public class BeatmapDifficulty : DatabaseDataType
    {
        #region Properties
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { SongId, CharacteristicName, Difficulty }; } }


        public string CharacteristicName { get; set; }
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
        //public virtual ICollection<CharacteristicDifficulty> CharacteristicDifficulties { get; set; }
        public string SongId { get; set; }
        public virtual Song Song { get; set; }

        //public virtual Characteristic Characteristic { get; set; }
        #endregion

        public BeatmapDifficulty() { }

        public BeatmapDifficulty(JProperty diffToken, string characteristicName, string songId)
        {
            CharacteristicName = characteristicName;
            SongId = songId;
            var diffStats = diffToken.First;
            Difficulty = diffToken.Name;
            Duration = diffStats["duration"]?.Value<double>() ?? 0;
            Length = diffStats["length"]?.Value<int>() ?? 0;
            Bombs = diffStats["bombs"]?.Value<int>() ?? 0;
            Notes = diffStats["notes"]?.Value<int>() ?? 0;
            Obstacles = diffStats["obstacles"]?.Value<int>() ?? 0;
            NoteJumpSpeed = diffStats["njs"]?.Value<float>() ?? 0;
        }

        public BeatmapDifficulty(JProperty diffToken, string characteristicName, Song song)
            : this(diffToken, characteristicName, song.SongId)
        {
            Song = song;
        }

        public BeatmapDifficulty(DifficultyCharacteristic diff, string diffName, string characteristicName, string songId)
        {
            CharacteristicName = characteristicName;
            SongId = songId;
            Difficulty = diffName;
            Duration = diff.duration;
            Length = diff.length;
            Bombs = diff.bombs;
            Notes = diff.notes;
            Obstacles = diff.obstacles;
            NoteJumpSpeed = diff.njs;
        }

        public BeatmapDifficulty(DifficultyCharacteristic diff, string diffName, string characteristicName, Song song)
            : this(diff, diffName, characteristicName, song.SongId)
        {
            Song = song;
        }

        public static string DifficultyToString(int level)
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

        public static int StringToDifficulty(string difficulty)
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


        public override string ToString()
        {
            return $"{CharacteristicName}-{Difficulty}, {SongId}";
        }
    }

}
