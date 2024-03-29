﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VSCThemesStore.WebApi.Domain.Models;
using VSCThemesStore.WebApi.Domain.Repositories;
using Polly;
using Polly.Retry;
using Serilog;

namespace VSCThemesStore.WebApi.Services.ThemeStoreRefreshing
{
    public interface IThemeStoreRefresher
    {
        Task<ExtensionType> GetSavedExtensionType(string extensionId);
        Task<VSCodeTheme> GetStoredTheme(string extensionId);
        Task<VSCodeTheme> DownloadFreshTheme(ExtensionMetadata metadata);
        Task<ExtensionType> CheckAndUpdateFreshExtensionType(VSCodeTheme freshTheme);
        Task CreateTheme(VSCodeTheme newTheme);
        Task UpdateTheme(VSCodeTheme theme);
    }

    public class ThemeStoreRefresher : IThemeStoreRefresher
    {
        private readonly IVSAssetsClient _assetsClient;
        private readonly IVSExtensionHandler _extensionHandler;
        private readonly IThemeRepository _themeRepository;
        private readonly IExtensionsMetadataRepository _metadataRepository;

        public ThemeStoreRefresher(
            IVSAssetsClient assetsClient,
            IVSExtensionHandler extensionHandler,
            IThemeRepository themeRepository,
            IExtensionsMetadataRepository metadataRepository)
        {
            _assetsClient = assetsClient;
            _extensionHandler = extensionHandler;
            _themeRepository = themeRepository;
            _metadataRepository = metadataRepository;
        }

        public async Task<ExtensionType> GetSavedExtensionType(string extensionId)
        {
            var savedThemeMetadata = await _metadataRepository.GetExtensionMetadata(extensionId);

            return savedThemeMetadata.Type;
        }

        public async Task<VSCodeTheme> GetStoredTheme(string extensionId)
        {
            return await _themeRepository.GetTheme(extensionId);
        }

        public async Task<VSCodeTheme> DownloadFreshTheme(ExtensionMetadata metadata)
        {
            VSCodeTheme freshTheme;
            using (var stream = await _assetsClient.GetVsixFileStream(metadata))
            {
                freshTheme = await _extensionHandler.ProcessExtension(metadata.Id, stream);
            }

            return freshTheme;
        }

        public async Task<ExtensionType> CheckAndUpdateFreshExtensionType(VSCodeTheme freshTheme)
        {
            if (freshTheme.Themes.Count == 0)
            {
                Log.Information($"Extension '{freshTheme.Id}' does not contribute any theme. Changing its Type.");
                const ExtensionType themeType = ExtensionType.NoThemes;
                var result = await _metadataRepository.ChangeExtensionType(
                    freshTheme.Id,
                    themeType
                );

                if (result)
                {
                    Log.Information($"Successfully changed Type to {themeType}.");
                }
                else
                {
                    Log.Warning($"Type change unsuccessful: ExtensionId: '{freshTheme.Id}'.");
                }

                return themeType;
            }
            if (freshTheme.Themes.Any(t => t.TokenColors.Count == 0))
            {
                Log.Information($"Extension '{freshTheme.Id}' does not contains any TokenColor. Changing its Type.");
                const ExtensionType themeType = ExtensionType.NeedAttention;
                var result = await _metadataRepository.ChangeExtensionType(
                    freshTheme.Id,
                    themeType
                );

                if (result)
                {
                    Log.Warning($"Successfully changed Type to {themeType}.");
                }
                else
                {
                    Log.Warning($"Type change unsuccessful: ExtensionId: '{freshTheme.Id}'.");
                }

                return themeType;
            }

            return ExtensionType.Default;
        }

        public async Task CreateTheme(VSCodeTheme newTheme)
        {
            await _themeRepository.Create(newTheme);
            Log.Information($"Created new theme in store: Id='{newTheme.Id}'.");
        }

        public async Task UpdateTheme(VSCodeTheme theme)
        {
            var result = await _themeRepository.Update(theme);

            if (result)
            {
                Log.Information($"Successfuly updated theme in store: ThemeId='{theme.Id}'.");
            }
            else
            {
                Log.Warning($"Update unsuccessful theme in store: ThemeId='{theme.Id}'.");
            }
        }
    }
}
