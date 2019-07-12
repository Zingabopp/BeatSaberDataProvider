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
    [Table("uploaders")]
    public class Uploader : DatabaseDataType
    {
        [NotMapped]
        public override object[] PrimaryKey { get { return new object[] { UploaderId }; } }
        [Key]
        public string UploaderId { get; set; }
        [Key]
        public string UploaderName { get; set; }

        public virtual ICollection<Song> Songs { get; set; }

        public override string ToString()
        {
            return UploaderName;
        }
    }

}
