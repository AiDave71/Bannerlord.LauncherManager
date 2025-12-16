using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager.Utils;

/// <summary>
/// Manages profile CRUD operations and persistence.
/// </summary>
public class ProfileManager
{
    private const string ProfileFileName = "profiles.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Func<Task<string>> _getProfilesPathAsync;
    private ProfileCollection _collection = new();
    private bool _isLoaded = false;

    public ProfileManager(Func<Task<string>> getProfilesPathAsync)
    {
        _getProfilesPathAsync = getProfilesPathAsync;
    }

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    public IReadOnlyList<Profile> Profiles => _collection.Profiles.AsReadOnly();

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    public Profile? ActiveProfile => _collection.Profiles.FirstOrDefault(p => p.Id == _collection.ActiveProfileId);

    /// <summary>
    /// Gets the active profile ID.
    /// </summary>
    public string? ActiveProfileId => _collection.ActiveProfileId;

    /// <summary>
    /// Loads profiles from disk.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            var profilesPath = await GetProfileFilePathAsync();
            if (File.Exists(profilesPath))
            {
                var json = await File.ReadAllTextAsync(profilesPath);
                _collection = JsonSerializer.Deserialize<ProfileCollection>(json, JsonOptions) ?? new ProfileCollection();
            }
            else
            {
                _collection = new ProfileCollection();
            }
            _isLoaded = true;
        }
        catch
        {
            _collection = new ProfileCollection();
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Saves profiles to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var profilesPath = await GetProfileFilePathAsync();
            var directory = Path.GetDirectoryName(profilesPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_collection, JsonOptions);
            await File.WriteAllTextAsync(profilesPath, json);
        }
        catch
        {
            // Log error in production
        }
    }

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    public async Task<Profile> CreateProfileAsync(string name, string? description = null)
    {
        await EnsureLoadedAsync();

        var profile = new Profile
        {
            Name = name,
            Description = description
        };

        _collection.Profiles.Add(profile);
        await SaveAsync();
        return profile;
    }

    /// <summary>
    /// Creates a profile from the current launcher state.
    /// </summary>
    public async Task<Profile> CreateProfileFromCurrentStateAsync(
        string name,
        LoadOrder loadOrder,
        GameMode gameMode,
        string? saveFile = null,
        bool continueLastSave = false,
        string? customExecutable = null,
        string? description = null)
    {
        await EnsureLoadedAsync();

        var profile = new Profile
        {
            Name = name,
            Description = description,
            GameMode = gameMode,
            LoadOrder = new LoadOrder(loadOrder),
            SaveFile = saveFile,
            ContinueLastSave = continueLastSave,
            CustomExecutable = customExecutable
        };

        _collection.Profiles.Add(profile);
        await SaveAsync();
        return profile;
    }

    /// <summary>
    /// Gets a profile by ID.
    /// </summary>
    public async Task<Profile?> GetProfileAsync(string id)
    {
        await EnsureLoadedAsync();
        return _collection.Profiles.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    public async Task<Profile?> GetProfileByNameAsync(string name)
    {
        await EnsureLoadedAsync();
        return _collection.Profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates an existing profile.
    /// </summary>
    public async Task<bool> UpdateProfileAsync(Profile profile)
    {
        await EnsureLoadedAsync();

        var index = _collection.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index < 0) return false;

        profile.ModifiedAt = DateTime.UtcNow;
        _collection.Profiles[index] = profile;
        await SaveAsync();
        return true;
    }

    /// <summary>
    /// Deletes a profile by ID.
    /// </summary>
    public async Task<bool> DeleteProfileAsync(string id)
    {
        await EnsureLoadedAsync();

        var removed = _collection.Profiles.RemoveAll(p => p.Id == id) > 0;
        if (removed)
        {
            if (_collection.ActiveProfileId == id)
            {
                _collection.ActiveProfileId = null;
            }
            await SaveAsync();
        }
        return removed;
    }

    /// <summary>
    /// Duplicates a profile.
    /// </summary>
    public async Task<Profile?> DuplicateProfileAsync(string id, string? newName = null)
    {
        await EnsureLoadedAsync();

        var original = _collection.Profiles.FirstOrDefault(p => p.Id == id);
        if (original == null) return null;

        var clone = original.Clone(newName);
        _collection.Profiles.Add(clone);
        await SaveAsync();
        return clone;
    }

    /// <summary>
    /// Sets the active profile.
    /// </summary>
    public async Task<bool> SetActiveProfileAsync(string? id)
    {
        await EnsureLoadedAsync();

        if (id != null && !_collection.Profiles.Any(p => p.Id == id))
        {
            return false;
        }

        _collection.ActiveProfileId = id;
        await SaveAsync();
        return true;
    }

    /// <summary>
    /// Marks a profile as used (updates LastUsedAt).
    /// </summary>
    public async Task MarkProfileUsedAsync(string id)
    {
        await EnsureLoadedAsync();

        var profile = _collection.Profiles.FirstOrDefault(p => p.Id == id);
        if (profile != null)
        {
            profile.LastUsedAt = DateTime.UtcNow;
            await SaveAsync();
        }
    }

    /// <summary>
    /// Gets profiles filtered by tag.
    /// </summary>
    public async Task<IReadOnlyList<Profile>> GetProfilesByTagAsync(string tag)
    {
        await EnsureLoadedAsync();
        return _collection.Profiles
            .Where(p => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets profiles sorted by last used date.
    /// </summary>
    public async Task<IReadOnlyList<Profile>> GetRecentProfilesAsync(int count = 5)
    {
        await EnsureLoadedAsync();
        return _collection.Profiles
            .Where(p => p.LastUsedAt.HasValue)
            .OrderByDescending(p => p.LastUsedAt)
            .Take(count)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Exports a profile to JSON string.
    /// </summary>
    public async Task<string?> ExportProfileAsync(string id)
    {
        await EnsureLoadedAsync();

        var profile = _collection.Profiles.FirstOrDefault(p => p.Id == id);
        if (profile == null) return null;

        return JsonSerializer.Serialize(profile, JsonOptions);
    }

    /// <summary>
    /// Imports a profile from JSON string.
    /// </summary>
    public async Task<Profile?> ImportProfileAsync(string json, bool generateNewId = true)
    {
        await EnsureLoadedAsync();

        try
        {
            var profile = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
            if (profile == null) return null;

            if (generateNewId)
            {
                profile.Id = Guid.NewGuid().ToString();
            }

            // Ensure unique name
            var baseName = profile.Name;
            var counter = 1;
            while (_collection.Profiles.Any(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                profile.Name = $"{baseName} ({counter++})";
            }

            profile.CreatedAt = DateTime.UtcNow;
            profile.ModifiedAt = DateTime.UtcNow;
            profile.LastUsedAt = null;

            _collection.Profiles.Add(profile);
            await SaveAsync();
            return profile;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Exports a profile to a file.
    /// </summary>
    public async Task<bool> ExportProfileToFileAsync(string id, string filePath)
    {
        var json = await ExportProfileAsync(id);
        if (json == null) return false;

        try
        {
            await File.WriteAllTextAsync(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Imports a profile from a file.
    /// </summary>
    public async Task<Profile?> ImportProfileFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            var json = await File.ReadAllTextAsync(filePath);
            return await ImportProfileAsync(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> GetProfileFilePathAsync()
    {
        var basePath = await _getProfilesPathAsync();
        return Path.Combine(basePath, ProfileFileName);
    }

    private async Task EnsureLoadedAsync()
    {
        if (!_isLoaded)
        {
            await LoadAsync();
        }
    }
}
