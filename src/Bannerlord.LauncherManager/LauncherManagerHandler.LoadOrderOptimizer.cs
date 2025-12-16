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
    private const string LoadOrderHistoryFile = "load_order_history.json";
    private LoadOrderHistory? _loadOrderHistory;

    private static readonly JsonSerializerOptions OptimizerJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Analyzes the current load order and provides optimization suggestions.
    /// </summary>
    public async Task<OptimizationResult> AnalyzeLoadOrderAsync(OptimizationOptions? options = null)
    {
        options ??= new OptimizationOptions();
        var result = new OptimizationResult();

        var modules = await GetModulesAsync();
        var viewModels = await GetModuleViewModelsAsync();
        
        if (viewModels == null || !viewModels.Any())
        {
            result.Summary = "No modules loaded.";
            return result;
        }

        var enabledModules = viewModels
            .Where(vm => !options.EnabledOnly || vm.IsSelected)
            .Where(vm => options.IncludeNative || !vm.ModuleInfoExtended.IsNative())
            .ToList();

        var moduleDict = modules.ToDictionary(m => m.Id, m => m);
        var positionDict = enabledModules
            .Select((vm, i) => (vm, i))
            .ToDictionary(x => x.vm.ModuleInfoExtended.Id, x => x.i);

        // Check dependency order
        foreach (var vm in enabledModules)
        {
            var module = vm.ModuleInfoExtended;
            var currentPos = positionDict[module.Id];

            // Check DependedModules - these must come BEFORE
            foreach (var dep in module.DependentModules ?? Enumerable.Empty<DependentModule>())
            {
                if (!positionDict.TryGetValue(dep.Id, out var depPos))
                    continue;

                if (depPos > currentPos)
                {
                    result.Suggestions.Add(new OptimizationSuggestion
                    {
                        ModuleId = module.Id,
                        ModuleName = module.Name,
                        Type = OptimizationSuggestionType.MoveAfter,
                        TargetModuleId = dep.Id,
                        CurrentPosition = currentPos,
                        SuggestedPosition = depPos + 1,
                        Confidence = SuggestionConfidence.Required,
                        Reason = SuggestionReason.DependencyOrder,
                        Explanation = $"'{module.Name}' depends on '{dep.Id}' which is currently loaded after it.",
                        Priority = 1
                    });
                    result.CriticalIssues++;
                }
            }

            // Check LoadAfterModules
            foreach (var loadAfter in module.DependentModuleMetadatas?
                .Where(m => m.LoadType == LoadType.LoadAfterThis)
                .Select(m => m.Id) ?? Enumerable.Empty<string>())
            {
                if (!positionDict.TryGetValue(loadAfter, out var laPos))
                    continue;

                if (laPos > currentPos)
                {
                    result.Suggestions.Add(new OptimizationSuggestion
                    {
                        ModuleId = module.Id,
                        ModuleName = module.Name,
                        Type = OptimizationSuggestionType.MoveAfter,
                        TargetModuleId = loadAfter,
                        CurrentPosition = currentPos,
                        SuggestedPosition = laPos + 1,
                        Confidence = SuggestionConfidence.High,
                        Reason = SuggestionReason.LoadAfterDependency,
                        Explanation = $"'{module.Name}' should load after '{loadAfter}'.",
                        Priority = 2
                    });
                    result.Warnings++;
                }
            }

            // Check LoadBeforeModules
            foreach (var loadBefore in module.DependentModuleMetadatas?
                .Where(m => m.LoadType == LoadType.LoadBeforeThis)
                .Select(m => m.Id) ?? Enumerable.Empty<string>())
            {
                if (!positionDict.TryGetValue(loadBefore, out var lbPos))
                    continue;

                if (lbPos < currentPos)
                {
                    result.Suggestions.Add(new OptimizationSuggestion
                    {
                        ModuleId = module.Id,
                        ModuleName = module.Name,
                        Type = OptimizationSuggestionType.MoveBefore,
                        TargetModuleId = loadBefore,
                        CurrentPosition = currentPos,
                        SuggestedPosition = lbPos,
                        Confidence = SuggestionConfidence.High,
                        Reason = SuggestionReason.LoadBeforeDependency,
                        Explanation = $"'{module.Name}' should load before '{loadBefore}'.",
                        Priority = 2
                    });
                    result.Warnings++;
                }
            }
        }

        // Calculate health score
        result.HasIssues = result.CriticalIssues > 0 || result.Warnings > 0;
        result.HealthScore = Math.Max(0, 100 - (result.CriticalIssues * 20) - (result.Warnings * 5));

        // Generate optimized order if requested
        if (options.GenerateOptimizedOrder)
        {
            result.OptimizedOrder = await GenerateOptimizedOrderAsync(enabledModules, moduleDict);
        }

        // Generate summary
        result.Summary = result.HasIssues
            ? $"Found {result.CriticalIssues} critical issues and {result.Warnings} warnings. Health score: {result.HealthScore}/100."
            : "Load order is optimal. No issues found.";

        // Sort suggestions by priority
        result.Suggestions = result.Suggestions.OrderBy(s => s.Priority).ToList();

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Applies the optimized load order.
    /// </summary>
    public async Task<bool> ApplyOptimizedOrderAsync()
    {
        var result = await AnalyzeLoadOrderAsync(new OptimizationOptions { GenerateOptimizedOrder = true });
        
        if (result.OptimizedOrder == null || !result.OptimizedOrder.Any())
            return false;

        try
        {
            // Save snapshot before applying
            await SaveSnapshotAsync("Before optimization");

            var modules = await GetModulesAsync();
            var moduleDict = modules.ToDictionary(m => m.Id, m => m);

            var loadOrder = new LoadOrder();
            for (var i = 0; i < result.OptimizedOrder.Count; i++)
            {
                var moduleId = result.OptimizedOrder[i];
                if (!moduleDict.TryGetValue(moduleId, out var module))
                    continue;

                loadOrder[moduleId] = new LoadOrderEntry
                {
                    Id = moduleId,
                    Name = module.Name,
                    IsSelected = true,
                    IsDisabled = false,
                    Index = i
                };
            }

            await SetGameParameterLoadOrderAsync(loadOrder);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Applies a specific suggestion.
    /// </summary>
    public async Task<bool> ApplySuggestionAsync(string suggestionId)
    {
        var result = await AnalyzeLoadOrderAsync();
        var suggestion = result.Suggestions.FirstOrDefault(s => s.Id == suggestionId);
        
        if (suggestion == null)
            return false;

        var viewModels = await GetModuleViewModelsAsync();
        if (viewModels == null)
            return false;

        var vm = viewModels.FirstOrDefault(v => v.ModuleInfoExtended.Id == suggestion.ModuleId);
        if (vm == null)
            return false;

        // Save snapshot before applying
        await SaveSnapshotAsync($"Before applying suggestion for {suggestion.ModuleName}");

        return await SortHelperChangeModulePositionAsync(vm, suggestion.SuggestedPosition);
    }

    /// <summary>
    /// External<br/>
    /// Saves a snapshot of the current load order.
    /// </summary>
    public async Task<LoadOrderSnapshot> SaveSnapshotAsync(string? description = null)
    {
        await EnsureHistoryLoadedAsync();

        var viewModels = await GetModuleViewModelsAsync();
        var snapshot = new LoadOrderSnapshot
        {
            Description = description,
            ModuleOrder = viewModels?.Select(vm => vm.ModuleInfoExtended.Id).ToList() ?? new List<string>(),
            EnabledState = viewModels?.ToDictionary(vm => vm.ModuleInfoExtended.Id, vm => vm.IsSelected) 
                           ?? new Dictionary<string, bool>()
        };

        _loadOrderHistory!.Snapshots.Add(snapshot);

        // Trim old snapshots
        while (_loadOrderHistory.Snapshots.Count > _loadOrderHistory.MaxSnapshots)
        {
            _loadOrderHistory.Snapshots.RemoveAt(0);
        }

        await SaveHistoryAsync();
        return snapshot;
    }

    /// <summary>
    /// External<br/>
    /// Gets load order history.
    /// </summary>
    public async Task<IReadOnlyList<LoadOrderSnapshot>> GetSnapshotsAsync()
    {
        await EnsureHistoryLoadedAsync();
        return _loadOrderHistory!.Snapshots.OrderByDescending(s => s.CreatedAt).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Restores a snapshot.
    /// </summary>
    public async Task<bool> RestoreSnapshotAsync(string snapshotId)
    {
        await EnsureHistoryLoadedAsync();
        var snapshot = _loadOrderHistory!.Snapshots.FirstOrDefault(s => s.Id == snapshotId);
        
        if (snapshot == null)
            return false;

        try
        {
            // Save current state before restoring
            await SaveSnapshotAsync($"Before restoring snapshot from {snapshot.CreatedAt:g}");

            var modules = await GetModulesAsync();
            var moduleDict = modules.ToDictionary(m => m.Id, m => m);

            var loadOrder = new LoadOrder();
            for (var i = 0; i < snapshot.ModuleOrder.Count; i++)
            {
                var moduleId = snapshot.ModuleOrder[i];
                if (!moduleDict.TryGetValue(moduleId, out var module))
                    continue;

                var isEnabled = snapshot.EnabledState.TryGetValue(moduleId, out var enabled) && enabled;
                
                loadOrder[moduleId] = new LoadOrderEntry
                {
                    Id = moduleId,
                    Name = module.Name,
                    IsSelected = isEnabled,
                    IsDisabled = false,
                    Index = i
                };
            }

            await SetGameParameterLoadOrderAsync(loadOrder);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Deletes a snapshot.
    /// </summary>
    public async Task<bool> DeleteSnapshotAsync(string snapshotId)
    {
        await EnsureHistoryLoadedAsync();
        var snapshot = _loadOrderHistory!.Snapshots.FirstOrDefault(s => s.Id == snapshotId);
        
        if (snapshot == null)
            return false;

        _loadOrderHistory.Snapshots.Remove(snapshot);
        await SaveHistoryAsync();
        return true;
    }

    /// <summary>
    /// External<br/>
    /// Compares current load order with a snapshot.
    /// </summary>
    public async Task<LoadOrderComparison> CompareWithSnapshotAsync(string snapshotId)
    {
        await EnsureHistoryLoadedAsync();
        var snapshot = _loadOrderHistory!.Snapshots.FirstOrDefault(s => s.Id == snapshotId);
        var comparison = new LoadOrderComparison();

        if (snapshot == null)
            return comparison;

        var viewModels = await GetModuleViewModelsAsync();
        var currentOrder = viewModels?.Select(vm => vm.ModuleInfoExtended.Id).ToList() ?? new List<string>();
        var currentEnabled = viewModels?.ToDictionary(vm => vm.ModuleInfoExtended.Id, vm => vm.IsSelected) 
                             ?? new Dictionary<string, bool>();

        var snapshotSet = snapshot.ModuleOrder.ToHashSet();
        var currentSet = currentOrder.ToHashSet();

        comparison.Added = currentOrder.Where(id => !snapshotSet.Contains(id)).ToList();
        comparison.Removed = snapshot.ModuleOrder.Where(id => !currentSet.Contains(id)).ToList();

        // Check position changes
        var snapshotPositions = snapshot.ModuleOrder
            .Select((id, i) => (id, i))
            .ToDictionary(x => x.id, x => x.i);
        var currentPositions = currentOrder
            .Select((id, i) => (id, i))
            .ToDictionary(x => x.id, x => x.i);

        foreach (var id in currentSet.Intersect(snapshotSet))
        {
            if (snapshotPositions[id] != currentPositions[id])
            {
                comparison.PositionChanges.Add(new LoadOrderPositionChange
                {
                    ModuleId = id,
                    OldPosition = snapshotPositions[id],
                    NewPosition = currentPositions[id]
                });
            }

            var wasEnabled = snapshot.EnabledState.TryGetValue(id, out var oldState) && oldState;
            var isEnabled = currentEnabled.TryGetValue(id, out var newState) && newState;

            if (wasEnabled != isEnabled)
            {
                comparison.StateChanges.Add(new LoadOrderStateChange
                {
                    ModuleId = id,
                    WasEnabled = wasEnabled,
                    IsEnabled = isEnabled
                });
            }
        }

        return comparison;
    }

    /// <summary>
    /// External<br/>
    /// Gets the current health score.
    /// </summary>
    public async Task<int> GetLoadOrderHealthScoreAsync()
    {
        var result = await AnalyzeLoadOrderAsync(new OptimizationOptions { GenerateOptimizedOrder = false });
        return result.HealthScore;
    }

    private async Task<List<string>> GenerateOptimizedOrderAsync(
        List<ModuleViewModel> enabledModules, 
        Dictionary<string, ModuleInfoExtendedWithMetadata> moduleDict)
    {
        // Use topological sort based on dependencies
        var ordered = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(string moduleId)
        {
            if (visited.Contains(moduleId))
                return;
            if (visiting.Contains(moduleId))
                return; // Circular dependency, skip

            visiting.Add(moduleId);

            if (moduleDict.TryGetValue(moduleId, out var module))
            {
                // Visit dependencies first
                foreach (var dep in module.DependentModules ?? Enumerable.Empty<DependentModule>())
                {
                    Visit(dep.Id);
                }

                // Visit LoadAfter modules
                foreach (var loadAfter in module.DependentModuleMetadatas?
                    .Where(m => m.LoadType == LoadType.LoadAfterThis)
                    .Select(m => m.Id) ?? Enumerable.Empty<string>())
                {
                    Visit(loadAfter);
                }
            }

            visiting.Remove(moduleId);
            visited.Add(moduleId);
            ordered.Add(moduleId);
        }

        foreach (var vm in enabledModules)
        {
            Visit(vm.ModuleInfoExtended.Id);
        }

        return await Task.FromResult(ordered);
    }

    private async Task EnsureHistoryLoadedAsync()
    {
        if (_loadOrderHistory != null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, LoadOrderHistoryFile);

        if (File.Exists(dataPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(dataPath);
                _loadOrderHistory = JsonSerializer.Deserialize<LoadOrderHistory>(json, OptimizerJsonOptions);
            }
            catch
            {
                _loadOrderHistory = null;
            }
        }

        _loadOrderHistory ??= new LoadOrderHistory();
    }

    private async Task SaveHistoryAsync()
    {
        if (_loadOrderHistory == null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, LoadOrderHistoryFile);

        var json = JsonSerializer.Serialize(_loadOrderHistory, OptimizerJsonOptions);
        await File.WriteAllTextAsync(dataPath, json);
    }
}
