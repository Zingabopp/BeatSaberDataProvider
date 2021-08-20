using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Services
{
    public class OldAndruzzScrapedInfoProvider : SongInfoProvider
    {
        private const string ScrapedDataUrl = @"https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/beatSaverScrappedData.zip";
        private const string dataFileName = "beatSaverScrappedData.json";
        private Dictionary<string, ScrapedSong> _byHash = new Dictionary<string, ScrapedSong>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ScrapedSong> _byKey = new Dictionary<string, ScrapedSong>(StringComparer.OrdinalIgnoreCase);
        private static JsonSerializer JsonSerializer = new JsonSerializer();
        private object _initializeLock = new object();
        private Task<bool>? initializeTask;

        private string? FilePath;
        public OldAndruzzScrapedInfoProvider() { }
        public OldAndruzzScrapedInfoProvider(string filePath)
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
            Stream? jsonStream = null;
            ZipArchive? zip = null;
            List<OldAndruzzScrapedSong>? songList = null;
            try
            {
                if (FilePath != null && File.Exists(FilePath))
                {
                    try
                    {
                        jsonStream = File.OpenRead(FilePath);
                        songList = ParseJson(jsonStream);
                        Logger?.Debug($"{songList?.Count ?? 0} songs data loaded from '{FilePath}'");
                    }
                    catch (Exception ex)
                    {
                        Logger?.Warning($"Error reading song info json file at '{FilePath}': {ex.Message}");
                    }
                }
                if (songList == null || songList.Count == 0)
                {
                    try
                    {
                        WebUtilities.IWebResponseMessage? downloadResponse = await WebUtils.WebClient.GetAsync(ScrapedDataUrl).ConfigureAwait(false);
                        downloadResponse.EnsureSuccessStatusCode();
                        if (downloadResponse.Content != null)
                        {
                            scrapeStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            zip = new ZipArchive(scrapeStream, ZipArchiveMode.Read);
                            jsonStream = zip.GetEntry(dataFileName).Open();
                            songList = ParseJson(jsonStream);
                            Logger?.Debug($"{songList?.Count ?? 0} songs data loaded from GitHub.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.Warning($"Error loading Andruzz's Scrapped Data from GitHub: {ex.Message}");
                    }
                }
                if (songList != null)
                {
                    foreach (OldAndruzzScrapedSong? song in songList)
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
                jsonStream?.Dispose();
                zip?.Dispose();
                scrapeStream?.Dispose();
            }

            return true;
        }

        private List<OldAndruzzScrapedSong>? ParseJson(Stream jsonStream)
        {
            using StreamReader? sr = new StreamReader(jsonStream);
            using JsonTextReader? jr = new JsonTextReader(sr);
            return JsonSerializer.Deserialize<List<OldAndruzzScrapedSong>>(jr);
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
