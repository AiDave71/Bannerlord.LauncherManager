using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Type of dependency relationship.
/// </summary>
public enum DependencyType
{
    Required,
    Optional,
    Incompatible,
    LoadBefore,
    LoadAfter
}

/// <summary>
/// Represents a node in the dependency graph.
/// </summary>
public class DependencyNode
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Module version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a native module.
    /// </summary>
    public bool IsNative { get; set; }

    /// <summary>
    /// Whether the module is currently selected/enabled.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Depth in the dependency tree (0 = root).
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Number of modules that depend on this one.
    /// </summary>
    public int DependentCount { get; set; }

    /// <summary>
    /// Number of modules this depends on.
    /// </summary>
    public int DependencyCount { get; set; }
}

/// <summary>
/// Represents an edge (connection) in the dependency graph.
/// </summary>
public class DependencyEdge
{
    /// <summary>
    /// Source module ID (the dependent).
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Target module ID (the dependency).
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Type of dependency.
    /// </summary>
    public DependencyType Type { get; set; }

    /// <summary>
    /// Required version (if applicable).
    /// </summary>
    public string? RequiredVersion { get; set; }

    /// <summary>
    /// Whether this dependency is satisfied.
    /// </summary>
    public bool IsSatisfied { get; set; }

    /// <summary>
    /// Label for display.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Complete dependency graph structure.
/// </summary>
public class DependencyGraph
{
    /// <summary>
    /// All nodes in the graph.
    /// </summary>
    public List<DependencyNode> Nodes { get; set; } = new();

    /// <summary>
    /// All edges in the graph.
    /// </summary>
    public List<DependencyEdge> Edges { get; set; } = new();

    /// <summary>
    /// Total module count.
    /// </summary>
    public int TotalNodes => Nodes.Count;

    /// <summary>
    /// Total edge count.
    /// </summary>
    public int TotalEdges => Edges.Count;

    /// <summary>
    /// Whether circular dependencies were detected.
    /// </summary>
    public bool HasCircularDependencies { get; set; }

    /// <summary>
    /// List of circular dependency chains found.
    /// </summary>
    public List<List<string>> CircularChains { get; set; } = new();

    /// <summary>
    /// Orphaned modules (no dependents).
    /// </summary>
    public List<string> OrphanedModules { get; set; } = new();

    /// <summary>
    /// Root modules (no dependencies except natives).
    /// </summary>
    public List<string> RootModules { get; set; } = new();
}

/// <summary>
/// Dependency tree for a single module.
/// </summary>
public class ModuleDependencyTree
{
    /// <summary>
    /// Root module ID.
    /// </summary>
    public string RootModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Root module name.
    /// </summary>
    public string RootModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Direct dependencies.
    /// </summary>
    public List<DependencyTreeNode> Dependencies { get; set; } = new();

    /// <summary>
    /// Direct dependents (modules that depend on this).
    /// </summary>
    public List<DependencyTreeNode> Dependents { get; set; } = new();

    /// <summary>
    /// Total unique dependencies (flattened).
    /// </summary>
    public int TotalDependencies { get; set; }

    /// <summary>
    /// Total unique dependents (flattened).
    /// </summary>
    public int TotalDependents { get; set; }

    /// <summary>
    /// Maximum depth of dependency chain.
    /// </summary>
    public int MaxDepth { get; set; }
}

/// <summary>
/// Node in a dependency tree.
/// </summary>
public class DependencyTreeNode
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Module version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Type of dependency.
    /// </summary>
    public DependencyType Type { get; set; }

    /// <summary>
    /// Whether dependency is satisfied.
    /// </summary>
    public bool IsSatisfied { get; set; }

    /// <summary>
    /// Whether module is installed.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Depth in the tree.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Child dependencies.
    /// </summary>
    public List<DependencyTreeNode> Children { get; set; } = new();
}

/// <summary>
/// Export format for dependency graph.
/// </summary>
public enum GraphExportFormat
{
    Json,
    Dot,
    Mermaid,
    Csv
}

/// <summary>
/// Options for graph export.
/// </summary>
public class GraphExportOptions
{
    /// <summary>
    /// Export format.
    /// </summary>
    public GraphExportFormat Format { get; set; } = GraphExportFormat.Json;

    /// <summary>
    /// Whether to include native modules.
    /// </summary>
    public bool IncludeNativeModules { get; set; } = false;

    /// <summary>
    /// Whether to include optional dependencies.
    /// </summary>
    public bool IncludeOptional { get; set; } = true;

    /// <summary>
    /// Whether to include version information.
    /// </summary>
    public bool IncludeVersions { get; set; } = true;

    /// <summary>
    /// Only include selected/enabled modules.
    /// </summary>
    public bool SelectedOnly { get; set; } = false;
}
