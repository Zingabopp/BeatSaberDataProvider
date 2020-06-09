using Newtonsoft.Json.Linq;
using SongFeedReaders.Data;
using System;
using System.Linq;

namespace SongFeedReaders.Filtering
{
    public static class BuiltInFilters
    {
        public static Func<ScrapedSong, bool> ThreeSixtyDegree => new Func<ScrapedSong, bool>(song =>
        {
            JToken? meta = song?.JsonData?["metadata"];
            JToken? chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "360Degree");
        });

        public static Func<ScrapedSong, bool> NinetyDegree => new Func<ScrapedSong, bool>(song =>
        {
            JToken? meta = song?.JsonData?["metadata"];
            JToken? chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "90Degree");
        });

        public static Func<ScrapedSong, bool> OneSaber => new Func<ScrapedSong, bool>(song =>
        {
            JToken? meta = song?.JsonData?["metadata"];
            JToken? chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "OneSaber");
        });


    }
}
