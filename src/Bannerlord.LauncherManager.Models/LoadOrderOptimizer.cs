using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Type of optimization suggestion.
/// </summary>
public enum OptimizationSuggestionType
{
    MoveUp,
    MoveDown,
    MoveBefore,
    MoveAfter,
    Enable,
    Disable,
    ResolveConflict
}

/// <summary>
/// Confidence level of the suggestion.
/// </summary>
public enum SuggestionConfidence
{
    Low,
    Medium,
    High,
    Required
}

/// <summary>
/// Reason for the suggestion.
/// </summary>
public enum SuggestionReason
{
    DependencyOrder,
    LoadBeforeDependency,
    LoadAfterDependency,
    ConflictResolution,
    CommonPattern,
    PerformanceOptimization,
    CompatibilityFix
}

/// <summary>
/// A single optimization suggestion.
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// Unique ID for this suggestion.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Module this suggestion applies to.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Type of suggestion.
    /// </summary>
    public OptimizationSuggestionType Type { get; set; }

    /// <summary>
    /// Target module ID (for MoveBefore/MoveAfter).
    /// </summary>
    public string? TargetModuleId { get; set; }

    /// <summary>
    /// Current position.
    /// </summary>
    public int CurrentPosition { get; set; }

    /// <summary>
    /// Suggested new position.
    /// </summary>
    public int SuggestedPosition { get; set; }

    /// <summary>
    /// Confidence level.
    /// </summary>
    public SuggestionConfidence Confidence { get; set; }

    /// <summary>
    /// Reason for suggestion.
    /// </summary>
    public SuggestionReason Reason { get; set; }

    /// <summary>
    /// Human-readable explanation.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Priority (lower = more important).
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Result of optimization analysis.
/// </summary>
public class OptimizationResult
{
    /// <summary>
    /// Whether the current order has issues.
    /// </summary>
    public bool HasIssues { get; set; }

    /// <summary>
    /// Number of critical issues.
    /// </summary>
    public int CriticalIssues { get; set; }

    /// <summary>
    /// Number of warnings.
    /// </summary>
    public int Warnings { get; set; }

    /// <summary>
    /// Overall health score (0-100).
    /// </summary>
    public int HealthScore { get; set; } = 100;

    /// <summary>
    /// List of suggestions.
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Optimized load order if auto-fix was requested.
    /// </summary>
    public List<string>? OptimizedOrder { get; set; }

    /// <summary>
    /// Summary message.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Options for optimization.
/// </summary>
public class OptimizationOptions
{
    /// <summary>
    /// Only check enabled modules.
    /// </summary>
    public bool EnabledOnly { get; set; } = true;

    /// <summary>
    /// Include native modules in analysis.
    /// </summary>
    public bool IncludeNative { get; set; }

    /// <summary>
    /// Generate optimized order automatically.
    /// </summary>
    public bool GenerateOptimizedOrder { get; set; } = true;

    /// <summary>
    /// Consider community patterns.
    /// </summary>
    public bool UseCommunityPatterns { get; set; } = true;

    /// <summary>
    /// Strictness level (1-3).
    /// </summary>
    public int StrictnessLevel { get; set; } = 2;
}

/// <summary>
/// Snapshot of load order state.
/// </summary>
public class LoadOrderSnapshot
{
    /// <summary>
    /// Snapshot ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// When snapshot was taken.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Module IDs in order.
    /// </summary>
    public List<string> ModuleOrder { get; set; } = new();

    /// <summary>
    /// Whether each module was enabled.
    /// </summary>
    public Dictionary<string, bool> EnabledState { get; set; } = new();
}

/// <summary>
/// History of load order changes.
/// </summary>
public class LoadOrderHistory
{
    /// <summary>
    /// Maximum snapshots to keep.
    /// </summary>
    public int MaxSnapshots { get; set; } = 20;

    /// <summary>
    /// Snapshots in chronological order.
    /// </summary>
    public List<LoadOrderSnapshot> Snapshots { get; set; } = new();

    /// <summary>
    /// Version for migration.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Comparison between two load orders.
/// </summary>
public class LoadOrderComparison
{
    /// <summary>
    /// Modules added.
    /// </summary>
    public List<string> Added { get; set; } = new();

    /// <summary>
    /// Modules removed.
    /// </summary>
    public List<string> Removed { get; set; } = new();

    /// <summary>
    /// Modules that changed position.
    /// </summary>
    public List<LoadOrderPositionChange> PositionChanges { get; set; } = new();

    /// <summary>
    /// Modules that changed enabled state.
    /// </summary>
    public List<LoadOrderStateChange> StateChanges { get; set; } = new();
}

public class LoadOrderPositionChange
{
    public string ModuleId { get; set; } = string.Empty;
    public int OldPosition { get; set; }
    public int NewPosition { get; set; }
}

public class LoadOrderStateChange
{
    public string ModuleId { get; set; } = string.Empty;
    public bool WasEnabled { get; set; }
    public bool IsEnabled { get; set; }
}
