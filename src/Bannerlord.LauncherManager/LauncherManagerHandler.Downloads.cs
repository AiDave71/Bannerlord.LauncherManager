using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private readonly Dictionary<ModSource, IModSourceProvider> _modProviders = new();
    private readonly List<DownloadTask> _downloadQueue = new();
    private readonly object _downloadLock = new();

    /// <summary>
    /// Registers a mod source provider.
    /// </summary>
    public void RegisterModProvider(IModSourceProvider provider)
    {
        _modProviders[provider.Source] = provider;
    }

    /// <summary>
    /// Gets registered mod providers.
    /// </summary>
    public IReadOnlyDictionary<ModSource, IModSourceProvider> ModProviders => _modProviders;

    /// <summary>
    /// External<br/>
    /// Configures a mod source with the given settings.
    /// </summary>
    public async Task<bool> ConfigureModSourceAsync(ModSourceConfig config)
    {
        if (_modProviders.TryGetValue(config.Source, out var provider))
        {
            return await provider.ConfigureAsync(config);
        }
        return false;
    }

    /// <summary>
    /// External<br/>
    /// Checks for updates for all installed modules.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var result = new UpdateCheckResult { Success = true };
        var modules = await GetModulesAsync();

        var modulesWithSource = modules.Select(m => new ModuleInfoWithSource
        {
            ModuleId = m.Id,
            Name = m.Name,
            Version = m.Version.ToString(),
            Url = m.Url
        }).ToList();

        foreach (var provider in _modProviders.Values.Where(p => p.IsConfigured))
        {
            try
            {
                var providerResult = await provider.CheckForUpdatesAsync(modulesWithSource, cancellationToken);
                if (providerResult.Success)
                {
                    result.AvailableUpdates.AddRange(providerResult.AvailableUpdates);
                    result.UpToDateCount += providerResult.UpToDateCount;
                }
                else
                {
                    result.UncheckableCount += modulesWithSource.Count;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error checking {provider.Source}: {ex.Message}";
            }
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Checks for updates for a specific module.
    /// </summary>
    public async Task<ModUpdateInfo?> CheckModuleForUpdateAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        foreach (var provider in _modProviders.Values.Where(p => p.IsConfigured))
        {
            try
            {
                var info = await provider.GetModInfoAsync(moduleId, cancellationToken);
                if (info != null)
                {
                    return info;
                }
            }
            catch
            {
                // Try next provider
            }
        }
        return null;
    }

    /// <summary>
    /// External<br/>
    /// Queues a download for a mod update.
    /// </summary>
    public DownloadTask QueueDownload(ModUpdateInfo updateInfo)
    {
        var task = new DownloadTask
        {
            ModuleId = updateInfo.ModuleId,
            Name = updateInfo.Name,
            Version = updateInfo.LatestVersion,
            Source = updateInfo.Source,
            DownloadUrl = updateInfo.DownloadUrl ?? string.Empty,
            TotalBytes = updateInfo.FileSize ?? 0,
            IsUpdate = true
        };

        lock (_downloadLock)
        {
            _downloadQueue.Add(task);
        }

        return task;
    }

    /// <summary>
    /// External<br/>
    /// Gets the current download queue.
    /// </summary>
    public IReadOnlyList<DownloadTask> GetDownloadQueue()
    {
        lock (_downloadLock)
        {
            return _downloadQueue.ToList();
        }
    }

    /// <summary>
    /// External<br/>
    /// Gets a download task by ID.
    /// </summary>
    public DownloadTask? GetDownloadTask(string taskId)
    {
        lock (_downloadLock)
        {
            return _downloadQueue.FirstOrDefault(t => t.Id == taskId);
        }
    }

    /// <summary>
    /// External<br/>
    /// Starts downloading all queued tasks.
    /// </summary>
    public async Task<IReadOnlyList<DownloadResult>> ProcessDownloadQueueAsync(
        string downloadDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DownloadResult>();
        List<DownloadTask> tasksToProcess;

        lock (_downloadLock)
        {
            tasksToProcess = _downloadQueue.Where(t => t.Status == DownloadStatus.Pending).ToList();
        }

        foreach (var task in tasksToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                task.Status = DownloadStatus.Cancelled;
                results.Add(DownloadResult.AsError("Cancelled", task));
                continue;
            }

            var result = await DownloadModAsync(task, downloadDirectory, progress, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// External<br/>
    /// Downloads a specific mod.
    /// </summary>
    public async Task<DownloadResult> DownloadModAsync(
        DownloadTask task,
        string downloadDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        task.Status = DownloadStatus.Downloading;
        task.StartedAt = DateTime.UtcNow;

        try
        {
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            var fileName = $"{task.ModuleId}_{task.Version}.zip";
            var filePath = Path.Combine(downloadDirectory, fileName);
            task.LocalPath = filePath;

            // Check if provider can handle this
            if (_modProviders.TryGetValue(task.Source, out var provider) && provider.IsConfigured)
            {
                var modInfo = new ModUpdateInfo
                {
                    ModuleId = task.ModuleId,
                    Name = task.Name,
                    LatestVersion = task.Version,
                    Source = task.Source,
                    DownloadUrl = task.DownloadUrl
                };

                var result = await provider.DownloadModAsync(modInfo, filePath, progress, cancellationToken);
                
                if (result.Success)
                {
                    task.Status = DownloadStatus.Completed;
                    task.CompletedAt = DateTime.UtcNow;
                    task.DownloadedBytes = task.TotalBytes;
                }
                else
                {
                    task.Status = DownloadStatus.Failed;
                    task.ErrorMessage = result.ErrorMessage;
                }

                return result;
            }

            // Fallback to basic HTTP download
            return await BasicHttpDownloadAsync(task, filePath, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            task.Status = DownloadStatus.Cancelled;
            return DownloadResult.AsError("Download cancelled", task);
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = ex.Message;
            return DownloadResult.AsError(ex.Message, task);
        }
    }

    /// <summary>
    /// External<br/>
    /// Cancels a download task.
    /// </summary>
    public bool CancelDownload(string taskId)
    {
        lock (_downloadLock)
        {
            var task = _downloadQueue.FirstOrDefault(t => t.Id == taskId);
            if (task != null && task.Status == DownloadStatus.Downloading || task?.Status == DownloadStatus.Pending)
            {
                task.Status = DownloadStatus.Cancelled;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// External<br/>
    /// Removes completed/failed/cancelled downloads from the queue.
    /// </summary>
    public int ClearCompletedDownloads()
    {
        lock (_downloadLock)
        {
            var toRemove = _downloadQueue
                .Where(t => t.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
                .ToList();

            foreach (var task in toRemove)
            {
                _downloadQueue.Remove(task);
            }

            return toRemove.Count;
        }
    }

    /// <summary>
    /// External<br/>
    /// Searches for mods across all configured providers.
    /// </summary>
    public async Task<IReadOnlyList<ModSearchResult>> SearchModsAsync(
        string query,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ModSearchResult>();

        foreach (var provider in _modProviders.Values.Where(p => p.IsConfigured))
        {
            try
            {
                var providerResults = await provider.SearchModsAsync(query, maxResults, cancellationToken);
                results.AddRange(providerResults);
            }
            catch
            {
                // Continue with other providers
            }
        }

        return results
            .OrderByDescending(r => r.DownloadCount ?? 0)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Downloads and installs a mod update.
    /// </summary>
    public async Task<DownloadResult> DownloadAndInstallUpdateAsync(
        ModUpdateInfo updateInfo,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var installPath = await GetInstallPathAsync();
        var downloadDir = Path.Combine(installPath, "Downloads");

        var task = QueueDownload(updateInfo);
        var result = await DownloadModAsync(task, downloadDir, progress, cancellationToken);

        if (result.Success && result.FilePath != null)
        {
            // TODO: Extract and install the mod
            // This would involve extracting the archive and copying files to the Modules folder
            task.Status = DownloadStatus.Installing;

            // For now, just mark as needing manual installation
            result.Installed = false;
        }

        return result;
    }

    private async Task<DownloadResult> BasicHttpDownloadAsync(
        DownloadTask task,
        string filePath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(30);

        try
        {
            using var response = await httpClient.GetAsync(task.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            task.TotalBytes = totalBytes;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            var totalRead = 0L;
            var lastProgressUpdate = DateTime.UtcNow;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;
                task.DownloadedBytes = totalRead;

                if ((DateTime.UtcNow - lastProgressUpdate).TotalMilliseconds > 100)
                {
                    progress?.Report(new DownloadProgress
                    {
                        TotalBytes = totalBytes,
                        DownloadedBytes = totalRead,
                        Status = DownloadStatus.Downloading
                    });
                    lastProgressUpdate = DateTime.UtcNow;
                }
            }

            task.Status = DownloadStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;

            return DownloadResult.AsSuccess(task, filePath);
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = ex.Message;
            return DownloadResult.AsError(ex.Message, task);
        }
    }
}
