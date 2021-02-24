﻿using Newtonsoft.Json;
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
        private const string ScrapedDataUrl = @"https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/beatSaverScrappedData.zip";
        private const string dataFileName = "beatSaverScrappedData.json";
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

        protected async Task<bool> InitializeData(CancellationToken cancellationToken)
        {
            lock (_initializeLock)
            {
                if (initializeTask == null)
                    initializeTask = InitializeDataInternal();
            }
            if (initializeTask.IsCompleted)
                return await initializeTask;
            TaskCompletionSource<bool> cancellationTask = new TaskCompletionSource<bool>();
            using CancellationTokenRegistration cancel = cancellationToken.Register(() => cancellationTask.TrySetResult(false));
            Task<bool>? finished = await Task<bool>.WhenAny(initializeTask, cancellationTask.Task);
            return await finished;

        }

        private async Task<bool> InitializeDataInternal()
        {
            Stream? scrapeStream = null;
            Stream? jsonStream = null;
            ZipArchive? zip = null;
            try
            {
                if (FilePath != null && File.Exists(FilePath))
                {
                    try
                    {
                        jsonStream = File.OpenRead(FilePath);
                    }
                    catch { }
                }
                if (jsonStream == null)
                {
                    WebUtilities.IWebResponseMessage? downloadResponse = await WebUtils.WebClient.GetAsync(ScrapedDataUrl).ConfigureAwait(false);
                    downloadResponse.EnsureSuccessStatusCode();
                    if (downloadResponse.Content != null)
                    {
                        scrapeStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        zip = new ZipArchive(scrapeStream, ZipArchiveMode.Read);
                        jsonStream = zip.GetEntry(dataFileName).Open();
                    }
                }

                if (jsonStream != null)
                {
                    using StreamReader? sr = new StreamReader(jsonStream);
                    using JsonTextReader? jr = new JsonTextReader(sr);
                    List<AndruzzScrapedSong>? songList = JsonSerializer.Deserialize<List<AndruzzScrapedSong>>(jr);
                    if (songList != null)
                    {
                        foreach (AndruzzScrapedSong? song in songList)
                        {
                            if (song.Hash != null)
                                _byHash[song.Hash] = song;
                            if (song.Key != null)
                                _byKey[song.Key] = song;
                        }
                    }
                }
            }
            finally
            {
                jsonStream?.Dispose();
                zip?.Dispose();
                scrapeStream?.Dispose();
            }

            return true;
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
