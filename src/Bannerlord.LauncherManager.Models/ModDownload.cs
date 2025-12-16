using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Status of a download operation.
/// </summary>
public enum DownloadStatus
{
    Pending,
    Downloading,
    Paused,
    Extracting,
    Installing,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Source from which a mod can be downloaded.
/// </summary>
public enum ModSource
{
    NexusMods,
    SteamWorkshop,
    GitHub,
    Manual,
    Unknown
}

/// <summary>
/// Represents information about an available mod update.
/// </summary>
public class ModUpdateInfo
{
    /// <summary>
    /// Module ID of the mod.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the mod.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Currently installed version.
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// Latest available version.
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// Source where the update is available.
    /// </summary>
    public ModSource Source { get; set; }

    /// <summary>
    /// URL to download the update.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Size of the update in bytes.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Changelog or release notes.
    /// </summary>
    public string? Changelog { get; set; }

    /// <summary>
    /// When the update was released.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Whether this is a major update that may break saves.
    /// </summary>
    public bool IsMajorUpdate { get; set; }

    /// <summary>
    /// NexusMods specific: Mod ID.
    /// </summary>
    public int? NexusModId { get; set; }

    /// <summary>
    /// NexusMods specific: File ID.
    /// </summary>
    public int? NexusFileId { get; set; }

    /// <summary>
    /// Steam Workshop specific: Workshop item ID.
    /// </summary>
    public ulong? SteamWorkshopId { get; set; }
}

/// <summary>
/// Represents a download task.
/// </summary>
public class DownloadTask
{
    /// <summary>
    /// Unique identifier for this download.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Module ID being downloaded.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version being downloaded.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Source of the download.
    /// </summary>
    public ModSource Source { get; set; }

    /// <summary>
    /// URL to download from.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Local path where the file is being downloaded.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Current status of the download.
    /// </summary>
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    /// <summary>
    /// Total size in bytes.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Bytes downloaded so far.
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double Progress => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;

    /// <summary>
    /// Download speed in bytes per second.
    /// </summary>
    public long SpeedBytesPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// When the download started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the download completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this is an update to an existing mod.
    /// </summary>
    public bool IsUpdate { get; set; }
}

/// <summary>
/// Result of checking for updates.
/// </summary>
public class UpdateCheckResult
{
    /// <summary>
    /// Whether the check was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of available updates.
    /// </summary>
    public List<ModUpdateInfo> AvailableUpdates { get; set; } = new();

    /// <summary>
    /// Number of mods that are up to date.
    /// </summary>
    public int UpToDateCount { get; set; }

    /// <summary>
    /// Number of mods that couldn't be checked.
    /// </summary>
    public int UncheckableCount { get; set; }

    /// <summary>
    /// When the check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of a download operation.
/// </summary>
public class DownloadResult
{
    /// <summary>
    /// Whether the download was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The download task.
    /// </summary>
    public DownloadTask? Task { get; set; }

    /// <summary>
    /// Path to the downloaded file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Whether installation was also completed.
    /// </summary>
    public bool Installed { get; set; }

    public static DownloadResult AsSuccess(DownloadTask task, string filePath, bool installed = false) =>
        new() { Success = true, Task = task, FilePath = filePath, Installed = installed };

    public static DownloadResult AsError(string message, DownloadTask? task = null) =>
        new() { Success = false, ErrorMessage = message, Task = task };
}

/// <summary>
/// Configuration for a mod source provider.
/// </summary>
public class ModSourceConfig
{
    /// <summary>
    /// The mod source this config is for.
    /// </summary>
    public ModSource Source { get; set; }

    /// <summary>
    /// Whether this source is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// API key for the source (e.g., NexusMods API key).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for API requests.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Rate limit (requests per minute).
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 30;
}
