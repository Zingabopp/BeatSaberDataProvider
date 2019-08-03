using BeatSaberDataProvider.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BeatSaberDataProvider.DataModels
{
    [Serializable]
    public class SongHashData : IEquatable<SongHashData>
    {
        [JsonIgnore]
        public string Directory { get; set; }
        [JsonProperty("directoryHash")]
        public long DirectoryHash { get; set; }
        [JsonProperty("songHash")]
        public string SongHash { get; set; }

        public SongHashData() { }

        public SongHashData(string directory)
        {
            Directory = directory;
        }

        public SongHashData(JProperty token, string directory)
            : this(directory)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token), "JProperty token cannot be null when included in SongHashData's constructor.");
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
            byte[] combinedBytes = Array.Empty<byte>();
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
                Logger.Warning($"Hash doesn't match SongCore's data for {Directory}");
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

        public void GenerateDirectoryHash()
        {
            DirectoryHash = GenerateDirectoryHash(Directory);
        }

        /// <summary>
        /// Generates a quick hash of a directory's contents. Does NOT match SongCore.
        /// Uses most of Kylemc1413's implementation from SongCore.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when path's directory doesn't exist.</exception>
        /// <returns></returns>
        public static long GenerateDirectoryHash(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "Path cannot be null or empty for GenerateDirectoryHash");

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"GenerateDirectoryHash couldn't find {path}");
            long dirHash = 0L;
            foreach (var file in directoryInfo.GetFiles())
            {
                dirHash ^= file.CreationTimeUtc.ToFileTimeUtc();
                dirHash ^= file.LastWriteTimeUtc.ToFileTimeUtc();
                dirHash ^= SumCharacters(file.Name);
                dirHash ^= file.Length;
            }
            return dirHash;
        }

        private static int SumCharacters(string str)
        {
            unchecked
            {
                int charSum = 0;
                for (int i = 0; i < str.Count(); i++)
                {
                    charSum += str[i];
                }
                return charSum;
            }
        }

        /// <summary>
        /// Returns true if the folder path matches. Case sensitive
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SongHashData other)
        {
            if (other == null)
                return false;
            return Directory.Equals(other.Directory, StringComparison.CurrentCulture);
        }
    }
}
