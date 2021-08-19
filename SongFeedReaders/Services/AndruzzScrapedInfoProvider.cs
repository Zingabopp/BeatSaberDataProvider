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
        private Dictionary<string, ScrapedSong>? _byHash;
        private Dictionary<string, ScrapedSong>? _byKey;
        private object _initializeLock = new object();
        private Task<bool>? initializeTask;

        private readonly string? FilePath;
        public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(2);
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
        private enum DataSource
        {
            GitHub,
            File
        }

        private static AndruzzProtobufContainer? ParseFile(string filePath)
        {
            AndruzzProtobufContainer? songContainer = null;
            if (filePath != null && File.Exists(filePath))
            {
                try
                {
                    using Stream? fs = File.OpenRead(filePath);
                    songContainer = ParseProtobuf(fs);
                    if (songContainer != null)
                        Logger?.Debug($"{songContainer.songs?.Length ?? 0} songs data loaded from '{filePath}', format version {songContainer.formatVersion}, last updated {songContainer.ScrapeTime:g}");
                    else
                        Logger?.Warning($"Failed to load song info protobuf file at '{filePath}'");
                }
                catch (Exception ex)
                {
                    Logger?.Warning($"Error reading song info protobuf file at '{filePath}': {ex.Message}");
                }
            }
            return songContainer;
        }

        private async Task<bool> InitializeDataInternal()
        {
            Stream? scrapeStream = null;
            Stream? protobuf = null;
            AndruzzProtobufContainer? songContainer = null;
            DataSource source;
            try
            {
                string? filePath = FilePath;
                if (filePath != null && filePath.Length > 0)
                {
                    songContainer = ParseFile(filePath);
                    source = DataSource.File;
                }
                if (songContainer?.songs == null 
                    || songContainer.songs.Length == 0 
                    || (DateTime.UtcNow - songContainer.ScrapeTime) > MaxAge)
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
                if (songContainer?.songs != null)
                {
                    CreateDictionaries(songContainer.songs.Length);
                    foreach (AndruzzProtobufSong song in songContainer.songs)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (song.Hash != null)
                            _byHash[song.Hash] = song;
                        if (song.Key != null && song.Key.Length > 0)
                            _byKey[song.Key] = song;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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
                protobuf?.Dispose();
                scrapeStream?.Dispose();
            }

            return true;
        }

        private void CreateDictionaries(int size)
        {
            if (_byHash == null)
                _byHash = new Dictionary<string, ScrapedSong>(size, StringComparer.OrdinalIgnoreCase);
            if (_byKey == null)
                _byKey = new Dictionary<string, ScrapedSong>(size, StringComparer.OrdinalIgnoreCase);
        }

        private static AndruzzProtobufContainer? ParseProtobuf(Stream protobufStream)
        {
            return Serializer.Deserialize<AndruzzProtobufContainer>(protobufStream);
        }

        private bool _available = true;
        public override bool Available => _available;

        public override async Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {
            await InitializeData(cancellationToken).ConfigureAwait(false);
            ScrapedSong? song = null;
            _byHash?.TryGetValue(hash, out song);
            return song;
        }

        public override async Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {
            await InitializeData(cancellationToken).ConfigureAwait(false);
            ScrapedSong? song = null;
            _byKey?.TryGetValue(key, out song);
            return song;
        }
    }
}
