using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Alfred.Localization;
using Alfred.Update.Applying;
using Alfred.Update.Checking;
using Alfred.Update.Fetching;

namespace Alfred.Update;

public sealed class UpdateService : INotifyPropertyChanged
{
    private static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(6);

    private readonly ReleaseFetcher _fetcher;
    private readonly IReleaseFeed _feed;
    private string? _fetchedZipPath;

    public UpdateService()
        : this(new GitHubReleaseFeed(), ReleaseFetcher.DefaultFolder)
    {
    }

    internal UpdateService(IReleaseFeed feed, string fetchFolder)
    {
        _feed = feed;
        _fetcher = new ReleaseFetcher(feed, fetchFolder);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UpdateState State
    {
        get;
        private set => Set(ref field, value);
    } = UpdateState.Idle;

    public Release? Available
    {
        get;
        private set => Set(ref field, value);
    }

    public double Progress
    {
        get;
        private set => Set(ref field, value);
    }

    public string Message
    {
        get;
        private set => Set(ref field, value);
    } = string.Empty;

    public string CurrentVersionText { get; } = "v" + AppVersion.Current;

    public TimeSpan MinimumCheckInterval { get; set; } = DefaultCheckInterval;

    public DateTimeOffset? LastCheckedUtc { get; set; }

    public async Task CheckIfDueAsync(UpdateChannel channel, CancellationToken cancellationToken)
    {
        if (LastCheckedUtc is DateTimeOffset lastChecked &&
            DateTimeOffset.UtcNow - lastChecked < MinimumCheckInterval)
        {
            return;
        }

        await CheckAsync(channel, cancellationToken).ConfigureAwait(false);
    }

    public async Task CheckAsync(UpdateChannel channel, CancellationToken cancellationToken)
    {
        if (State is UpdateState.Checking or UpdateState.Downloading)
        {
            return;
        }

        State = UpdateState.Checking;
        Message = LocalizationService.Text(LocalizationKeys.UpdateChecking);
        Progress = 0d;

        Release? release;

        try
        {
            string payload = await _feed.ReadReleasesAsync(cancellationToken).ConfigureAwait(false);
            LastCheckedUtc = DateTimeOffset.UtcNow;
            release = ReleaseReader.FindNewest(payload, channel, AppVersion.Current);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Fail(LocalizationService.Text(LocalizationKeys.UpdateTimedOut));
            return;
        }
        catch (HttpRequestException failure)
        {
            Fail(failure.StatusCode is { } status
                ? LocalizationService.Text(LocalizationKeys.UpdateGitHubStatus, (int)status)
                : failure.Message);
            return;
        }
        catch (JsonException)
        {
            Fail(LocalizationService.Text(LocalizationKeys.UpdateBadListing));
            return;
        }

        if (release is null)
        {
            Available = null;
            State = UpdateState.UpToDate;
            Message = LocalizationService.Text(LocalizationKeys.UpdateUpToDate, CurrentVersionText);
            return;
        }

        Available = release;
        State = UpdateState.Available;
        Message = LocalizationService.Text(LocalizationKeys.UpdateAvailable, release.Tag);
    }

    public async Task DownloadAsync(CancellationToken cancellationToken)
    {
        if (State == UpdateState.Downloading)
        {
            return;
        }

        if (Available is not Release release)
        {
            Fail(LocalizationService.Text(LocalizationKeys.UpdateNothingToDownload));
            return;
        }

        State = UpdateState.Downloading;
        Progress = 0d;
        Report(new FetchProgress(0, release.SizeBytes));

        Progress<FetchProgress> listener = new(Report);

        try
        {
            _fetchedZipPath = await _fetcher.FetchAsync(release, listener, cancellationToken).ConfigureAwait(false);
            State = UpdateState.Ready;
            Message = LocalizationService.Text(LocalizationKeys.UpdateReady, release.Tag);
        }
        catch (OperationCanceledException)
        {
            _fetchedZipPath = null;
            State = UpdateState.Available;
            Message = LocalizationService.Text(LocalizationKeys.UpdateCancelled);
        }
        catch (InvalidDataException)
        {
            _fetchedZipPath = null;
            Fail(LocalizationService.Text(LocalizationKeys.UpdateBadArchive));
        }
        catch (Exception failure) when (failure is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            _fetchedZipPath = null;
            Fail(failure.Message);
        }
    }

    public bool InstallAndRestart()
    {
        if (State != UpdateState.Ready || _fetchedZipPath is not string zipPath)
        {
            Fail(LocalizationService.Text(LocalizationKeys.UpdateNotDownloaded));
            return false;
        }

        try
        {
            UpdateApplier.ApplyAndRestart(zipPath, ResolveInstallDirectory());
            Message = LocalizationService.Text(LocalizationKeys.UpdateRestarting);
            return true;
        }
        catch (Exception failure) when (failure is InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException or FileNotFoundException or DirectoryNotFoundException)
        {
            Fail(failure.Message);
            return false;
        }
    }

    private void Report(FetchProgress progress)
    {
        Progress = progress.Ratio;

        if (progress.TotalBytes > 0)
        {
            Message = LocalizationService.Text(
                LocalizationKeys.UpdateDownloading,
                Megabytes(progress.CopiedBytes),
                Megabytes(progress.TotalBytes));
        }
    }

    private void Fail(string message)
    {
        State = UpdateState.Failed;
        Message = message;
    }

    private static string Megabytes(long bytes) =>
        string.Create(LocalizationService.Current.Culture, $"{bytes / 1048576d:F1} MB");

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
