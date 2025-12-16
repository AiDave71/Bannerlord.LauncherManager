using Bannerlord.LauncherManager.Models;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    /// <summary>
    /// Options for launching the game.
    /// </summary>
    public class LaunchOptions
    {
        /// <summary>
        /// Whether to run the game with elevated (administrator) privileges.
        /// </summary>
        public bool RunAsAdmin { get; set; } = false;

        /// <summary>
        /// Whether to wait for the game process to exit before returning.
        /// </summary>
        public bool WaitForExit { get; set; } = false;

        /// <summary>
        /// Working directory for the game process. If null, uses the executable's directory.
        /// </summary>
        public string? WorkingDirectory { get; set; }
    }

    /// <summary>
    /// Result of a game launch operation.
    /// </summary>
    public class LaunchResult
    {
        /// <summary>
        /// Whether the launch was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if launch failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The process ID of the launched game, if successful.
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// Exit code of the process if WaitForExit was true.
        /// </summary>
        public int? ExitCode { get; set; }

        public static LaunchResult AsSuccess(int processId) => new() { Success = true, ProcessId = processId };
        public static LaunchResult AsSuccessWithExit(int processId, int exitCode) => new() { Success = true, ProcessId = processId, ExitCode = exitCode };
        public static LaunchResult AsError(string message) => new() { Success = false, ErrorMessage = message };
    }

    /// <summary>
    /// External<br/>
    /// Launches the game with the current parameters.
    /// </summary>
    public async Task<LaunchResult> LaunchGameAsync(LaunchOptions? options = null)
    {
        options ??= new LaunchOptions();

        try
        {
            var installPath = await GetInstallPathAsync();
            if (string.IsNullOrEmpty(installPath))
                return LaunchResult.AsError("Install path is not set.");

            var platform = await GetPlatformAsync();
            var configuration = GetConfigurationByPlatform(platform);
            if (string.IsNullOrEmpty(configuration))
                return LaunchResult.AsError($"Unknown platform: {platform}");

            var executablePath = Path.Combine(installPath, Constants.BinFolder, configuration, _currentExecutable);
            if (!File.Exists(executablePath))
                return LaunchResult.AsError($"Executable not found: {executablePath}");

            // Build command line arguments
            var arguments = BuildLaunchArguments();

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = options.WorkingDirectory ?? Path.GetDirectoryName(executablePath) ?? installPath
            };

            if (options.RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            var process = Process.Start(startInfo);
            if (process == null)
                return LaunchResult.AsError("Failed to start the game process.");

            var processId = process.Id;

            if (options.WaitForExit)
            {
                await process.WaitForExitAsync();
                return LaunchResult.AsSuccessWithExit(processId, process.ExitCode);
            }

            return LaunchResult.AsSuccess(processId);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            return LaunchResult.AsError("Launch cancelled by user (UAC prompt declined).");
        }
        catch (Exception ex)
        {
            return LaunchResult.AsError($"Failed to launch game: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Launches the game with a specific executable and custom arguments.
    /// </summary>
    public async Task<LaunchResult> LaunchGameWithExecutableAsync(string executablePath, string? arguments = null, LaunchOptions? options = null)
    {
        options ??= new LaunchOptions();

        try
        {
            if (!File.Exists(executablePath))
                return LaunchResult.AsError($"Executable not found: {executablePath}");

            var finalArguments = arguments ?? BuildLaunchArguments();

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = finalArguments,
                UseShellExecute = true,
                WorkingDirectory = options.WorkingDirectory ?? Path.GetDirectoryName(executablePath)
            };

            if (options.RunAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            var process = Process.Start(startInfo);
            if (process == null)
                return LaunchResult.AsError("Failed to start the game process.");

            var processId = process.Id;

            if (options.WaitForExit)
            {
                await process.WaitForExitAsync();
                return LaunchResult.AsSuccessWithExit(processId, process.ExitCode);
            }

            return LaunchResult.AsSuccess(processId);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return LaunchResult.AsError("Launch cancelled by user (UAC prompt declined).");
        }
        catch (Exception ex)
        {
            return LaunchResult.AsError($"Failed to launch game: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Gets the full path to the game executable based on current settings.
    /// </summary>
    public async Task<string?> GetGameExecutablePathAsync()
    {
        try
        {
            var installPath = await GetInstallPathAsync();
            if (string.IsNullOrEmpty(installPath))
                return null;

            var platform = await GetPlatformAsync();
            var configuration = GetConfigurationByPlatform(platform);
            if (string.IsNullOrEmpty(configuration))
                return null;

            var executablePath = Path.Combine(installPath, Constants.BinFolder, configuration, _currentExecutable);
            return File.Exists(executablePath) ? executablePath : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// External<br/>
    /// Checks if the game is currently running.
    /// </summary>
    public bool IsGameRunning()
    {
        try
        {
            var processName = Path.GetFileNameWithoutExtension(_currentExecutable);
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Gets the running game process, if any.
    /// </summary>
    public Process? GetRunningGameProcess()
    {
        try
        {
            var processName = Path.GetFileNameWithoutExtension(_currentExecutable);
            var processes = Process.GetProcessesByName(processName);
            return processes.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Internal<br/>
    /// Builds the command line arguments string for launching the game.
    /// </summary>
    private string BuildLaunchArguments()
    {
        var args = new[]
        {
            GetGameModeParameter(_currentGameMode),
            _currentLoadOrder ?? string.Empty,
            string.IsNullOrEmpty(_currentSaveFile) ? string.Empty : $"/continuesave \"{_currentSaveFile}\"",
            _continueLastSaveFile ? "/continuegame" : string.Empty
        };

        return string.Join(" ", args.Where(a => !string.IsNullOrWhiteSpace(a)));
    }
}
