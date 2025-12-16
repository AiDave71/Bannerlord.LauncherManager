using Bannerlord.LauncherManager.Models;
using Bannerlord.LauncherManager.Utils;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private ProfileManager? _profileManager;

    /// <summary>
    /// Gets the profile manager instance, creating it if necessary.
    /// </summary>
    protected ProfileManager ProfileManager => _profileManager ??= new ProfileManager(GetProfilesPathAsync);

    /// <summary>
    /// Gets the path where profiles should be stored.
    /// Override this to customize profile storage location.
    /// </summary>
    protected virtual Task<string> GetProfilesPathAsync() => GetInstallPathAsync();

    /// <summary>
    /// External<br/>
    /// Gets all saved profiles.
    /// </summary>
    public async Task<IReadOnlyList<Profile>> GetProfilesAsync()
    {
        await ProfileManager.LoadAsync();
        return ProfileManager.Profiles;
    }

    /// <summary>
    /// External<br/>
    /// Gets the currently active profile.
    /// </summary>
    public async Task<Profile?> GetActiveProfileAsync()
    {
        await ProfileManager.LoadAsync();
        return ProfileManager.ActiveProfile;
    }

    /// <summary>
    /// External<br/>
    /// Creates a new profile with the given name.
    /// </summary>
    public Task<Profile> CreateProfileAsync(string name, string? description = null)
    {
        return ProfileManager.CreateProfileAsync(name, description);
    }

    /// <summary>
    /// External<br/>
    /// Creates a profile from the current launcher state.
    /// </summary>
    public async Task<Profile> CreateProfileFromCurrentStateAsync(string name, string? description = null)
    {
        var loadOrder = await GetLoadOrderFromCurrentStateAsync();
        return await ProfileManager.CreateProfileFromCurrentStateAsync(
            name,
            loadOrder,
            _currentGameMode,
            _currentSaveFile,
            _continueLastSaveFile,
            _currentExecutable == Constants.BannerlordExecutable ? null : _currentExecutable,
            description
        );
    }

    /// <summary>
    /// External<br/>
    /// Gets a profile by ID.
    /// </summary>
    public Task<Profile?> GetProfileByIdAsync(string id)
    {
        return ProfileManager.GetProfileAsync(id);
    }

    /// <summary>
    /// External<br/>
    /// Gets a profile by name.
    /// </summary>
    public Task<Profile?> GetProfileByNameAsync(string name)
    {
        return ProfileManager.GetProfileByNameAsync(name);
    }

    /// <summary>
    /// External<br/>
    /// Updates an existing profile.
    /// </summary>
    public Task<bool> UpdateProfileAsync(Profile profile)
    {
        return ProfileManager.UpdateProfileAsync(profile);
    }

    /// <summary>
    /// External<br/>
    /// Deletes a profile by ID.
    /// </summary>
    public Task<bool> DeleteProfileAsync(string id)
    {
        return ProfileManager.DeleteProfileAsync(id);
    }

    /// <summary>
    /// External<br/>
    /// Duplicates a profile.
    /// </summary>
    public Task<Profile?> DuplicateProfileAsync(string id, string? newName = null)
    {
        return ProfileManager.DuplicateProfileAsync(id, newName);
    }

    /// <summary>
    /// External<br/>
    /// Sets the active profile.
    /// </summary>
    public Task<bool> SetActiveProfileAsync(string? id)
    {
        return ProfileManager.SetActiveProfileAsync(id);
    }

    /// <summary>
    /// External<br/>
    /// Applies a profile's settings to the current launcher state.
    /// </summary>
    public async Task<bool> ApplyProfileAsync(string id)
    {
        var profile = await ProfileManager.GetProfileAsync(id);
        if (profile == null) return false;

        // Apply game mode
        _currentGameMode = profile.GameMode;

        // Apply executable
        if (!string.IsNullOrEmpty(profile.CustomExecutable))
        {
            _currentExecutable = profile.CustomExecutable;
        }
        else
        {
            _currentExecutable = Constants.BannerlordExecutable;
        }

        // Apply save file settings
        _currentSaveFile = profile.SaveFile;
        _continueLastSaveFile = profile.ContinueLastSave;

        // Apply load order
        await SetGameParameterLoadOrderAsync(profile.LoadOrder);

        // Set as active and mark as used
        await ProfileManager.SetActiveProfileAsync(id);
        await ProfileManager.MarkProfileUsedAsync(id);

        // Refresh game parameters
        await RefreshGameParametersAsync();

        return true;
    }

    /// <summary>
    /// External<br/>
    /// Updates the active profile with the current launcher state.
    /// </summary>
    public async Task<bool> SaveCurrentStateToActiveProfileAsync()
    {
        var profile = ProfileManager.ActiveProfile;
        if (profile == null) return false;

        profile.GameMode = _currentGameMode;
        profile.SaveFile = _currentSaveFile;
        profile.ContinueLastSave = _continueLastSaveFile;
        profile.CustomExecutable = _currentExecutable == Constants.BannerlordExecutable ? null : _currentExecutable;
        profile.LoadOrder = await GetLoadOrderFromCurrentStateAsync();

        return await ProfileManager.UpdateProfileAsync(profile);
    }

    /// <summary>
    /// External<br/>
    /// Gets recently used profiles.
    /// </summary>
    public Task<IReadOnlyList<Profile>> GetRecentProfilesAsync(int count = 5)
    {
        return ProfileManager.GetRecentProfilesAsync(count);
    }

    /// <summary>
    /// External<br/>
    /// Gets profiles by tag.
    /// </summary>
    public Task<IReadOnlyList<Profile>> GetProfilesByTagAsync(string tag)
    {
        return ProfileManager.GetProfilesByTagAsync(tag);
    }

    /// <summary>
    /// External<br/>
    /// Exports a profile to JSON string.
    /// </summary>
    public Task<string?> ExportProfileAsync(string id)
    {
        return ProfileManager.ExportProfileAsync(id);
    }

    /// <summary>
    /// External<br/>
    /// Imports a profile from JSON string.
    /// </summary>
    public Task<Profile?> ImportProfileAsync(string json)
    {
        return ProfileManager.ImportProfileAsync(json);
    }

    /// <summary>
    /// External<br/>
    /// Exports a profile to a file.
    /// </summary>
    public Task<bool> ExportProfileToFileAsync(string id, string filePath)
    {
        return ProfileManager.ExportProfileToFileAsync(id, filePath);
    }

    /// <summary>
    /// External<br/>
    /// Imports a profile from a file.
    /// </summary>
    public Task<Profile?> ImportProfileFromFileAsync(string filePath)
    {
        return ProfileManager.ImportProfileFromFileAsync(filePath);
    }

    /// <summary>
    /// Internal<br/>
    /// Gets the current load order as a LoadOrder object.
    /// </summary>
    private async Task<LoadOrder> GetLoadOrderFromCurrentStateAsync()
    {
        var viewModels = await GetModuleViewModelsAsync();
        var loadOrder = new LoadOrder();
        
        if (viewModels != null)
        {
            foreach (var vm in viewModels)
            {
                loadOrder[vm.ModuleInfoExtended.Id] = new LoadOrderEntry
                {
                    Id = vm.ModuleInfoExtended.Id,
                    Name = vm.ModuleInfoExtended.Name,
                    IsSelected = vm.IsSelected,
                    IsDisabled = vm.IsDisabled,
                    Index = vm.Index
                };
            }
        }

        return loadOrder;
    }
}
