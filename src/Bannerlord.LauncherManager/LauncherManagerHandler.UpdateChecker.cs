using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private const string UpdateTrackerFile = "update_tracker.json";
    private UpdateTrackerData? _updateTrackerData;
    private static readonly HttpClient UpdateHttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly JsonSerializerOptions UpdateJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// External<br/>
    /// Checks for updates for all or specified modules.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(UpdateCheckOptions? options = null)
    {
        options ??= new UpdateCheckOptions();
        await EnsureUpdateTrackerLoadedAsync();

        var result = new UpdateCheckResult { Success = true };
        var modules = await GetModulesAsync();

        // Filter modules if specified
        if (options.ModuleIds?.Count > 0)
        {
            modules = modules.Where(m => options.ModuleIds.Contains(m.Id)).ToList();
        }

        foreach (var module in modules)
        {
            var sourceInfo = _updateTrackerData!.SourceInfos.FirstOrDefault(s => s.ModuleId == module.Id);
            
            // Skip ignored unless forced
            if (sourceInfo?.IgnoreUpdates == true && !options.IncludeIgnored)
                continue;

            // Skip if recently checked unless forced
            if (!options.ForceRefresh && sourceInfo?.LastChecked != null)
            {
                var hoursSinceCheck = (DateTime.UtcNow - sourceInfo.LastChecked.Value).TotalHours;
                if (hoursSinceCheck < 1)
                {
                    // Use cached result
                    var cached = _updateTrackerData.LastCheckResult?.Modules
                        .FirstOrDefault(m => m.ModuleId == module.Id);
                    if (cached != null)
                    {
                        result.Modules.Add(cached);
                        continue;
                    }
                }
            }

            var versionInfo = new ModVersionInfo
            {
                ModuleId = module.Id,
                ModuleName = module.Name,
                InstalledVersion = module.Version.ToString(),
                Source = sourceInfo?.Source ?? DetectModSource(module),
                Status = UpdateCheckStatus.NotTracked
            };

            // Check for updates based on source
            if (sourceInfo != null || versionInfo.Source != ModUpdateSource.Unknown)
            {
                try
                {
                    await CheckModuleUpdateAsync(versionInfo, sourceInfo);
                    result.TotalChecked++;

                    if (versionInfo.Status == UpdateCheckStatus.UpdateAvailable)
                        result.UpdatesAvailable++;
                    else if (versionInfo.Status == UpdateCheckStatus.UpToDate)
                        result.UpToDate++;
                }
                catch
                {
                    versionInfo.Status = UpdateCheckStatus.CheckFailed;
                    result.CheckFailed++;
                }
            }

            result.Modules.Add(versionInfo);
        }

        // Cache result
        _updateTrackerData!.LastCheckResult = result;
        await SaveUpdateTrackerDataAsync();

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Gets modules with available updates.
    /// </summary>
    public async Task<IReadOnlyList<ModVersionInfo>> GetAvailableUpdatesAsync()
    {
        await EnsureUpdateTrackerLoadedAsync();

        if (_updateTrackerData?.LastCheckResult == null)
        {
            var result = await CheckForUpdatesAsync();
            return result.AvailableUpdates;
        }

        return _updateTrackerData.LastCheckResult.AvailableUpdates;
    }

    /// <summary>
    /// External<br/>
    /// Sets the update source for a module.
    /// </summary>
    public async Task SetModuleSourceAsync(string moduleId, ModUpdateSource source, string? sourceId = null, string? url = null)
    {
        await EnsureUpdateTrackerLoadedAsync();

        var existing = _updateTrackerData!.SourceInfos.FirstOrDefault(s => s.ModuleId == moduleId);
        if (existing != null)
        {
            existing.Source = source;
            existing.SourceId = sourceId;
            existing.Url = url;
        }
        else
        {
            _updateTrackerData.SourceInfos.Add(new ModSourceInfo
            {
                ModuleId = moduleId,
                Source = source,
                SourceId = sourceId,
                Url = url
            });
        }

        await SaveUpdateTrackerDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Gets source info for a module.
    /// </summary>
    public async Task<ModSourceInfo?> GetModuleSourceAsync(string moduleId)
    {
        await EnsureUpdateTrackerLoadedAsync();
        return _updateTrackerData!.SourceInfos.FirstOrDefault(s => s.ModuleId == moduleId);
    }

    /// <summary>
    /// External<br/>
    /// Sets whether to ignore updates for a module.
    /// </summary>
    public async Task SetIgnoreUpdatesAsync(string moduleId, bool ignore)
    {
        await EnsureUpdateTrackerLoadedAsync();

        var existing = _updateTrackerData!.SourceInfos.FirstOrDefault(s => s.ModuleId == moduleId);
        if (existing != null)
        {
            existing.IgnoreUpdates = ignore;
        }
        else
        {
            _updateTrackerData.SourceInfos.Add(new ModSourceInfo
            {
                ModuleId = moduleId,
                IgnoreUpdates = ignore
            });
        }

        await SaveUpdateTrackerDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Gets list of ignored modules.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetIgnoredModulesAsync()
    {
        await EnsureUpdateTrackerLoadedAsync();
        return _updateTrackerData!.SourceInfos
            .Where(s => s.IgnoreUpdates)
            .Select(s => s.ModuleId)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets auto-update settings.
    /// </summary>
    public async Task<AutoUpdateSettings> GetAutoUpdateSettingsAsync()
    {
        await EnsureUpdateTrackerLoadedAsync();
        return _updateTrackerData!.AutoSettings;
    }

    /// <summary>
    /// External<br/>
    /// Sets auto-update settings.
    /// </summary>
    public async Task SetAutoUpdateSettingsAsync(AutoUpdateSettings settings)
    {
        await EnsureUpdateTrackerLoadedAsync();
        _updateTrackerData!.AutoSettings = settings;
        await SaveUpdateTrackerDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Checks if auto-check is due.
    /// </summary>
    public async Task<bool> IsAutoCheckDueAsync()
    {
        await EnsureUpdateTrackerLoadedAsync();
        var settings = _updateTrackerData!.AutoSettings;

        if (!settings.Enabled)
            return false;

        if (settings.LastAutoCheck == null)
            return true;

        var hoursSinceCheck = (DateTime.UtcNow - settings.LastAutoCheck.Value).TotalHours;
        return hoursSinceCheck >= settings.IntervalHours;
    }

    /// <summary>
    /// External<br/>
    /// Performs auto-check if due.
    /// </summary>
    public async Task<UpdateCheckResult?> PerformAutoCheckAsync()
    {
        if (!await IsAutoCheckDueAsync())
            return null;

        var result = await CheckForUpdatesAsync(new UpdateCheckOptions { ForceRefresh = true });

        _updateTrackerData!.AutoSettings.LastAutoCheck = DateTime.UtcNow;
        await SaveUpdateTrackerDataAsync();

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Gets the last check result.
    /// </summary>
    public async Task<UpdateCheckResult?> GetLastCheckResultAsync()
    {
        await EnsureUpdateTrackerLoadedAsync();
        return _updateTrackerData!.LastCheckResult;
    }

    /// <summary>
    /// External<br/>
    /// Clears cached update data.
    /// </summary>
    public async Task ClearUpdateCacheAsync()
    {
        await EnsureUpdateTrackerLoadedAsync();
        _updateTrackerData!.LastCheckResult = null;
        foreach (var source in _updateTrackerData.SourceInfos)
        {
            source.LastChecked = null;
        }
        await SaveUpdateTrackerDataAsync();
    }

    private async Task CheckModuleUpdateAsync(ModVersionInfo versionInfo, ModSourceInfo? sourceInfo)
    {
        var source = sourceInfo?.Source ?? versionInfo.Source;
        versionInfo.Source = source;

        switch (source)
        {
            case ModUpdateSource.NexusMods:
                await CheckNexusModsUpdateAsync(versionInfo, sourceInfo);
                break;
            case ModUpdateSource.GitHub:
                await CheckGitHubUpdateAsync(versionInfo, sourceInfo);
                break;
            case ModUpdateSource.SteamWorkshop:
                // Steam Workshop updates are handled by Steam
                versionInfo.Status = UpdateCheckStatus.UpToDate;
                break;
            default:
                versionInfo.Status = UpdateCheckStatus.NotTracked;
                break;
        }

        // Update last checked time
        if (sourceInfo != null)
        {
            sourceInfo.LastChecked = DateTime.UtcNow;
        }
    }

    private async Task CheckNexusModsUpdateAsync(ModVersionInfo versionInfo, ModSourceInfo? sourceInfo)
    {
        if (string.IsNullOrEmpty(sourceInfo?.SourceId))
        {
            versionInfo.Status = UpdateCheckStatus.NotTracked;
            return;
        }

        try
        {
            // Note: This would require a valid NexusMods API key in production
            var url = $"https://api.nexusmods.com/v1/games/mountandblade2bannerlord/mods/{sourceInfo.SourceId}.json";
            
            // For now, mark as not tracked since we don't have API credentials
            versionInfo.Status = UpdateCheckStatus.NotTracked;
            versionInfo.PageUrl = $"https://www.nexusmods.com/mountandblade2bannerlord/mods/{sourceInfo.SourceId}";
        }
        catch
        {
            versionInfo.Status = UpdateCheckStatus.CheckFailed;
        }

        await Task.CompletedTask;
    }

    private async Task CheckGitHubUpdateAsync(ModVersionInfo versionInfo, ModSourceInfo? sourceInfo)
    {
        if (string.IsNullOrEmpty(sourceInfo?.SourceId))
        {
            versionInfo.Status = UpdateCheckStatus.NotTracked;
            return;
        }

        try
        {
            // sourceId should be in format "owner/repo"
            var url = $"https://api.github.com/repos/{sourceInfo.SourceId}/releases/latest";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Bannerlord-LauncherManager");
            
            var response = await UpdateHttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                versionInfo.Status = UpdateCheckStatus.CheckFailed;
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubReleaseInfo>(json, UpdateJsonOptions);

            if (release != null)
            {
                var latestVersion = release.TagName?.TrimStart('v', 'V') ?? string.Empty;
                versionInfo.LatestVersion = latestVersion;
                versionInfo.Changelog = release.Body;
                versionInfo.ReleaseDate = release.PublishedAt;
                versionInfo.PageUrl = release.HtmlUrl;

                // Find download asset
                var asset = release.Assets?.FirstOrDefault(a => 
                    a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
                if (asset != null)
                {
                    versionInfo.DownloadUrl = asset.BrowserDownloadUrl;
                    versionInfo.FileSize = asset.Size;
                }

                // Compare versions
                versionInfo.Status = CompareVersions(versionInfo.InstalledVersion, latestVersion);
            }
        }
        catch
        {
            versionInfo.Status = UpdateCheckStatus.CheckFailed;
        }
    }

    private static UpdateCheckStatus CompareVersions(string installed, string latest)
    {
        try
        {
            // Clean version strings
            var installedClean = CleanVersionString(installed);
            var latestClean = CleanVersionString(latest);

            if (Version.TryParse(installedClean, out var installedVer) &&
                Version.TryParse(latestClean, out var latestVer))
            {
                var comparison = installedVer.CompareTo(latestVer);
                return comparison switch
                {
                    < 0 => UpdateCheckStatus.UpdateAvailable,
                    > 0 => UpdateCheckStatus.NewerInstalled,
                    _ => UpdateCheckStatus.UpToDate
                };
            }

            // Fall back to string comparison
            return string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase)
                ? UpdateCheckStatus.UpToDate
                : UpdateCheckStatus.UpdateAvailable;
        }
        catch
        {
            return UpdateCheckStatus.Unknown;
        }
    }

    private static string CleanVersionString(string version)
    {
        // Remove common prefixes and suffixes
        version = Regex.Replace(version, @"^[vV]", "");
        version = Regex.Replace(version, @"[-+].*$", "");
        
        // Ensure at least major.minor format
        var parts = version.Split('.');
        if (parts.Length == 1)
            version += ".0";

        return version;
    }

    private static ModUpdateSource DetectModSource(ModuleInfoExtendedWithMetadata module)
    {
        // Try to detect source from module URL or metadata
        var url = module.Url ?? string.Empty;

        if (url.Contains("nexusmods.com", StringComparison.OrdinalIgnoreCase))
            return ModUpdateSource.NexusMods;
        if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            return ModUpdateSource.GitHub;
        if (url.Contains("steamcommunity.com", StringComparison.OrdinalIgnoreCase))
            return ModUpdateSource.SteamWorkshop;

        return ModUpdateSource.Unknown;
    }

    private async Task EnsureUpdateTrackerLoadedAsync()
    {
        if (_updateTrackerData != null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, UpdateTrackerFile);

        if (File.Exists(dataPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(dataPath);
                _updateTrackerData = JsonSerializer.Deserialize<UpdateTrackerData>(json, UpdateJsonOptions);
            }
            catch
            {
                _updateTrackerData = null;
            }
        }

        _updateTrackerData ??= new UpdateTrackerData();
    }

    private async Task SaveUpdateTrackerDataAsync()
    {
        if (_updateTrackerData == null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, UpdateTrackerFile);

        var json = JsonSerializer.Serialize(_updateTrackerData, UpdateJsonOptions);
        await File.WriteAllTextAsync(dataPath, json);
    }
}
