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
    //[Table("difficulties")]
    //public class Difficulty : DatabaseDataType
    //{
    //    [NotMapped]
    //    public override object[] PrimaryKey { get { return new object[] { DifficultyName }; } }

    //    /// <summary>
    //    /// Use a dictionary of created Difficulties so it doesn't keep creating the same ones.
    //    /// </summary>
    //    [NotMapped]
    //    public static Dictionary<int, Difficulty> AvailableDifficulties;
    //    /// <summary>
    //    /// Initialize the standard Difficulties so they have the right ID.
    //    /// </summary>
    //    static Difficulty()
    //    {
    //        AvailableDifficulties = new Dictionary<int, Difficulty>
    //        {
    //            //{ 1, new Difficulty() { DifficultyId = 1, DifficultyName = "Easy" } },
    //            //{ 2, new Difficulty() { DifficultyId = 2, DifficultyName = "Normal" } },
    //            //{ 3, new Difficulty() { DifficultyId = 3, DifficultyName = "Hard" } },
    //            //{ 4, new Difficulty() { DifficultyId = 4, DifficultyName = "Expert" } },
    //            //{ 5, new Difficulty() { DifficultyId = 5, DifficultyName = "ExpertPlus" } }
    //        };
    //    }
    //    [Key]
    //    public int? DifficultyLevel { get; set; }
    //    [NotMapped]
    //    private string _difficultyName;
    //    [Key]
    //    public string DifficultyName
    //    {
    //        get { return _difficultyName; }
    //        set
    //        {
    //            _difficultyName = value;
    //            DifficultyLevel = GetDifficultyLevel(value);
    //        }
    //    }
    //    public virtual ICollection<SongDifficulty> SongDifficulties { get; set; }


    //    public static ICollection<Difficulty> DictionaryToDifficulties(Dictionary<string, bool> diffs)
    //    {
    //        List<Difficulty> difficulties = new List<Difficulty>();
    //        for (int i = 0; i < diffs.Count; i++)
    //        {
    //            if (diffs.Values.ElementAt(i)) // May break with custom difficulties.
    //            {
    //                difficulties.Add(new Difficulty() { DifficultyName = diffs.Keys.ElementAt(i) });
    //                // DifficultyId = i, 
    //                //if (!AvailableDifficulties.ContainsKey(i + 1))
    //                //AvailableDifficulties.Add(i + 1, new Difficulty() { DifficultyName = diffs.Keys.ElementAt(i) });
    //                //difficulties.Add(AvailableDifficulties[i + 1]);
    //                //difficulties.Add(new Difficulty() { DifficultyName = diffs.ElementAt(i).Key });
    //            }
    //        }
    //        return difficulties;
    //    }

    //    public static int GetDifficultyLevel(string diffString)
    //    {
    //        switch (diffString.ToLower())
    //        {
    //            case "easy":
    //                return 1;
    //            case "normal":
    //                return 2;
    //            case "hard":
    //                return 3;
    //            case "expert":
    //                return 4;
    //            case "expertplus":
    //                return 5;
    //        }
    //        return 0;
    //    }

    //    public override string ToString()
    //    {
    //        return $"{DifficultyLevel}: {DifficultyName}";
    //    }
    //}

}
