using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Source of mod updates.
/// </summary>
public enum ModUpdateSource
{
    Unknown,
    NexusMods,
    SteamWorkshop,
    GitHub,
    ModDB,
    Manual
}

/// <summary>
/// Status of an update check.
/// </summary>
public enum UpdateCheckStatus
{
    Unknown,
    UpToDate,
    UpdateAvailable,
    NewerInstalled,
    NotTracked,
    CheckFailed
}

/// <summary>
/// Information about a mod's update source.
/// </summary>
public class ModSourceInfo
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Update source.
    /// </summary>
    public ModUpdateSource Source { get; set; } = ModUpdateSource.Unknown;

    /// <summary>
    /// Source-specific ID (e.g., NexusMods mod ID, Steam Workshop ID).
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// URL to the mod page.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Last checked timestamp.
    /// </summary>
    public DateTime? LastChecked { get; set; }

    /// <summary>
    /// Whether to ignore updates for this mod.
    /// </summary>
    public bool IgnoreUpdates { get; set; }
}

/// <summary>
/// Version information for a mod.
/// </summary>
public class ModVersionInfo
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Currently installed version.
    /// </summary>
    public string InstalledVersion { get; set; } = string.Empty;

    /// <summary>
    /// Latest available version.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Update source.
    /// </summary>
    public ModUpdateSource Source { get; set; }

    /// <summary>
    /// Update check status.
    /// </summary>
    public UpdateCheckStatus Status { get; set; } = UpdateCheckStatus.Unknown;

    /// <summary>
    /// URL to download the update.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// URL to the mod page.
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>
    /// Changelog or release notes.
    /// </summary>
    public string? Changelog { get; set; }

    /// <summary>
    /// Release date of latest version.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// File size of the update.
    /// </summary>
    public long? FileSize { get; set; }
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
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total modules checked.
    /// </summary>
    public int TotalChecked { get; set; }

    /// <summary>
    /// Modules with updates available.
    /// </summary>
    public int UpdatesAvailable { get; set; }

    /// <summary>
    /// Modules that are up to date.
    /// </summary>
    public int UpToDate { get; set; }

    /// <summary>
    /// Modules that failed to check.
    /// </summary>
    public int CheckFailed { get; set; }

    /// <summary>
    /// Version info for all checked modules.
    /// </summary>
    public List<ModVersionInfo> Modules { get; set; } = new();

    /// <summary>
    /// Only modules with updates available.
    /// </summary>
    public List<ModVersionInfo> AvailableUpdates => 
        Modules.FindAll(m => m.Status == UpdateCheckStatus.UpdateAvailable);
}

/// <summary>
/// Options for update checking.
/// </summary>
public class UpdateCheckOptions
{
    /// <summary>
    /// Specific module IDs to check. Empty = all modules.
    /// </summary>
    public List<string>? ModuleIds { get; set; }

    /// <summary>
    /// Include modules marked as ignored.
    /// </summary>
    public bool IncludeIgnored { get; set; }

    /// <summary>
    /// Force refresh even if recently checked.
    /// </summary>
    public bool ForceRefresh { get; set; }

    /// <summary>
    /// Timeout for each source check (seconds).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Settings for automatic update checking.
/// </summary>
public class AutoUpdateSettings
{
    /// <summary>
    /// Whether auto-check is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Check interval in hours.
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Show notification for updates.
    /// </summary>
    public bool ShowNotification { get; set; } = true;

    /// <summary>
    /// Auto-download updates (not install).
    /// </summary>
    public bool AutoDownload { get; set; }

    /// <summary>
    /// Last auto-check timestamp.
    /// </summary>
    public DateTime? LastAutoCheck { get; set; }
}

/// <summary>
/// Stored update tracking data.
/// </summary>
public class UpdateTrackerData
{
    /// <summary>
    /// Source info for all tracked mods.
    /// </summary>
    public List<ModSourceInfo> SourceInfos { get; set; } = new();

    /// <summary>
    /// Auto-update settings.
    /// </summary>
    public AutoUpdateSettings AutoSettings { get; set; } = new();

    /// <summary>
    /// Cache of last check results.
    /// </summary>
    public UpdateCheckResult? LastCheckResult { get; set; }

    /// <summary>
    /// Version for migration.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// NexusMods API response structures.
/// </summary>
public class NexusModInfo
{
    public int ModId { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public string? Summary { get; set; }
}

/// <summary>
/// GitHub release information.
/// </summary>
public class GitHubReleaseInfo
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? HtmlUrl { get; set; }
    public List<GitHubAsset>? Assets { get; set; }
}

public class GitHubAsset
{
    public string? Name { get; set; }
    public string? BrowserDownloadUrl { get; set; }
    public long Size { get; set; }
}
