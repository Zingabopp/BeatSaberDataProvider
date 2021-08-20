using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Services
{
    public class SongInfoManager
    {
        private readonly List<InfoProviderEntry> InfoProviders = new List<InfoProviderEntry>();
        private object _providerLock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="SongInfoManager"/> has no providers.</exception>
        public async Task<SongInfoResponse> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {
            InfoProviderEntry[] infoProviders = GetProvidersEntries();
            if (infoProviders.Length == 0) throw new InvalidOperationException("SongInfoManager has no providers.");
            List<SongInfoResponse> failedResponses = new List<SongInfoResponse>();
            SongInfoResponse? lastResponse = null;
            for (int i = 0; i < infoProviders.Length; i++)
            {
                try
                {
                    ScrapedSong? song = await infoProviders[i].InfoProvider.GetSongByHashAsync(hash, cancellationToken).ConfigureAwait(false);
                    lastResponse = new SongInfoResponse(song, infoProviders[i].InfoProvider);
                }
                catch (Exception ex)
                {
                    lastResponse = new SongInfoResponse(null, infoProviders[i].InfoProvider, ex);
                }
                if (lastResponse.Success)
                {
                    break;
                }
                else
                    failedResponses.Add(lastResponse);
            }
            if (lastResponse != null)
            {
                if (failedResponses.Count > 0)
                    lastResponse.FailedResponses = failedResponses.ToArray();
                return lastResponse;
            }
            else
                return SongInfoResponse.FailedResponse;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="SongInfoManager"/> has no providers.</exception>
        public async Task<SongInfoResponse> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {
            InfoProviderEntry[] infoProviders = GetProvidersEntries();
            if (infoProviders.Length == 0) throw new InvalidOperationException("SongInfoManager has no providers.");
            List<SongInfoResponse> failedResponses = new List<SongInfoResponse>();
            SongInfoResponse? lastResponse = null;
            for (int i = 0; i < infoProviders.Length; i++)
            {
                try
                {
                    ScrapedSong? song = await infoProviders[i].InfoProvider.GetSongByKeyAsync(key, cancellationToken).ConfigureAwait(false);
                    lastResponse = new SongInfoResponse(song, infoProviders[i].InfoProvider);
                }
                catch (Exception ex)
                {
                    lastResponse = new SongInfoResponse(null, infoProviders[i].InfoProvider, ex);
                }
                if (lastResponse.Success)
                {
                    break;
                }
                else
                    failedResponses.Add(lastResponse);
            }
            if (lastResponse != null)
            {
                if (failedResponses.Count > 0)
                    lastResponse.FailedResponses = failedResponses.ToArray();
                return lastResponse;
            }
            else
                return SongInfoResponse.FailedResponse;
        }
        public T AddProvider<T>() where T : ISongInfoProvider, new()
        {
            T provider = new T();
            AddProvider(provider);
            return provider;
        }

        public T AddProvider<T>(string providerId, int priority = 100) where T : ISongInfoProvider, new()
        {
            T provider = new T();
            AddProvider(provider, providerId, priority);
            return provider;
        }

        public void AddProvider(ISongInfoProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            string[] existingIds;
            lock (_providerLock)
            {
                existingIds = InfoProviders.Select(p => p.ProviderID).ToArray();
            }
            int idIndex = 0;
            string defaultId;
            do
            {
                defaultId = $"{provider.GetType().Name}{idIndex}";
                idIndex++;
            }
            while (existingIds.Contains(defaultId));
            lock (_providerLock)
            {
                InfoProviders.Add(new InfoProviderEntry(provider, defaultId) { Priority = 100 });
            }
        }
        public void AddProvider(ISongInfoProvider provider, string providerId, int priority = 100)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            lock (_providerLock)
            {
                InfoProviders.Add(new InfoProviderEntry(provider, providerId) { Priority = priority });
            }
        }

        public bool RemoveProvider(InfoProviderEntry infoProviderEntry)
        {
            lock (_providerLock)
            {
                return InfoProviders.Remove(infoProviderEntry);
            }

        }

        public bool RemoveProvider(string providerId)
        {
            InfoProviderEntry? entry = GetProviderEntry(providerId);
            if (entry != null)
                return RemoveProvider(entry);
            else
                return false;
        }

        public InfoProviderEntry[] GetProvidersEntries()
        {
            lock (_providerLock)
            {
                return InfoProviders.OrderBy(e => e.Priority).ToArray();
            }
        }

        public InfoProviderEntry[] GetProvidersEntries<T>() where T : ISongInfoProvider
        {
            lock (_providerLock)
            {
                return InfoProviders.Where(p => p.InfoProvider is T).OrderBy(e => e.Priority).ToArray();
            }
        }

        public InfoProviderEntry[] GetProvidersEntries(Func<InfoProviderEntry, bool> predicate)
        {
            lock (_providerLock)
            {
                return InfoProviders.Where(p => predicate(p)).OrderBy(e => e.Priority).ToArray();
            }
        }

        public InfoProviderEntry? GetProviderEntry(string providerId)
        {
            lock (_providerLock)
            {
                return InfoProviders.FirstOrDefault(p => p.ProviderID == providerId);
            }

        }
        public InfoProviderEntry? GetProviderEntry<T>() where T : ISongInfoProvider
        {
            lock (_providerLock)
            {
                return InfoProviders.FirstOrDefault(p => p.InfoProvider is T);
            }

        }
        public InfoProviderEntry? GetProviderEntry<T>(string providerId) where T : ISongInfoProvider
        {
            lock (_providerLock)
            {

                return InfoProviders.FirstOrDefault(p => p.InfoProvider is T && p.ProviderID == providerId);
            }
        }

        public Task<SongInfoResponse> GetSongByHashAsync(string hash) => GetSongByHashAsync(hash, CancellationToken.None);
        public Task<SongInfoResponse> GetSongByKeyAsync(string key) => GetSongByKeyAsync(key, CancellationToken.None);
    }

    public class SongInfoResponse
    {
        internal static SongInfoResponse FailedResponse = new SongInfoResponse(null, null);
        internal static SongInfoResponse[] EmptyResponseAry = new SongInfoResponse[0];
        public bool Success { get; }
        public ScrapedSong? Song { get; }
        public ISongInfoProvider? Source { get; }
        internal SongInfoResponse[] FailedResponses;
        public Exception? Exception { get; }
        public SongInfoResponse[] GetFailedResponses() => FailedResponses;
        internal SongInfoResponse(ScrapedSong? song, ISongInfoProvider? provider)
        {
            Song = song;
            Source = provider;
            Success = Song != null;
            FailedResponses = EmptyResponseAry;
        }
        internal SongInfoResponse(ScrapedSong? song, ISongInfoProvider? provider, Exception? exception)
            : this(song, provider)
        {
            Exception = exception;
        }
    }

    public class InfoProviderEntry
    {
        public InfoProviderEntry(ISongInfoProvider infoProvider, string providerId)
        {
            InfoProvider = infoProvider;
            ProviderID = providerId;
        }
        public ISongInfoProvider InfoProvider { get; protected set; }
        public int Priority { get; set; }
        public string ProviderID { get; set; }
    }
}
