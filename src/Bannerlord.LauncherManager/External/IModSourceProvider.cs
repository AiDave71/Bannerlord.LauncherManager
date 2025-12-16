using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager.External;

/// <summary>
/// Interface for mod source providers (NexusMods, Steam Workshop, etc.)
/// </summary>
public interface IModSourceProvider
{
    /// <summary>
    /// The source this provider handles.
    /// </summary>
    ModSource Source { get; }

    /// <summary>
    /// Whether this provider is configured and ready to use.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Configures the provider with the given settings.
    /// </summary>
    Task<bool> ConfigureAsync(ModSourceConfig config);

    /// <summary>
    /// Checks if updates are available for the given modules.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(
        IReadOnlyList<ModuleInfoWithSource> modules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets download information for a specific mod.
    /// </summary>
    Task<ModUpdateInfo?> GetModInfoAsync(
        string moduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a mod file.
    /// </summary>
    Task<DownloadResult> DownloadModAsync(
        ModUpdateInfo modInfo,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for mods by name or keyword.
    /// </summary>
    Task<IReadOnlyList<ModSearchResult>> SearchModsAsync(
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the API key or authentication.
    /// </summary>
    Task<bool> ValidateAuthenticationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Module info with source tracking.
/// </summary>
public class ModuleInfoWithSource
{
    public string ModuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Url { get; set; }
    public ModSource? KnownSource { get; set; }
    public int? NexusModId { get; set; }
    public ulong? SteamWorkshopId { get; set; }
}

/// <summary>
/// Progress information for downloads.
/// </summary>
public class DownloadProgress
{
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double Percentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
    public long SpeedBytesPerSecond { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public DownloadStatus Status { get; set; }
}

/// <summary>
/// Result of a mod search.
/// </summary>
public class ModSearchResult
{
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Author { get; set; }
    public string? Version { get; set; }
    public ModSource Source { get; set; }
    public string? DownloadUrl { get; set; }
    public string? PageUrl { get; set; }
    public int? NexusModId { get; set; }
    public ulong? SteamWorkshopId { get; set; }
    public long? DownloadCount { get; set; }
    public long? EndorsementCount { get; set; }
    public DateTime? LastUpdated { get; set; }
}
