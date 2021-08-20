using ProtoBuf;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities.DownloadContainers;

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
        public bool AllowWebFetch { get; set; }
        public bool CacheToDisk { get; set; }
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
            AndruzzProtobufContainer? songContainer = null;
            string source = "None";
            bool fetchWeb = AllowWebFetch;
            try
            {
                string? filePath = FilePath;
                if (filePath != null && filePath.Length > 0)
                {
                    songContainer = ParseFile(filePath);
                    if (songContainer != null)
                    {
                        source = $"File|{filePath}";
                        TimeSpan dataAge = DateTime.UtcNow - songContainer.ScrapeTime;
                        if (dataAge < MaxAge)
                            fetchWeb = false;
                        else
                            Logger?.Debug($"Cached data is outdated ({songContainer.ScrapeTime:g}).");
                    }
                }

                if (fetchWeb)
                {
                    AndruzzProtobufContainer? webContainer = await ParseGzipWebSource(new Uri(ScrapedDataUrl)).ConfigureAwait(false);

                    if (webContainer != null)
                    {
                        songContainer = webContainer;
                        source = $"GitHub|{ScrapedDataUrl}";
                    }
                    else if (songContainer != null)
                        Logger?.Warning($"Unable to fetch updated data from web, using cached data.");
                }
                AndruzzProtobufSong[]? songs = songContainer?.songs;
                if (songs != null)
                {
                    CreateDictionaries(songs.Length);
                    foreach (AndruzzProtobufSong song in songs)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (song.Hash != null)
                            _byHash[song.Hash] = song;
                        if (song.Key != null && song.Key.Length > 0)
                            _byKey[song.Key] = song;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    _available = true;
                    Logger?.Debug($"{songs?.Length ?? 0} songs data loaded from '{source}', format version {songContainer?.formatVersion}, last updated {songContainer?.ScrapeTime:g}");
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
            return true;
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
                    if (songContainer == null)
                        Logger?.Warning($"Failed to load song info protobuf file at '{filePath}'");
                }
                catch (Exception ex)
                {
                    Logger?.Warning($"Error reading song info protobuf file at '{filePath}': {ex.Message}");
                }
            }
            return songContainer;
        }

        private async Task<AndruzzProtobufContainer?> ParseGzipWebSource(Uri uri)
        {
            AndruzzProtobufContainer? songContainer = null;
            try
            {
                WebUtilities.IWebResponseMessage? downloadResponse = await WebUtils.WebClient.GetAsync(uri).ConfigureAwait(false);
                downloadResponse.EnsureSuccessStatusCode();
                if (downloadResponse.Content != null)
                {
                    using Stream scrapeStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    if (CacheToDisk && !string.IsNullOrWhiteSpace(FilePath))
                    {
                        using GZipStream gstream = new GZipStream(scrapeStream, CompressionMode.Decompress);
                        using MemoryDownloadContainer container = new MemoryDownloadContainer();
                        await container.ReceiveDataAsync(gstream).ConfigureAwait(false);
                        using Stream ps = container.GetResultStream();
                        songContainer = ParseProtobuf(ps);
                        try
                        {
                            using Stream s = container.GetResultStream();
                            using Stream fs = File.Create(FilePath);
                            await s.CopyToAsync(fs).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Logger?.Warning($"Error caching Andruzz's scraped data to file '{FilePath}': {ex.Message}");
                        }
                    }
                    else
                    {
                        using GZipStream gstream = new GZipStream(scrapeStream, CompressionMode.Decompress);
                        songContainer = ParseProtobuf(gstream);
                    }

                }
            }
            catch (Exception ex)
            {
                Logger?.Warning($"Error loading Andruzz's Scrapped Data from GitHub: {ex.Message}");
            }
            return songContainer;
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

        private bool _available = false;
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
