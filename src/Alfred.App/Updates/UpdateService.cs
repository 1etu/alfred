using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Alfred.App.Updates;

public enum UpdateState
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Ready,
    Failed,
}

public sealed class UpdateService : INotifyPropertyChanged
{
    private static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(6);

    private readonly UpdateChecker _checker = new();

    private UpdateState _state = UpdateState.Idle;
    private ReleaseInfo? _available;
    private double _progress;
    private string _message = string.Empty;
    private string? _downloadedZipPath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UpdateState State
    {
        get => _state;
        private set => Set(ref _state, value);
    }

    public ReleaseInfo? Available
    {
        get => _available;
        private set => Set(ref _available, value);
    }

    public double Progress
    {
        get => _progress;
        private set => Set(ref _progress, value);
    }

    public string Message
    {
        get => _message;
        private set => Set(ref _message, value);
    }

    public string CurrentVersionText { get; } = "v" + AppVersion.Current;

    public TimeSpan MinimumCheckInterval { get; set; } = DefaultCheckInterval;

    public DateTimeOffset? LastCheckedUtc
    {
        get => _checker.LastCheckedUtc;
        set => _checker.LastCheckedUtc = value;
    }

    public async Task CheckIfDueAsync(UpdateChannel channel, CancellationToken cancellationToken)
    {
        if (!_checker.IsCheckDue(MinimumCheckInterval))
        {
            return;
        }

        await CheckAsync(channel, cancellationToken);
    }

    public async Task CheckAsync(UpdateChannel channel, CancellationToken cancellationToken)
    {
        if (State is UpdateState.Checking or UpdateState.Downloading)
        {
            return;
        }

        State = UpdateState.Checking;
        Message = "Checking for updates...";
        Progress = 0d;

        ReleaseInfo? release = await _checker.CheckAsync(channel, cancellationToken);
        if (release is null)
        {
            SettleEmptyCheck();
            return;
        }

        Available = release;
        State = UpdateState.Available;
        Message = $"Alfred {release.Tag} is available.";
    }

    public async Task DownloadAsync(CancellationToken cancellationToken)
    {
        if (State == UpdateState.Downloading)
        {
            return;
        }

        if (Available is not ReleaseInfo release)
        {
            State = UpdateState.Failed;
            Message = "There is no update to download.";
            return;
        }

        State = UpdateState.Downloading;
        Progress = 0d;
        Message = $"Downloading Alfred {release.Tag}...";

        var listener = new System.Progress<double>(value => Progress = value);

        try
        {
            _downloadedZipPath = await UpdateDownloader.DownloadAsync(release, listener, cancellationToken);
            State = UpdateState.Ready;
            Message = $"Alfred {release.Tag} is ready to install.";
        }
        catch (OperationCanceledException)
        {
            _downloadedZipPath = null;
            State = UpdateState.Available;
            Message = "The download was cancelled.";
        }
        catch (Exception failure) when (failure is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            _downloadedZipPath = null;
            State = UpdateState.Failed;
            Message = failure.Message;
        }
    }

    public bool InstallAndRestart()
    {
        if (State != UpdateState.Ready || _downloadedZipPath is not string zipPath)
        {
            State = UpdateState.Failed;
            Message = "The update has not finished downloading.";
            return false;
        }

        try
        {
            UpdateInstaller.InstallAndRestart(zipPath, ResolveInstallDirectory());
            Message = "Restarting to finish the update...";
            return true;
        }
        catch (Exception failure) when (failure is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            State = UpdateState.Failed;
            Message = failure.Message;
            return false;
        }
    }

    private void SettleEmptyCheck()
    {
        if (_checker.LastError is string error)
        {
            State = UpdateState.Failed;
            Message = error;
            return;
        }

        Available = null;
        State = UpdateState.UpToDate;
        Message = $"Alfred {CurrentVersionText} is up to date.";
    }

    private static string ResolveInstallDirectory()
    {
        string? processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        return string.IsNullOrEmpty(processDirectory) ? AppContext.BaseDirectory : processDirectory;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
