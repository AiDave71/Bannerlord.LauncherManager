using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Represents a saved configuration profile for mod loading.
/// </summary>
public class Profile
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name for the profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the profile.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The game mode for this profile.
    /// </summary>
    public GameMode GameMode { get; set; } = GameMode.Singleplayer;

    /// <summary>
    /// The load order configuration for this profile.
    /// </summary>
    public LoadOrder LoadOrder { get; set; } = new();

    /// <summary>
    /// Optional save file to load with this profile.
    /// </summary>
    public string? SaveFile { get; set; }

    /// <summary>
    /// Whether to continue the last save file when launching.
    /// </summary>
    public bool ContinueLastSave { get; set; } = false;

    /// <summary>
    /// Custom executable to use (if different from default).
    /// </summary>
    public string? CustomExecutable { get; set; }

    /// <summary>
    /// When the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the profile was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the profile was last used to launch the game.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Custom tags for organizing profiles.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this profile with a new ID.
    /// </summary>
    public Profile Clone(string? newName = null)
    {
        return new Profile
        {
            Id = Guid.NewGuid().ToString(),
            Name = newName ?? $"{Name} (Copy)",
            Description = Description,
            GameMode = GameMode,
            LoadOrder = new LoadOrder(LoadOrder),
            SaveFile = SaveFile,
            ContinueLastSave = ContinueLastSave,
            CustomExecutable = CustomExecutable,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            LastUsedAt = null,
            Tags = new List<string>(Tags)
        };
    }
}

/// <summary>
/// Collection of profiles with metadata.
/// </summary>
public class ProfileCollection
{
    /// <summary>
    /// Version of the profile format for migration purposes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The ID of the currently active profile.
    /// </summary>
    public string? ActiveProfileId { get; set; }

    /// <summary>
    /// All saved profiles.
    /// </summary>
    public List<Profile> Profiles { get; set; } = new();
}
