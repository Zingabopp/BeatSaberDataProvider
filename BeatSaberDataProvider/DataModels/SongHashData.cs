using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberDataProvider.DataModels
{
    [Serializable]
    public class SongHashData
    {
        [JsonProperty("directoryHash")]
        public string directoryHash { get; set; }
        [JsonProperty("songHash")]
        public string songHash { get; set; }
    }
}
