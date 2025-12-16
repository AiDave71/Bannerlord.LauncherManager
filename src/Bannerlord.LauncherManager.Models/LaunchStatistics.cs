using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Outcome of a game session.
/// </summary>
public enum SessionOutcome
{
    Unknown,
    Normal,
    Crash,
    ForceQuit,
    Error
}

/// <summary>
/// Record of a single game launch.
/// </summary>
public class LaunchRecord
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the game was launched.
    /// </summary>
    public DateTime LaunchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Duration of the session.
    /// </summary>
    public TimeSpan Duration => EndedAt.HasValue ? EndedAt.Value - LaunchedAt : TimeSpan.Zero;

    /// <summary>
    /// Game version.
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// Profile used (if any).
    /// </summary>
    public string? ProfileId { get; set; }

    /// <summary>
    /// Profile name.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Game mode.
    /// </summary>
    public string? GameMode { get; set; }

    /// <summary>
    /// Enabled modules during this launch.
    /// </summary>
    public List<string> EnabledModules { get; set; } = new();

    /// <summary>
    /// Module count.
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// How the session ended.
    /// </summary>
    public SessionOutcome Outcome { get; set; } = SessionOutcome.Unknown;

    /// <summary>
    /// Exit code if available.
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Error message if crashed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Aggregated statistics.
/// </summary>
public class LaunchStatsSummary
{
    /// <summary>
    /// Total launches.
    /// </summary>
    public int TotalLaunches { get; set; }

    /// <summary>
    /// Total play time.
    /// </summary>
    public TimeSpan TotalPlayTime { get; set; }

    /// <summary>
    /// Average session duration.
    /// </summary>
    public TimeSpan AverageSessionDuration { get; set; }

    /// <summary>
    /// Longest session.
    /// </summary>
    public TimeSpan LongestSession { get; set; }

    /// <summary>
    /// Total crashes.
    /// </summary>
    public int TotalCrashes { get; set; }

    /// <summary>
    /// Crash rate percentage.
    /// </summary>
    public double CrashRate { get; set; }

    /// <summary>
    /// First launch date.
    /// </summary>
    public DateTime? FirstLaunch { get; set; }

    /// <summary>
    /// Last launch date.
    /// </summary>
    public DateTime? LastLaunch { get; set; }

    /// <summary>
    /// Most used profile.
    /// </summary>
    public string? MostUsedProfile { get; set; }

    /// <summary>
    /// Launches per day (last 30 days).
    /// </summary>
    public double LaunchesPerDay { get; set; }
}

/// <summary>
/// Statistics for a specific module.
/// </summary>
public class ModuleUsageStats
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
    /// Times this module was used in launches.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Total play time with this module.
    /// </summary>
    public TimeSpan TotalPlayTime { get; set; }

    /// <summary>
    /// Crashes with this module enabled.
    /// </summary>
    public int CrashCount { get; set; }

    /// <summary>
    /// Crash rate when this module is enabled.
    /// </summary>
    public double CrashRate { get; set; }

    /// <summary>
    /// Last used date.
    /// </summary>
    public DateTime? LastUsed { get; set; }
}

/// <summary>
/// Crash correlation data.
/// </summary>
public class CrashCorrelation
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
    /// Total crashes with this module.
    /// </summary>
    public int CrashCount { get; set; }

    /// <summary>
    /// Total launches with this module.
    /// </summary>
    public int TotalLaunches { get; set; }

    /// <summary>
    /// Crash rate for this module.
    /// </summary>
    public double CrashRate { get; set; }

    /// <summary>
    /// Crash rate compared to average.
    /// </summary>
    public double RelativeCrashRate { get; set; }

    /// <summary>
    /// Confidence level of correlation.
    /// </summary>
    public string Confidence { get; set; } = "Low";
}

/// <summary>
/// Collection of all statistics data.
/// </summary>
public class StatisticsData
{
    /// <summary>
    /// All launch records.
    /// </summary>
    public List<LaunchRecord> Records { get; set; } = new();

    /// <summary>
    /// Version for migration.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Time range for statistics queries.
/// </summary>
public class StatisticsTimeRange
{
    /// <summary>
    /// Start date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Last N days.
    /// </summary>
    public int? LastDays { get; set; }

    public static StatisticsTimeRange AllTime => new();
    public static StatisticsTimeRange Last7Days => new() { LastDays = 7 };
    public static StatisticsTimeRange Last30Days => new() { LastDays = 30 };
    public static StatisticsTimeRange Last90Days => new() { LastDays = 90 };
}
