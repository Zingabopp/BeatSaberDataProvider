using Newtonsoft.Json.Linq;
using SongFeedReaders.Data;
using System;
using System.Linq;

namespace SongFeedReaders.Filtering
{
    public static class BuiltInFilters
    {
        public static Func<IScrapedSong, bool> ThreeSixtyDegree => new Func<IScrapedSong, bool>(song =>
        {
            JToken? meta = song?.JsonData?["metadata"];
            JToken? chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "360Degree");
        });

        public static Func<IScrapedSong, bool> NinetyDegree => new Func<IScrapedSong, bool>(song =>
        {
            JToken? meta = song?.JsonData?["metadata"];
            JToken? chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "90Degree");
        });

        public static Func<IScrapedSong, bool> OneSaber => new Func<IScrapedSong, bool>(song =>
        {
            JToken? meta = song?.JsonData?["metadata"];
            JToken? chara = meta?["characteristics"];
            if (chara == null) return false;
            return chara.Any(t => (t["name"]?.Value<string>() ?? string.Empty) == "OneSaber");
        });


    }
}
