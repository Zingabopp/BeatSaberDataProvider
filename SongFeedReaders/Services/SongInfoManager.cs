using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Services
{
    public class SongInfoManager
    {
        private readonly List<InfoProviderEntry> InfoProviders = new List<InfoProviderEntry>();
        private object _providerLock = new object();
        public async Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {
            InfoProviderEntry[] infoProviders = GetProviders();
            for(int i = 0; i < infoProviders.Length; i++)
            {
                ScrapedSong? song = await infoProviders[i].InfoProvider.GetSongByHashAsync(hash, cancellationToken).ConfigureAwait(false);
                if (song != null)
                    return song;
            }
            return null;
        }
        public async Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {
            InfoProviderEntry[] infoProviders = GetProviders();
            for (int i = 0; i < infoProviders.Length; i++)
            {
                ScrapedSong? song = await infoProviders[i].InfoProvider.GetSongByKeyAsync(key, cancellationToken).ConfigureAwait(false);
                if (song != null)
                    return song;
            }
            return null;
        }
        public void AddProvider<T>() where T : ISongInfoProvider, new()
        {
            string[] existingIds;
            lock (_providerLock)
            {
                existingIds = InfoProviders.Select(p => p.ProviderID).ToArray();
            }
            int idIndex = 0;
            string defaultId;
            do
            {
                defaultId = $"{typeof(T).Name}{idIndex}";
                idIndex++;
            }
            while (existingIds.Contains(defaultId)); 
            lock (_providerLock)
            {
                InfoProviders.Add(new InfoProviderEntry(new T(), defaultId) { Priority = 100 });
            }
        }
        public void AddProvider<T>(string providerId, int priority = 100) where T : ISongInfoProvider, new()
        {
            lock (_providerLock)
            {
                InfoProviders.Add(new InfoProviderEntry(new T(), providerId) { Priority = priority });
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

        public InfoProviderEntry[] GetProviders()
        {
            lock (_providerLock)
            {
                return InfoProviders.OrderBy(e => e.Priority).ToArray();
            }
            
        }

        public InfoProviderEntry[] GetProviders(Func<InfoProviderEntry, bool> predicate)
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
