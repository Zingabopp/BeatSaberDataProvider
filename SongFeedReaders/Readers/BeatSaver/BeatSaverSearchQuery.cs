using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers.BeatSaver
{
    public struct BeatSaverSearchQuery
    {
        public string BaseUrl { get; }
        
        public string Criteria { get; }

        public string Query { get; }

        public BeatSaverSearchType SearchType { get; }

        public BeatSaverSearchQuery(string baseUrl, string query, string criteria, BeatSaverSearchType searchType)
        {
            BaseUrl = baseUrl;
            Criteria = criteria;
            Query = query;
            SearchType = searchType;
        }

        public Uri GetUriForPage(int pageNum)
        {
            return new Uri(BaseUrl.Replace(BeatSaverFeed.PAGEKEY, pageNum.ToString()));
        }
    }
}
