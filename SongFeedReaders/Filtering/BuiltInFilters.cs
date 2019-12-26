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
            var meta = song.JsonData["metadata"];
            var chara = meta["characteristics"];
            var list = chara.Children().ToList();
            return chara.Any(t => t["name"].Value<string>() == "360Degree");
        });
        
        public static Func<ScrapedSong, bool> NinetyDegree => new Func<ScrapedSong, bool>(song =>
        {
            var meta = song.JsonData["metadata"];
            var chara = meta["characteristics"];
            var list = chara.Children().ToList();
            return chara.Any(t => t["name"].Value<string>() == "90Degree");
        });

        public static Func<ScrapedSong, bool> OneSaber => new Func<ScrapedSong, bool>(song =>
        {
            var meta = song.JsonData["metadata"];
            var chara = meta["characteristics"];
            var list = chara.Children().ToList();
            return chara.Any(t => t["name"].Value<string>() == "OneSaber");
        });


    }
}
