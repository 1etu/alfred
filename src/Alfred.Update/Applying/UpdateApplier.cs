using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Alfred.Localization;

namespace Alfred.Update.Applying;

public static class UpdateApplier
{
    private const string ExecutableName = "Alfred.exe";
    private const string PreviousExecutableName = "Alfred.exe.previous";
    private const string StagingFolderName = "staged";
    private const string SuggestedInstallPath = @"%LOCALAPPDATA%\Programs\Alfred";

    public static void ApplyAndRestart(string zipPath, string installDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);

        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"The downloaded update was not found at '{zipPath}'.", zipPath);
        }

        if (!Directory.Exists(installDirectory))
        {
            throw new DirectoryNotFoundException($"The install folder '{installDirectory}' no longer exists.");
        }

        EnsureWritable(installDirectory);

        string stagedDirectory = Stage(zipPath);
        IReadOnlyList<string> script = RestartScript(stagedDirectory, installDirectory, Environment.ProcessId);
        Launch(WriteScript(script));
    }

    internal static IReadOnlyList<string> RestartScript(string stagedDirectory, string installDirectory, int processId)
    {
        string processText = processId.ToString(CultureInfo.InvariantCulture);

        return
        [
            "@echo off",
            "chcp 65001 >nul",
            ":wait",
            $"tasklist /FI \"PID eq {processText}\" | find \"{processText}\" >nul && timeout /t 1 >nul && goto wait",
            $"copy /y \"{Path.Combine(installDirectory, ExecutableName)}\" \"{Path.Combine(installDirectory, PreviousExecutableName)}\" >nul 2>&1",
            $"robocopy \"{stagedDirectory}\" \"{installDirectory}\" /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP >nul",
            $"start \"\" \"{Path.Combine(installDirectory, ExecutableName)}\"",
            $"rmdir /s /q \"{stagedDirectory}\" >nul 2>&1",
            "(goto) 2>nul & del \"%~f0\"",
            string.Empty,
        ];
    }

    private static void EnsureWritable(string installDirectory)
    {
        string probePath = Path.Combine(installDirectory, $".alfred-update-{Guid.NewGuid():N}");

        try
        {
            using FileStream probe = new(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        }
        catch (Exception failure) when (failure is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                LocalizationService.Text(LocalizationKeys.UpdateNotWritable, installDirectory, SuggestedInstallPath),
                failure);
        }
    }

    private static string Stage(string zipPath)
    {
        string stagingRoot = Path.Combine(Path.GetDirectoryName(zipPath) ?? Path.GetTempPath(), StagingFolderName);

        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, recursive: true);
        }

        Directory.CreateDirectory(stagingRoot);
        ZipFile.ExtractToDirectory(zipPath, stagingRoot, overwriteFiles: true);
        return FindPayloadRoot(stagingRoot);
    }

    private static string FindPayloadRoot(string stagingRoot)
    {
        if (File.Exists(Path.Combine(stagingRoot, ExecutableName)))
        {
            return stagingRoot;
        }

        string[] nestedDirectories = Directory.GetDirectories(stagingRoot);

        if (nestedDirectories.Length == 1 && File.Exists(Path.Combine(nestedDirectories[0], ExecutableName)))
        {
            return nestedDirectories[0];
        }

        throw new InvalidDataException($"The update package does not contain {ExecutableName}.");
    }

    private static string WriteScript(IReadOnlyList<string> lines)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), $"alfred-update-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(scriptPath, string.Join("\r\n", lines), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return scriptPath;
    }

    private static void Launch(string scriptPath)
    {
        ProcessStartInfo startInfo = new(scriptPath)
        {
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetTempPath(),
        };

        using Process? updater = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows refused to start the update script.");
    }
}
