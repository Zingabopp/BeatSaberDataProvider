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
            JToken? latest = song?.JsonData?["versions"]?.First;
            JToken? diffs = latest?["diffs"];
            if (diffs == null)
                return false;
            bool has360 = diffs.Any(t => (t["characteristic"]?.Value<string>() ?? string.Empty) == "360Degree");
            return has360;
        });

        public static Func<ScrapedSong, bool> NinetyDegree => new Func<ScrapedSong, bool>(song =>
        {
            JToken? latest = song?.JsonData?["versions"]?.First;
            JToken? diffs = latest?["diffs"];
            if (diffs == null)
                return false;
            bool has90 = diffs.Any(t => (t["characteristic"]?.Value<string>() ?? string.Empty) == "90Degree");
            return has90;
        });

        public static Func<ScrapedSong, bool> OneSaber => new Func<ScrapedSong, bool>(song =>
        {
            JToken? latest = song?.JsonData?["versions"]?.First;
            JToken? diffs = latest?["diffs"];
            if (diffs == null)
                return false;
            bool hasOneSaber = diffs.Any(t => (t["characteristic"]?.Value<string>() ?? string.Empty) == "OneSaber");
            return hasOneSaber;
        });


    }
}
