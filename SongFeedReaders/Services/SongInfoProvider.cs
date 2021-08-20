﻿using SongFeedReaders.Data;
using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Services
{
    public abstract class SongInfoProvider : ISongInfoProvider
    {
        private static FeedReaderLoggerBase? _logger;
        public static FeedReaderLoggerBase? Logger
        {
            get { return _logger ?? LoggingController.DefaultLogger; }
            set { _logger = value; }
        }

        public int Priority { get; set; }

        public abstract bool Available { get; }

        public abstract Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken);


        public abstract Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken);

        public Task<ScrapedSong?> GetSongByHashAsync(string hash) => GetSongByHashAsync(hash, CancellationToken.None);
        public Task<ScrapedSong?> GetSongByKeyAsync(string key) => GetSongByKeyAsync(key, CancellationToken.None);
    }
}
