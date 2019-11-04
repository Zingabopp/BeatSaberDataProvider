using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class SearchQueryBuilder
    {
        private const string CRITERIA_KEY = "{CRITERIA}";
        public static Dictionary<BeatSaverSearchType, string> SearchBases = new Dictionary<BeatSaverSearchType, string>()
        {
            { BeatSaverSearchType.author, $"(uploader.username:{CRITERIA_KEY} metadata.levelAuthorName:{CRITERIA_KEY})"},
            { BeatSaverSearchType.name, $"metadata.songName:{CRITERIA_KEY}"},
            { BeatSaverSearchType.user, $"uploader.username:{CRITERIA_KEY}"},
            { BeatSaverSearchType.hash, $"hash:{CRITERIA_KEY}"},
            { BeatSaverSearchType.song, $"(name:{CRITERIA_KEY} metadata.songName:{CRITERIA_KEY} metadata.songSubName:{CRITERIA_KEY} metadata.songAuthorName:{CRITERIA_KEY}"},
            { BeatSaverSearchType.key, $"key:{CRITERIA_KEY}"},
            { BeatSaverSearchType.custom, CRITERIA_KEY },
            { BeatSaverSearchType.all, $""}
        };

        private string _searchBase;
        private string _searchCriteria;

        public string GetQueryString()
        {
            return _searchBase.Replace(CRITERIA_KEY, _searchCriteria);
        }

        public SearchQueryBuilder(BeatSaverSearchType searchType, string criteria)
        {
            _searchBase = SearchBases[searchType];
            _searchCriteria = criteria;
        }


    }
}
