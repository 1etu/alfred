using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Alfred.App.Updates;

public static class UpdateInstaller
{
    private const string ExecutableName = "Alfred.exe";
    private const string StagingFolderName = "staged";

    public static void InstallAndRestart(string zipPath, string installDirectory)
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
        Launch(WriteRestartScript(stagedDirectory, installDirectory));
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
                $"Alfred cannot update itself because '{installDirectory}' is not writable. Move Alfred out of Program Files, for example to %LOCALAPPDATA%\\Programs\\Alfred, and try again.",
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

    private static string WriteRestartScript(string stagedDirectory, string installDirectory)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), $"alfred-update-{Guid.NewGuid():N}.cmd");
        string processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        string[] lines =
        [
            "@echo off",
            "chcp 65001 >nul",
            ":wait",
            $"tasklist /FI \"PID eq {processId}\" | find \"{processId}\" >nul && timeout /t 1 >nul && goto wait",
            $"robocopy \"{stagedDirectory}\" \"{installDirectory}\" /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP >nul",
            $"start \"\" \"{Path.Combine(installDirectory, ExecutableName)}\"",
            $"rmdir /s /q \"{stagedDirectory}\" >nul 2>&1",
            "(goto) 2>nul & del \"%~f0\"",
            string.Empty,
        ];

        File.WriteAllText(scriptPath, string.Join("\r\n", lines), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return scriptPath;
    }

    private static void Launch(string scriptPath)
    {
        var startInfo = new ProcessStartInfo(scriptPath)
        {
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetTempPath(),
        };

        using Process? updater = Process.Start(startInfo) ?? throw new InvalidOperationException("Windows refused to start the update script.");
    }
}
