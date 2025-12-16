using Bannerlord.LauncherManager.Models;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private static readonly JsonSerializerOptions GraphJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Gets the complete dependency graph for all modules.
    /// </summary>
    public async Task<DependencyGraph> GetDependencyGraphAsync(GraphExportOptions? options = null)
    {
        options ??= new GraphExportOptions();
        var graph = new DependencyGraph();

        var modules = await GetModulesAsync();
        var viewModels = await GetModuleViewModelsAsync();
        var moduleDict = modules.ToDictionary(m => m.Id, m => m);
        var selectedIds = viewModels?.Where(vm => vm.IsSelected).Select(vm => vm.ModuleInfoExtended.Id).ToHashSet() ?? new HashSet<string>();

        // Build nodes
        foreach (var module in modules)
        {
            if (!options.IncludeNativeModules && module.IsNative())
                continue;

            if (options.SelectedOnly && !selectedIds.Contains(module.Id))
                continue;

            var node = new DependencyNode
            {
                Id = module.Id,
                Name = module.Name,
                Version = module.Version.ToString(),
                IsNative = module.IsNative(),
                IsSelected = selectedIds.Contains(module.Id)
            };

            graph.Nodes.Add(node);
        }

        var nodeIds = graph.Nodes.Select(n => n.Id).ToHashSet();

        // Build edges
        foreach (var module in modules)
        {
            if (!nodeIds.Contains(module.Id))
                continue;

            // Required dependencies
            foreach (var dep in module.DependentModules.Where(d => !d.IsOptional))
            {
                if (!nodeIds.Contains(dep.Id) && !options.IncludeNativeModules)
                    continue;

                graph.Edges.Add(new DependencyEdge
                {
                    SourceId = module.Id,
                    TargetId = dep.Id,
                    Type = DependencyType.Required,
                    RequiredVersion = dep.Version.ToString(),
                    IsSatisfied = moduleDict.ContainsKey(dep.Id) && selectedIds.Contains(dep.Id),
                    Label = "requires"
                });
            }

            // Optional dependencies
            if (options.IncludeOptional)
            {
                foreach (var dep in module.DependentModules.Where(d => d.IsOptional))
                {
                    if (!nodeIds.Contains(dep.Id))
                        continue;

                    graph.Edges.Add(new DependencyEdge
                    {
                        SourceId = module.Id,
                        TargetId = dep.Id,
                        Type = DependencyType.Optional,
                        RequiredVersion = dep.Version.ToString(),
                        IsSatisfied = moduleDict.ContainsKey(dep.Id),
                        Label = "optional"
                    });
                }
            }

            // Incompatible modules
            foreach (var incomp in module.IncompatibleModules)
            {
                if (!nodeIds.Contains(incomp.Id))
                    continue;

                graph.Edges.Add(new DependencyEdge
                {
                    SourceId = module.Id,
                    TargetId = incomp.Id,
                    Type = DependencyType.Incompatible,
                    IsSatisfied = !selectedIds.Contains(incomp.Id),
                    Label = "incompatible"
                });
            }

            // Load order dependencies
            foreach (var loadAfter in module.DependentModuleMetadatas.Where(m => m.LoadType == Bannerlord.ModuleManager.LoadType.LoadAfterThis))
            {
                if (!nodeIds.Contains(loadAfter.Id))
                    continue;

                graph.Edges.Add(new DependencyEdge
                {
                    SourceId = module.Id,
                    TargetId = loadAfter.Id,
                    Type = DependencyType.LoadBefore,
                    Label = "loads before"
                });
            }
        }

        // Calculate node metrics
        foreach (var node in graph.Nodes)
        {
            node.DependencyCount = graph.Edges.Count(e => e.SourceId == node.Id && e.Type == DependencyType.Required);
            node.DependentCount = graph.Edges.Count(e => e.TargetId == node.Id && e.Type == DependencyType.Required);
        }

        // Find orphaned modules (no dependents, not native)
        graph.OrphanedModules = graph.Nodes
            .Where(n => !n.IsNative && n.DependentCount == 0)
            .Select(n => n.Id)
            .ToList();

        // Find root modules (no dependencies except natives)
        var nativeIds = modules.Where(m => m.IsNative()).Select(m => m.Id).ToHashSet();
        graph.RootModules = graph.Nodes
            .Where(n => !n.IsNative && graph.Edges
                .Where(e => e.SourceId == n.Id && e.Type == DependencyType.Required)
                .All(e => nativeIds.Contains(e.TargetId)))
            .Select(n => n.Id)
            .ToList();

        // Detect circular dependencies
        DetectCircularDependencies(graph);

        return graph;
    }

    /// <summary>
    /// External<br/>
    /// Gets the dependency tree for a specific module.
    /// </summary>
    public async Task<ModuleDependencyTree> GetModuleDependencyTreeAsync(string moduleId)
    {
        var modules = await GetModulesAsync();
        var module = modules.FirstOrDefault(m => m.Id == moduleId);

        var tree = new ModuleDependencyTree
        {
            RootModuleId = moduleId,
            RootModuleName = module?.Name ?? moduleId
        };

        if (module == null)
            return tree;

        var moduleDict = modules.ToDictionary(m => m.Id, m => m);
        var visited = new HashSet<string>();

        // Build dependency tree
        tree.Dependencies = BuildDependencyBranch(module, moduleDict, visited, 0, true);
        tree.TotalDependencies = CountUniqueNodes(tree.Dependencies);
        
        // Build dependent tree
        visited.Clear();
        tree.Dependents = BuildDependentBranch(moduleId, modules.ToList(), visited, 0);
        tree.TotalDependents = CountUniqueNodes(tree.Dependents);

        // Calculate max depth
        tree.MaxDepth = Math.Max(
            GetMaxDepth(tree.Dependencies),
            GetMaxDepth(tree.Dependents)
        );

        return tree;
    }

    /// <summary>
    /// External<br/>
    /// Gets circular dependency chains.
    /// </summary>
    public async Task<IReadOnlyList<List<string>>> GetCircularDependenciesAsync()
    {
        var graph = await GetDependencyGraphAsync();
        return graph.CircularChains;
    }

    /// <summary>
    /// External<br/>
    /// Gets orphaned modules (modules with no dependents).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetOrphanedModulesAsync()
    {
        var graph = await GetDependencyGraphAsync();
        return graph.OrphanedModules;
    }

    /// <summary>
    /// External<br/>
    /// Exports the dependency graph to a string format.
    /// </summary>
    public async Task<string> ExportDependencyGraphAsync(GraphExportOptions? options = null)
    {
        options ??= new GraphExportOptions();
        var graph = await GetDependencyGraphAsync(options);

        return options.Format switch
        {
            GraphExportFormat.Json => ExportToJson(graph),
            GraphExportFormat.Dot => ExportToDot(graph, options),
            GraphExportFormat.Mermaid => ExportToMermaid(graph, options),
            GraphExportFormat.Csv => ExportToCsv(graph),
            _ => ExportToJson(graph)
        };
    }

    /// <summary>
    /// External<br/>
    /// Gets modules that would be affected by disabling a module.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAffectedModulesAsync(string moduleId)
    {
        var tree = await GetModuleDependencyTreeAsync(moduleId);
        return FlattenTreeIds(tree.Dependents);
    }

    /// <summary>
    /// External<br/>
    /// Gets all required modules for a module (recursive).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAllRequiredModulesAsync(string moduleId)
    {
        var tree = await GetModuleDependencyTreeAsync(moduleId);
        return FlattenTreeIds(tree.Dependencies);
    }

    private static List<DependencyTreeNode> BuildDependencyBranch(
        ModuleInfoExtendedWithMetadata module,
        Dictionary<string, ModuleInfoExtendedWithMetadata> moduleDict,
        HashSet<string> visited,
        int depth,
        bool isRoot)
    {
        var nodes = new List<DependencyTreeNode>();

        foreach (var dep in module.DependentModules)
        {
            if (visited.Contains(dep.Id))
                continue;

            visited.Add(dep.Id);

            var node = new DependencyTreeNode
            {
                Id = dep.Id,
                Name = moduleDict.TryGetValue(dep.Id, out var m) ? m.Name : dep.Id,
                Version = dep.Version.ToString(),
                Type = dep.IsOptional ? DependencyType.Optional : DependencyType.Required,
                IsInstalled = moduleDict.ContainsKey(dep.Id),
                IsSatisfied = moduleDict.ContainsKey(dep.Id),
                Depth = depth
            };

            if (moduleDict.TryGetValue(dep.Id, out var depModule))
            {
                node.Children = BuildDependencyBranch(depModule, moduleDict, visited, depth + 1, false);
            }

            nodes.Add(node);
        }

        return nodes;
    }

    private static List<DependencyTreeNode> BuildDependentBranch(
        string moduleId,
        List<ModuleInfoExtendedWithMetadata> allModules,
        HashSet<string> visited,
        int depth)
    {
        var nodes = new List<DependencyTreeNode>();
        var dependents = allModules.Where(m => m.DependentModules.Any(d => d.Id == moduleId));

        foreach (var dep in dependents)
        {
            if (visited.Contains(dep.Id))
                continue;

            visited.Add(dep.Id);

            var node = new DependencyTreeNode
            {
                Id = dep.Id,
                Name = dep.Name,
                Version = dep.Version.ToString(),
                Type = DependencyType.Required,
                IsInstalled = true,
                IsSatisfied = true,
                Depth = depth
            };

            node.Children = BuildDependentBranch(dep.Id, allModules, visited, depth + 1);
            nodes.Add(node);
        }

        return nodes;
    }

    private static void DetectCircularDependencies(DependencyGraph graph)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var currentPath = new List<string>();

        foreach (var node in graph.Nodes)
        {
            if (!visited.Contains(node.Id))
            {
                DetectCyclesDfs(node.Id, graph, visited, recursionStack, currentPath);
            }
        }

        graph.HasCircularDependencies = graph.CircularChains.Count > 0;
    }

    private static void DetectCyclesDfs(
        string nodeId,
        DependencyGraph graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> currentPath)
    {
        visited.Add(nodeId);
        recursionStack.Add(nodeId);
        currentPath.Add(nodeId);

        var edges = graph.Edges.Where(e => e.SourceId == nodeId && e.Type == DependencyType.Required);

        foreach (var edge in edges)
        {
            if (!visited.Contains(edge.TargetId))
            {
                DetectCyclesDfs(edge.TargetId, graph, visited, recursionStack, currentPath);
            }
            else if (recursionStack.Contains(edge.TargetId))
            {
                // Found a cycle
                var cycleStart = currentPath.IndexOf(edge.TargetId);
                var cycle = currentPath.Skip(cycleStart).ToList();
                cycle.Add(edge.TargetId); // Complete the cycle
                graph.CircularChains.Add(cycle);
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        recursionStack.Remove(nodeId);
    }

    private static int CountUniqueNodes(List<DependencyTreeNode> nodes)
    {
        var ids = new HashSet<string>();
        CountNodesRecursive(nodes, ids);
        return ids.Count;
    }

    private static void CountNodesRecursive(List<DependencyTreeNode> nodes, HashSet<string> ids)
    {
        foreach (var node in nodes)
        {
            ids.Add(node.Id);
            CountNodesRecursive(node.Children, ids);
        }
    }

    private static int GetMaxDepth(List<DependencyTreeNode> nodes)
    {
        if (nodes.Count == 0) return 0;
        return nodes.Max(n => 1 + GetMaxDepth(n.Children));
    }

    private static List<string> FlattenTreeIds(List<DependencyTreeNode> nodes)
    {
        var ids = new HashSet<string>();
        FlattenTreeIdsRecursive(nodes, ids);
        return ids.ToList();
    }

    private static void FlattenTreeIdsRecursive(List<DependencyTreeNode> nodes, HashSet<string> ids)
    {
        foreach (var node in nodes)
        {
            ids.Add(node.Id);
            FlattenTreeIdsRecursive(node.Children, ids);
        }
    }

    private static string ExportToJson(DependencyGraph graph)
    {
        return JsonSerializer.Serialize(graph, GraphJsonOptions);
    }

    private static string ExportToDot(DependencyGraph graph, GraphExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph ModuleDependencies {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [shape=box];");
        sb.AppendLine();

        foreach (var node in graph.Nodes)
        {
            var label = options.IncludeVersions ? $"{node.Name}\\n{node.Version}" : node.Name;
            var style = node.IsNative ? "filled" : (node.IsSelected ? "bold" : "dashed");
            sb.AppendLine($"  \"{node.Id}\" [label=\"{label}\", style={style}];");
        }

        sb.AppendLine();

        foreach (var edge in graph.Edges)
        {
            var style = edge.Type switch
            {
                DependencyType.Required => "solid",
                DependencyType.Optional => "dashed",
                DependencyType.Incompatible => "dotted",
                _ => "solid"
            };
            var color = edge.Type == DependencyType.Incompatible ? "red" : "black";
            sb.AppendLine($"  \"{edge.SourceId}\" -> \"{edge.TargetId}\" [style={style}, color={color}];");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string ExportToMermaid(DependencyGraph graph, GraphExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph LR");

        foreach (var edge in graph.Edges)
        {
            var arrow = edge.Type switch
            {
                DependencyType.Required => "-->",
                DependencyType.Optional => "-.->",
                DependencyType.Incompatible => "--x",
                _ => "-->"
            };
            sb.AppendLine($"  {edge.SourceId}{arrow}{edge.TargetId}");
        }

        return sb.ToString();
    }

    private static string ExportToCsv(DependencyGraph graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Source,Target,Type,Satisfied");

        foreach (var edge in graph.Edges)
        {
            sb.AppendLine($"{edge.SourceId},{edge.TargetId},{edge.Type},{edge.IsSatisfied}");
        }

        return sb.ToString();
    }
}
