using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberDataProvider.DataModels
{
    [Serializable]
    public class SongHashData
    {
        [JsonIgnore]
        public string Directory { get; set; }
        [JsonProperty("directoryHash")]
        public long DirectoryHash { get; set; }
        [JsonProperty("songHash")]
        public string SongHash { get; set; }

        public SongHashData() { }

        public SongHashData(JProperty token, string directory)
        {
            Directory = directory;
            token.Value.Populate(this);
        }

        /// <summary>
        /// Generates a hash for the song and assigns it to the SongHash field.
        /// Uses Kylemc1413's implementation from SongCore.
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <returns>Hash of the song files.</returns>
        public string GenerateHash()
        {
            byte[] combinedBytes = new byte[0];
            string infoFile = Path.Combine(Directory, "info.dat");
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(infoFile)).ToArray();
            var token = JToken.Parse(File.ReadAllText(infoFile));
            var beatMapSets = token["_difficultyBeatmapSets"];
            int numChars = beatMapSets.Children().Count();
            for (int i = 0; i < numChars; i++)
            {
                var diffs = beatMapSets.ElementAt(i);
                int numDiffs = diffs["_difficultyBeatmaps"].Children().Count();
                for (int i2 = 0; i2 < numDiffs; i2++)
                {
                    var diff = diffs["_difficultyBeatmaps"].ElementAt(i2);
                    string beatmapPath = Path.Combine(Directory, diff["_beatmapFilename"].Value<string>());
                    if (File.Exists(beatmapPath))
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath)).ToArray();
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            if (!string.IsNullOrEmpty(SongHash) && SongHash != hash)
                Console.WriteLine("Hash doesn't match!");
            SongHash = hash;
            return hash;
        }

        /// <summary>
        /// Returns the Sha1 hash of the provided byte array.
        /// Uses Kylemc1413's implementation from SongCore.
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <param name="input">Byte array to hash.</param>
        /// <returns>Sha1 hash of the byte array.</returns>
        public static string CreateSha1FromBytes(byte[] input)
        {
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = input;
                var hashBytes = sha1.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }
    }
}
