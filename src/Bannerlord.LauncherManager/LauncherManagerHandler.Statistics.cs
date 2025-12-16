using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private const string StatisticsDataFile = "launch_statistics.json";
    private StatisticsData? _statisticsData;
    private LaunchRecord? _currentSession;

    private static readonly JsonSerializerOptions StatsJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Records a game launch.
    /// </summary>
    public async Task<LaunchRecord> RecordLaunchAsync()
    {
        await EnsureStatisticsLoadedAsync();

        var viewModels = await GetModuleViewModelsAsync();
        var enabledModules = viewModels?.Where(vm => vm.IsSelected)
            .Select(vm => vm.ModuleInfoExtended.Id)
            .ToList() ?? new List<string>();

        _currentSession = new LaunchRecord
        {
            GameVersion = await GetGameVersionAsync(),
            GameMode = GetGameMode().ToString(),
            EnabledModules = enabledModules,
            ModuleCount = enabledModules.Count
        };

        _statisticsData!.Records.Add(_currentSession);
        await SaveStatisticsDataAsync();

        return _currentSession;
    }

    /// <summary>
    /// External<br/>
    /// Records the end of a game session.
    /// </summary>
    public async Task RecordSessionEndAsync(SessionOutcome outcome, int? exitCode = null, string? errorMessage = null)
    {
        if (_currentSession == null)
            return;

        _currentSession.EndedAt = DateTime.UtcNow;
        _currentSession.Outcome = outcome;
        _currentSession.ExitCode = exitCode;
        _currentSession.ErrorMessage = errorMessage;

        await SaveStatisticsDataAsync();
        _currentSession = null;
    }

    /// <summary>
    /// External<br/>
    /// Gets launch history.
    /// </summary>
    public async Task<IReadOnlyList<LaunchRecord>> GetLaunchHistoryAsync(StatisticsTimeRange? range = null)
    {
        await EnsureStatisticsLoadedAsync();

        var records = _statisticsData!.Records.AsEnumerable();

        if (range != null)
        {
            records = ApplyTimeRange(records, range);
        }

        return records.OrderByDescending(r => r.LaunchedAt).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets aggregated statistics.
    /// </summary>
    public async Task<LaunchStatsSummary> GetStatsSummaryAsync(StatisticsTimeRange? range = null)
    {
        await EnsureStatisticsLoadedAsync();

        var records = _statisticsData!.Records.AsEnumerable();

        if (range != null)
        {
            records = ApplyTimeRange(records, range);
        }

        var recordList = records.ToList();
        var summary = new LaunchStatsSummary
        {
            TotalLaunches = recordList.Count
        };

        if (recordList.Count == 0)
            return summary;

        var completedSessions = recordList.Where(r => r.EndedAt.HasValue).ToList();

        summary.TotalPlayTime = TimeSpan.FromTicks(completedSessions.Sum(r => r.Duration.Ticks));
        summary.AverageSessionDuration = completedSessions.Count > 0
            ? TimeSpan.FromTicks(summary.TotalPlayTime.Ticks / completedSessions.Count)
            : TimeSpan.Zero;
        summary.LongestSession = completedSessions.Count > 0
            ? completedSessions.Max(r => r.Duration)
            : TimeSpan.Zero;

        summary.TotalCrashes = recordList.Count(r => r.Outcome == SessionOutcome.Crash);
        summary.CrashRate = recordList.Count > 0 ? (double)summary.TotalCrashes / recordList.Count * 100 : 0;

        summary.FirstLaunch = recordList.Min(r => r.LaunchedAt);
        summary.LastLaunch = recordList.Max(r => r.LaunchedAt);

        // Most used profile
        var profileGroups = recordList
            .Where(r => !string.IsNullOrEmpty(r.ProfileId))
            .GroupBy(r => r.ProfileId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        summary.MostUsedProfile = profileGroups?.First().ProfileName;

        // Launches per day (last 30 days)
        var last30Days = recordList.Where(r => r.LaunchedAt >= DateTime.UtcNow.AddDays(-30)).Count();
        summary.LaunchesPerDay = last30Days / 30.0;

        return summary;
    }

    /// <summary>
    /// External<br/>
    /// Gets total play time.
    /// </summary>
    public async Task<TimeSpan> GetTotalPlayTimeAsync(StatisticsTimeRange? range = null)
    {
        var summary = await GetStatsSummaryAsync(range);
        return summary.TotalPlayTime;
    }

    /// <summary>
    /// External<br/>
    /// Gets module usage statistics.
    /// </summary>
    public async Task<IReadOnlyList<ModuleUsageStats>> GetModuleUsageStatsAsync(StatisticsTimeRange? range = null)
    {
        await EnsureStatisticsLoadedAsync();

        var records = _statisticsData!.Records.AsEnumerable();

        if (range != null)
        {
            records = ApplyTimeRange(records, range);
        }

        var recordList = records.ToList();
        var modules = await GetModulesAsync();
        var moduleDict = modules.ToDictionary(m => m.Id, m => m.Name);

        var stats = new Dictionary<string, ModuleUsageStats>();

        foreach (var record in recordList)
        {
            foreach (var moduleId in record.EnabledModules)
            {
                if (!stats.TryGetValue(moduleId, out var stat))
                {
                    stat = new ModuleUsageStats
                    {
                        ModuleId = moduleId,
                        ModuleName = moduleDict.TryGetValue(moduleId, out var name) ? name : moduleId
                    };
                    stats[moduleId] = stat;
                }

                stat.UsageCount++;
                stat.TotalPlayTime += record.Duration;

                if (record.Outcome == SessionOutcome.Crash)
                    stat.CrashCount++;

                if (!stat.LastUsed.HasValue || record.LaunchedAt > stat.LastUsed)
                    stat.LastUsed = record.LaunchedAt;
            }
        }

        // Calculate crash rates
        foreach (var stat in stats.Values)
        {
            stat.CrashRate = stat.UsageCount > 0 ? (double)stat.CrashCount / stat.UsageCount * 100 : 0;
        }

        return stats.Values.OrderByDescending(s => s.UsageCount).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets crash correlation data.
    /// </summary>
    public async Task<IReadOnlyList<CrashCorrelation>> GetCrashCorrelationsAsync()
    {
        await EnsureStatisticsLoadedAsync();

        var records = _statisticsData!.Records;
        var modules = await GetModulesAsync();
        var moduleDict = modules.ToDictionary(m => m.Id, m => m.Name);

        var overallCrashRate = records.Count > 0
            ? (double)records.Count(r => r.Outcome == SessionOutcome.Crash) / records.Count
            : 0;

        var correlations = new Dictionary<string, CrashCorrelation>();

        foreach (var record in records)
        {
            foreach (var moduleId in record.EnabledModules)
            {
                if (!correlations.TryGetValue(moduleId, out var corr))
                {
                    corr = new CrashCorrelation
                    {
                        ModuleId = moduleId,
                        ModuleName = moduleDict.TryGetValue(moduleId, out var name) ? name : moduleId
                    };
                    correlations[moduleId] = corr;
                }

                corr.TotalLaunches++;
                if (record.Outcome == SessionOutcome.Crash)
                    corr.CrashCount++;
            }
        }

        // Calculate rates and confidence
        foreach (var corr in correlations.Values)
        {
            corr.CrashRate = corr.TotalLaunches > 0 ? (double)corr.CrashCount / corr.TotalLaunches : 0;
            corr.RelativeCrashRate = overallCrashRate > 0 ? corr.CrashRate / overallCrashRate : 0;

            corr.Confidence = corr.TotalLaunches switch
            {
                >= 20 => "High",
                >= 10 => "Medium",
                >= 5 => "Low",
                _ => "Very Low"
            };
        }

        return correlations.Values
            .Where(c => c.CrashCount > 0)
            .OrderByDescending(c => c.RelativeCrashRate)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets recent launches.
    /// </summary>
    public async Task<IReadOnlyList<LaunchRecord>> GetRecentLaunchesAsync(int count = 10)
    {
        await EnsureStatisticsLoadedAsync();

        return _statisticsData!.Records
            .OrderByDescending(r => r.LaunchedAt)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets launches for a specific profile.
    /// </summary>
    public async Task<IReadOnlyList<LaunchRecord>> GetLaunchesForProfileAsync(string profileId)
    {
        await EnsureStatisticsLoadedAsync();

        return _statisticsData!.Records
            .Where(r => r.ProfileId == profileId)
            .OrderByDescending(r => r.LaunchedAt)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets play time for a profile.
    /// </summary>
    public async Task<TimeSpan> GetPlayTimeForProfileAsync(string profileId)
    {
        var launches = await GetLaunchesForProfileAsync(profileId);
        return TimeSpan.FromTicks(launches.Sum(l => l.Duration.Ticks));
    }

    /// <summary>
    /// External<br/>
    /// Clears all statistics.
    /// </summary>
    public async Task ClearStatisticsAsync()
    {
        _statisticsData = new StatisticsData();
        await SaveStatisticsDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Exports statistics to JSON.
    /// </summary>
    public async Task<string> ExportStatisticsAsync()
    {
        await EnsureStatisticsLoadedAsync();
        return JsonSerializer.Serialize(_statisticsData, StatsJsonOptions);
    }

    /// <summary>
    /// External<br/>
    /// Gets a daily breakdown of launches.
    /// </summary>
    public async Task<Dictionary<string, int>> GetDailyLaunchCountsAsync(int days = 30)
    {
        await EnsureStatisticsLoadedAsync();

        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
        var counts = new Dictionary<string, int>();

        // Initialize all days
        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i).ToString("yyyy-MM-dd");
            counts[date] = 0;
        }

        // Count launches per day
        foreach (var record in _statisticsData!.Records.Where(r => r.LaunchedAt >= startDate))
        {
            var date = record.LaunchedAt.Date.ToString("yyyy-MM-dd");
            if (counts.ContainsKey(date))
                counts[date]++;
        }

        return counts;
    }

    private async Task EnsureStatisticsLoadedAsync()
    {
        if (_statisticsData != null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, StatisticsDataFile);

        if (File.Exists(dataPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(dataPath);
                _statisticsData = JsonSerializer.Deserialize<StatisticsData>(json, StatsJsonOptions);
            }
            catch
            {
                _statisticsData = null;
            }
        }

        _statisticsData ??= new StatisticsData();
    }

    private async Task SaveStatisticsDataAsync()
    {
        if (_statisticsData == null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, StatisticsDataFile);

        var json = JsonSerializer.Serialize(_statisticsData, StatsJsonOptions);
        await File.WriteAllTextAsync(dataPath, json);
    }

    private static IEnumerable<LaunchRecord> ApplyTimeRange(IEnumerable<LaunchRecord> records, StatisticsTimeRange range)
    {
        if (range.LastDays.HasValue)
        {
            var startDate = DateTime.UtcNow.AddDays(-range.LastDays.Value);
            records = records.Where(r => r.LaunchedAt >= startDate);
        }
        else
        {
            if (range.StartDate.HasValue)
                records = records.Where(r => r.LaunchedAt >= range.StartDate.Value);
            if (range.EndDate.HasValue)
                records = records.Where(r => r.LaunchedAt <= range.EndDate.Value);
        }

        return records;
    }
}
