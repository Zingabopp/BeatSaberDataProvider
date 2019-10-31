using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static BeatSaberDataProvider.Web.WebUtils;

namespace BeatSaberDataProvider.Web
{
    public static class OnlineSongSearch
    {
        private const string BEATSAVER_DETAILS_BASE_URL = "https://beatsaver.com/api/maps/detail/";
        private const string BEATSAVER_GETBYHASH_BASE_URL = "https://beatsaver.com/api/maps/by-hash/";

        public static async Task<Song> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {

            Uri uri = new Uri(BEATSAVER_DETAILS_BASE_URL + key);
            string pageText = "";
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrEmpty(pageText))
                {
                    Logger.Warning($"Unable to get web page at {uri}");
                    return null;
                }
            }
            catch (HttpRequestException)
            {
                Logger.Error($"HttpRequestException while trying to populate fields for {key}");
                return null;
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {key}");
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception getting page", ex);
            }
            Song song = ParseSongsFromPage(pageText).FirstOrDefault();
            song.ScrapedAt = DateTime.Now;
            return ScrapedDataProvider.GetOrCreateSong(song);
        }


        public static async Task<Song> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {

            Uri uri = new Uri(BEATSAVER_GETBYHASH_BASE_URL + hash);
            string pageText = "";
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                if (string.IsNullOrEmpty(pageText))
                {
                    Logger.Warning($"Unable to get web page at {uri}");
                    return null;
                }
            }
            catch (HttpRequestException)
            {
                Logger.Error($"HttpRequestException while trying to populate fields for {hash}");
                return null;
            }
            catch (AggregateException ae)
            {
                ae.WriteExceptions($"Exception while trying to get details for {hash}");
            }
            catch (Exception ex)
            {
                Logger.Exception("Exception getting page", ex);
            }
            Song song = ParseSongsFromPage(pageText).FirstOrDefault();
            song.ScrapedAt = DateTime.Now;
            return ScrapedDataProvider.GetOrCreateSong(song);
        }


        public static async Task<List<Song>> GetSongsFromPageAsync(string url, bool useDateLimit = false)
        {
            string pageText = string.Empty;
            List<Song> songs = new List<Song>(); ;
            try
            {
                using (var response = await WebClient.GetAsync(url).ConfigureAwait(false))
                {
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                Logger.Debug($"Successful got pageText from {url}");
                foreach (var song in ParseSongsFromPage(pageText))
                {
                    songs.Add(ScrapedDataProvider.GetOrCreateSong(song));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting page text from {url}");
            }

            return songs;
        }

        public static List<Song> ParseSongsFromPage(string pageText)
        {
            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);

            }
            catch (Exception ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            List<Song> songs = new List<Song>();
            Song newSong;
            int? resultTotal = result["totalDocs"]?.Value<int>();
            if (resultTotal == null) resultTotal = 0;

            // Single song in page text.
            if (resultTotal == 0)
            {
                if (result["key"] != null)
                {
                    newSong = ParseSongFromJson(result);
                    if (newSong != null)
                    {
                        songs.Add(newSong);
                        return songs;
                    }
                }
                return songs;
            }

            // Array of songs in page text.
            var songJSONAry = result["docs"]?.ToArray();

            if (songJSONAry == null)
            {

                Logger.Error("Invalid page text: 'songs' field not found.");
            }

            foreach (JObject song in songJSONAry)
            {
                newSong = ParseSongFromJson(song);
                if (newSong != null)
                    songs.Add(newSong);
            }
            return songs;
        }

        /// <summary>
        /// Creates a Song from a JObject. Sets the ScrapedAt time for the song.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static Song ParseSongFromJson(JObject song)
        {
            //JSONObject song = (JSONObject) aKeyValue;
            string songIndex = song["key"]?.Value<string>();
            //string songName = song["name"]?.Value<string>();
            //string author = song["uploader"]?["username"]?.Value<string>();
            //string songUrl = "https://beatsaver.com/download/" + songIndex;

            if (BeatSaverSong.TryParseBeatSaver(song, out Song newSong))
            {
                newSong.ScrapedAt = DateTime.Now;
                //Song songInfo = ScrapedDataProvider.GetOrCreateSong(newSong);

                //songInfo.BeatSaverInfo = newSong;
                return newSong;
            }
            else
            {
                if (!(string.IsNullOrEmpty(songIndex)))
                {
                    // TODO: look at this
                    Logger.Warning($"Couldn't parse song {songIndex}, skipping.");// using sparse definition.");
                    //return new SongInfo(songIndex, songName, songUrl, author);
                }
                else
                    Logger.Error("Unable to identify song, skipping");
            }
            return null;
        }
    }
}
