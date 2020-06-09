using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeatSaver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace SongFeedReaders.Services
{
    public class BeatSaverSongInfoProvider : ISongInfoProvider
    {
        public static string BeatSaverDetailsFromKeyBaseUrl => "https://beatsaver.com/api/maps/detail/";
        public static string BeatSaverDetailsFromHashBaseUrl => "https://beatsaver.com/api/maps/by-hash/";
        protected static Uri GetBeatSaverDetailsByKey(string key)
        {
            return new Uri(BeatSaverDetailsFromKeyBaseUrl + key.ToLower());
        }

        protected static Uri GetBeatSaverDetailsByHash(string hash)
        {
            return new Uri(BeatSaverDetailsFromHashBaseUrl + hash.ToLower());
        }

        /// <inheritdoc/>
        public int Priority { get; set; } = 100;

        /// <inheritdoc/>
        public bool Available => true;

        protected static async Task<ScrapedSong?> GetSongFromUriAsync(Uri uri, CancellationToken cancellationToken)
        {
            IWebResponseMessage? response = null;
            try
            {
                response = await WebUtils.GetBeatSaverAsync(uri, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content == null) return null;
                string pageText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return BeatSaverReader.ParseSongsFromPage(pageText, uri, true).FirstOrDefault();
            }
            catch (WebClientException ex)
            {
                string errorText = string.Empty;
                if (ex.Response != null)
                {
                    if (ex.Response.StatusCode == 404)
                        return null;
                    errorText = ex.Response.StatusCode switch
                    {
                        408 => "Timeout",
                        _ => "Site Error",
                    };
                }
                string message = $"Exception getting page: '{uri}'";
                throw new SongInfoProviderException(message, ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AggregateException ae)
            {
                string message = $"Exception while trying to get details from '{uri}'";
                throw new OperationCanceledException(message, ae);
            }
            catch (Exception ex)
            {
                string message = $"Exception while trying to get details from '{uri}'";
                throw new SongInfoProviderException(message, ex);
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <inheritdoc/>
        public Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
        {
            Uri uri = Utilities.GetBeatSaverDetailsByHash(hash);
            return GetSongFromUriAsync(uri, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
        {
            Uri uri = Utilities.GetBeatSaverDetailsByKey(key);
            return GetSongFromUriAsync(uri, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ScrapedSong?> GetSongByHashAsync(string hash) => GetSongByHashAsync(hash, CancellationToken.None);

        /// <inheritdoc/>
        public Task<ScrapedSong?> GetSongByKeyAsync(string key) => GetSongByKeyAsync(key, CancellationToken.None);
    }
}
