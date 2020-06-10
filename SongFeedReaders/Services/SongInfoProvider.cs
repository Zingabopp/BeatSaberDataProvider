using SongFeedReaders.Data;
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
        public int Priority { get; set; }

        public abstract bool Available { get; }

        public abstract Task<IScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken);


        public abstract Task<IScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken);

        public Task<IScrapedSong?> GetSongByHashAsync(string hash) => GetSongByHashAsync(hash, CancellationToken.None);
        public Task<IScrapedSong?> GetSongByKeyAsync(string key) => GetSongByKeyAsync(key, CancellationToken.None);
    }
}
