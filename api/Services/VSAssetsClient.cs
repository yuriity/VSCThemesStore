using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VSCThemesStore.WebApi.Domain.Models;
using Serilog;

namespace VSCThemesStore.WebApi.Services
{
    public interface IVSAssetsClient
    {
        Task<Stream> GetVsixFileStream(ExtensionMetadata metadata);
    }

    public class VSAssetsClient : IVSAssetsClient
    {
        private readonly HttpClient _httpClient;

        public VSAssetsClient(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<Stream> GetVsixFileStream(ExtensionMetadata metadata)
        {
            EnsureValidMetadata(metadata);

            try
            {
                Log.Information($"Requesting vsix file stream from: {metadata.AssetUri}");

                return await _httpClient.GetStreamAsync(metadata.AssetUri);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error while requesting vsix file stream from: '{metadata.AssetUri}'.");
                throw;
            }
        }

        private void EnsureValidMetadata(ExtensionMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.PublisherName))
            {
                throw new ArgumentNullException(nameof(metadata.PublisherName));
            }

            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                throw new ArgumentNullException(nameof(metadata.Name));
            }

            if (string.IsNullOrWhiteSpace(metadata.Version))
            {
                throw new ArgumentNullException(nameof(metadata.Version));
            }
        }
    }
}
