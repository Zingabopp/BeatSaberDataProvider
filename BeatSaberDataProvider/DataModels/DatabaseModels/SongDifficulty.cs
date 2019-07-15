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
    //[Table("songdifficulties")]
    //public class SongDifficulty : DatabaseDataType
    //{
    //    [NotMapped]
    //    public override object[] PrimaryKey { get { return new object[] { DifficultyName, SongId }; } }

    //    public string DifficultyName { get; set; }
    //    public virtual Difficulty Difficulty { get; set; }

    //    public string SongId { get; set; }
    //    public virtual Song Song { get; set; }

    //    public override string ToString()
    //    {
    //        return $"{DifficultyName}: {Difficulty?.DifficultyName}, {SongId}";
    //    }
    //}

}
