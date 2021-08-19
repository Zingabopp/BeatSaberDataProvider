using Newtonsoft.Json;
using ProtoBuf;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Services
{
    public class AndruzzScrapedInfoProvider : SongInfoProvider
    {
        private const string ScrapedDataUrl = @"https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/songDetails2.gz";
        private const string dataFileName = "songDetails2.gz";
        private Dictionary<string, ScrapedSong> _byHash = new Dictionary<string, ScrapedSong>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ScrapedSong> _byKey = new Dictionary<string, ScrapedSong>(StringComparer.OrdinalIgnoreCase);
        private static JsonSerializer JsonSerializer = new JsonSerializer();
        private object _initializeLock = new object();
        private Task<bool>? initializeTask;

        private string? FilePath;
        public AndruzzScrapedInfoProvider() { }
        public AndruzzScrapedInfoProvider(string filePath)
        {
            FilePath = filePath;
        }

        protected Task<bool> InitializeData(CancellationToken cancellationToken)
        {
            lock (_initializeLock)
            {
                if (initializeTask == null)
                    initializeTask = InitializeDataInternal();
            }
            if (initializeTask.IsCompleted)
                return initializeTask;
            else
            {
                var finished = Task.Run(async () => await initializeTask.ConfigureAwait(false), cancellationToken);
                return finished;
            }
        }

        private async Task<bool> InitializeDataInternal()
        {
            Stream? scrapeStream = null;
            Stream? protobu = null;
            AndruzzProtobufContainer? songContainer = null;
            try
            {
                if (FilePath != null && File.Exists(FilePath))
                {
                    try
                    {
                        protobu = File.OpenRead(FilePath);
                        songContainer = ParseProtobuf(protobu);
                        Logger?.Debug($"{songContainer?.songs.Length ?? 0} songs data loaded from '{FilePath}'");
                    }
                    catch (Exception ex)
                    {
                        Logger?.Warning($"Error reading song info json file at '{FilePath}': {ex.Message}");
                    }
                }
                if (songContainer?.songs == null || songContainer.songs.Length == 0)
                {
                    try
                    {
                        WebUtilities.IWebResponseMessage? downloadResponse = await WebUtils.WebClient.GetAsync(ScrapedDataUrl).ConfigureAwait(false);
                        downloadResponse.EnsureSuccessStatusCode();
                        if (downloadResponse.Content != null)
                        {
                            scrapeStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            using GZipStream gstream = new GZipStream(scrapeStream, CompressionMode.Decompress);
                            songContainer = ParseProtobuf(gstream);
                            Logger?.Debug($"{songContainer?.songs?.Length ?? 0} songs data loaded from GitHub.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.Warning($"Error loading Andruzz's Scrapped Data from GitHub: {ex.Message}");
                    }
                }
                if (songContainer != null)
                {
                    foreach (AndruzzProtobufSong? song in songContainer.songs)
                    {
                        if (song.Hash != null)
                            _byHash[song.Hash] = song;
                        if (song.Key != null)
                            _byKey[song.Key] = song;
                    }
                }
                else
                {
                    Logger?.Warning("Unable to load Andruzz's Scrapped Data.");
                }
            }
            catch (Exception ex)
            {
                Logger?.Warning($"Error loading Andruzz's Scrapped Data: {ex.Message}");
            }
            finally
            {
                protobu?.Dispose();
                scrapeStream?.Dispose();
            }

            return true;
        }

        private AndruzzProtobufContainer? ParseProtobuf(Stream protobufStream)
        {
            return Serializer.Deserialize<AndruzzProtobufContainer>(protobufStream);
        }

        private bool _available = true;
        public override bool Available => _available;

        public override async Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {
            await InitializeData(cancellationToken).ConfigureAwait(false);
            if (_byHash.TryGetValue(hash, out ScrapedSong song))
                return song;
            return null;
        }

        public override async Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {
            await InitializeData(cancellationToken).ConfigureAwait(false);
            if (_byKey.TryGetValue(key, out ScrapedSong song))
                return song;
            return null;
        }
    }
}
