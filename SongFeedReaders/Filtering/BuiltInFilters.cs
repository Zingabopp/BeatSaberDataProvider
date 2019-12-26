using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Filtering
{
    public static class BuiltInFilters
    {
        public static Func<ScrapedSong, bool> ThreeSixtyDegree => new Func<ScrapedSong, bool>(song =>
        {
            var meta = song?.JsonData["metadata"];
            var chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "360Degree");
        });
        
        public static Func<ScrapedSong, bool> NinetyDegree => new Func<ScrapedSong, bool>(song =>
        {
            var meta = song?.JsonData["metadata"];
            var chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "90Degree");
        });

        public static Func<ScrapedSong, bool> OneSaber => new Func<ScrapedSong, bool>(song =>
        {
            var meta = song?.JsonData["metadata"];
            var chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "OneSaber");
        });


    }
}
