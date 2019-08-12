﻿using BeatSaberDataProvider.Util;
using BeatSaberDataProvider.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace BeatSaberDataProvider.DataModels
{
    /// <summary>
    /// Holds all the data for a song provided by Beat Saver's API.
    /// </summary>
    public class BeatSaverSong : IEquatable<BeatSaverSong>
    {
        private const string SONG_DETAILS_URL_BASE = "https://beatsaver.com/api/songs/detail/";
        private const string SONG_BY_HASH_URL_BASE = "https://beatsaver.com/api/songs/search/hash/";

        [JsonIgnore]
        public bool Populated { get; private set; }
        private static readonly Regex _digitRegex = new Regex("^(?:0[xX])?([0-9a-fA-F]+)$", RegexOptions.Compiled);
        private static readonly Regex _oldBeatSaverRegex = new Regex("^[0-9]+-[0-9]+$", RegexOptions.Compiled);

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(Song);
                object retVal;
                FieldInfo test = myType.GetField(propertyName);
                if (test != null)
                {
                    retVal = test.GetValue(this);
                }
                else
                {
                    PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                    retVal = myPropInfo.GetValue(this);
                }

                Type whatType = retVal.GetType();
                return retVal;
            }
            set
            {
                Type myType = typeof(Song);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);
            }
        }

        public bool PopulateFields()
        {
            return PopulateFieldsAsync().Result;
        }

        enum SongGetMethod
        {
            SongIndex,
            Hash
        }

        public async Task<bool> PopulateFieldsAsync()
        {
            if (Populated)
                return true;
            SongGetMethod searchMethod;
            Uri uri;
            if (!string.IsNullOrEmpty(key))
            {
                uri = new Uri(SONG_DETAILS_URL_BASE + key);
                searchMethod = SongGetMethod.SongIndex;
            }
            else if (!string.IsNullOrEmpty(hash))
            {
                uri = new Uri(SONG_BY_HASH_URL_BASE + hash);
                searchMethod = SongGetMethod.Hash;
            }
            else
                return false;
            Logger.Debug($"Starting PopulateFieldsAsync for {key}");
            bool successful = true;

            string pageText = "";
            try
            {
                using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
                    pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                Logger.Error($"Timeout occurred while trying to populate fields for {key}");
                return false;
            }
            catch (HttpRequestException)
            {
                Logger.Error($"HttpRequestException while trying to populate fields for {key}");
                return false;
            }
            JObject result = new JObject();
            try
            {
                result = JObject.Parse(pageText);
            }
            catch (JsonReaderException ex)
            {
                Logger.Exception("Unable to parse JSON from text", ex);
            }
            lock (this)
            {
                if (searchMethod == SongGetMethod.SongIndex)
                    JsonConvert.PopulateObject(result["song"].ToString(), this);
                else if (searchMethod == SongGetMethod.Hash)
                    JsonConvert.PopulateObject(result["songs"].First.ToString(), this);
            }
            Logger.Debug($"Finished PopulateFieldsAsync for {key}");
            Populated = successful;
            return successful;
        }

        public BeatSaverSong()
        {
            metadata = new SongMetadata();
            stats = new SongStats();
            uploader = new SongUploader();
        }

        public BeatSaverSong(string songIndex, string songName, string songUrl, string _authorName)
            : this()
        {
            key = songIndex;
            name = songName;
            metadata.levelAuthorName = _authorName;
            downloadURL = songUrl;
        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            Populated = true;
        }



        public static bool TryParseBeatSaver(JToken token, out Song song)
        {
            string songIndex = token["key"]?.Value<string>();
            if (songIndex == null)
                songIndex = "";
            bool successful = true;
            Song beatSaverSong;
            try
            {
                beatSaverSong = token.ToObject<Song>(new JsonSerializer()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });

                song = beatSaverSong;
                //song.EnhancedInfo = enhancedSong;
                //Logger.Debug(song.ToString());
            }
            catch (Exception ex)
            {
                Logger.Exception($"Unable to create a SongInfo from the JSON for {songIndex}\n", ex);
                successful = false;
                song = null;
            }
            return successful;
        }

        public override string ToString()
        {
            StringBuilder retStr = new StringBuilder();
            retStr.Append("SongInfo:");
            retStr.AppendLine("   Index: " + key);
            retStr.AppendLine("   Name: " + name);
            retStr.AppendLine("   Author: " + metadata.levelAuthorName);
            retStr.AppendLine("   URL: " + downloadURL);
            return retStr.ToString();
        }

        public bool Equals(BeatSaverSong other)
        {
            if (other == null)
                return false;
            return hash.ToUpper() == other.hash.ToUpper();
        }

        [JsonIgnore]
        public int KeyAsInt
        {
            get
            {
                var match = _digitRegex.Match(key);
                return match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber) : 0;
            }
        }

        [JsonProperty("metadata")]
        public SongMetadata metadata { get; }
        [JsonProperty("stats")]
        public SongStats stats { get; }

        [JsonProperty("description")]
        public string description { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? deletedAt { get; set; }
        [JsonProperty("_id")]
        public string _id { get; set; }
        [JsonIgnore]
        private string _key;
        /// <summary>
        /// Key is always lowercase.
        /// </summary>
        [JsonProperty("key")]
        public string key { get { return _key; } set { _key = value.ToLower(); } }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("uploader")]
        public SongUploader uploader { get; }
        [JsonProperty("uploaded")]
        public DateTime uploaded { get; set; }
        [JsonIgnore]
        private string _hash;
        /// <summary>
        /// Hash is always uppercase.
        /// </summary>
        [JsonProperty("hash")]
        public string hash { get { return _hash; } set { _hash = value.ToUpper(); } }
        [JsonProperty("converted")]
        public string converted { get; set; }
        [JsonProperty("downloadURL")]
        public string downloadURL { get; set; }
        [JsonProperty("coverURL")]
        public string coverURL { get; set; }

        [JsonProperty("ScrapedAt")]
        public DateTime ScrapedAt { get; set; }

        /*
        [JsonIgnore]
        private SongMetadata _metadata;
        [JsonIgnore]
        private SongStats _stats;
        [JsonIgnore]
        private SongUploader _uploader;
        */


    }

    public class SongMetadata
    {
        [JsonProperty("difficulties")]
        public Dictionary<string, bool> difficulties { get; set; }

        [JsonProperty("characteristics")]
        public List<JsonBeatmapCharacteristic> characteristics { get; set; }

        [JsonProperty("songName")]
        public string songName { get; set; }

        [JsonProperty("songSubName")]
        public string songSubName { get; set; }

        [JsonProperty("songAuthorName")]
        public string songAuthorName { get; set; }

        [JsonProperty("levelAuthorName")]
        public string levelAuthorName { get; set; }

        [JsonProperty("bpm")]
        public float bpm { get; set; }
    }

    public class JsonBeatmapCharacteristic
    {
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("difficulties")]
        public Dictionary<string, DifficultyCharacteristic> difficulties { get; set; }
    }

    [Serializable]
    public class DifficultyCharacteristic
    {
        [JsonProperty("duration")]
        public double duration { get; set; }
        [JsonProperty("length")]
        public int length { get; set; }
        [JsonProperty("bombs")]
        public int bombs { get; set; }
        [JsonProperty("notes")]
        public int notes { get; set; }
        [JsonProperty("obstacles")]
        public int obstacles { get; set; }
        [JsonProperty("njs")]
        public float njs { get; set; }
        [JsonProperty("njsOffset")]
        public float njsOffset { get; set; }
    }

    public class SongStats
    {
        [JsonProperty("downloads")]
        public int downloads { get; set; }
        [JsonProperty("plays")]
        public int plays { get; set; }
        [JsonProperty("downVotes")]
        public int downVotes { get; set; }
        [JsonProperty("upVotes")]
        public int upVotes { get; set; }
        [JsonProperty("heat")]
        public double heat { get; set; }
        [JsonProperty("rating")]
        public double rating { get; set; }
    }

    public class SongUploader
    {
        [JsonProperty("_id")]
        public string id { get; set; }
        [JsonProperty("username")]
        public string username { get; set; }
    }


    public static class SongInfoEnhancedExtensions
    {
        public static void PopulateFromBeatSaver(this IEnumerable<BeatSaverSong> songs)
        {
            songs.PopulateFromBeatSaverAsync().Wait();
        }

        public static async Task PopulateFromBeatSaverAsync(this IEnumerable<BeatSaverSong> songs)
        {
            List<Task> populateTasks = new List<Task>();
            for (int i = 0; i < songs.Count(); i++)
            {
                if (!songs.ElementAt(i).Populated)
                    populateTasks.Add(songs.ElementAt(i).PopulateFieldsAsync());
            }

            await Task.WhenAll(populateTasks).ConfigureAwait(false);
        }
    }

}

