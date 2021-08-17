using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.BeatSaver
{
    public class SearchQueryBuilder
    {
        private static readonly string CRITERIA_KEY = "{CRITERIA}";
        private static readonly string SEARCHTYPEKEY = BeatSaverFeed.SEARCHTYPEKEY; // text or advanced
        private static readonly string SEARCHQUERYKEY = BeatSaverFeed.PARAMETERSKEY;
        private static readonly string PAGEKEY = BeatSaverFeed.PAGEKEY;
        private static readonly string BaseUrl = BeatSaverFeed.Feeds[BeatSaverFeedName.Search].BaseUrl;

        public static Dictionary<BeatSaverSearchType, string> SearchBases = new Dictionary<BeatSaverSearchType, string>()
        {
            { BeatSaverSearchType.author, $"(uploader.username:{CRITERIA_KEY} metadata.levelAuthorName:{CRITERIA_KEY})"},
            { BeatSaverSearchType.name, $"metadata.songName:{CRITERIA_KEY}"},
            { BeatSaverSearchType.user, $"uploader.username:{CRITERIA_KEY}"},
            { BeatSaverSearchType.hash, $"hash:{CRITERIA_KEY}"},
            { BeatSaverSearchType.song, $"(name:{CRITERIA_KEY} metadata.songName:{CRITERIA_KEY} metadata.songSubName:{CRITERIA_KEY} metadata.songAuthorName:{CRITERIA_KEY}"},
            { BeatSaverSearchType.key, $"key:{CRITERIA_KEY}"},
            { BeatSaverSearchType.custom, CRITERIA_KEY },
            { BeatSaverSearchType.all, $"{CRITERIA_KEY}"}
        };

        private string SearchBase => SearchBases[SearchType];
        public string Criteria { get; set; }

        public BeatSaverSearchType SearchType { get; set; }


        public string GetQueryString()
        {
            return SearchBase.Replace(CRITERIA_KEY, Criteria);
        }

        public string GetBaseUrl()
        {
            string url = BaseUrl;
            if (SearchType == BeatSaverSearchType.all)
                return url.Replace(SEARCHTYPEKEY, "text").Replace(CRITERIA_KEY, Criteria);

            url = url.Replace(SEARCHTYPEKEY, "advanced");
            url = url.Replace(SEARCHQUERYKEY, GetQueryString());
            return url;
        }

        public BeatSaverSearchQuery GetQuery()
        {
            return new BeatSaverSearchQuery(GetBaseUrl(), GetQueryString(), Criteria, SearchType);
        }

        public SearchQueryBuilder(BeatSaverSearchType searchType, string criteria)
        {
            SearchType = searchType;
            Criteria = criteria;
        }


    }
}
