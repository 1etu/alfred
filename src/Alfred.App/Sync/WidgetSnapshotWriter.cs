using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alfred.Core.Agenda;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;

namespace Alfred.App.Sync;

public static class WidgetSnapshotWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Alfred",
        "widgets");

    public static string FilePath { get; } = Path.Combine(Folder, "snapshot.json");

    public static void Write(VaultData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            Directory.CreateDirectory(Folder);

            string temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(Build(data), SerializerOptions));
            File.Move(temporary, FilePath, overwrite: true);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static Snapshot Build(VaultData data)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly monthStart = new(today.Year, today.Month, 1);
        DateOnly monthEnd = new(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        IReadOnlyList<AgendaItem> todayItems = AgendaService.Today(data, today);

        List<SnapshotItem> next = [.. todayItems
            .Where(item => !item.IsDone)
            .Take(3)
            .Select(item => new SnapshotItem(item.Title, item.Money?.Amount))];

        decimal expectingIn = 0;
        decimal expectingOut = 0;
        decimal spentSoFar = 0;

        foreach (LedgerEntry entry in data.Entries)
        {
            foreach (DateOnly occurrence in entry.Schedule.Occurrences(monthStart, monthEnd))
            {
                if (entry.Flow == CashFlow.In)
                {
                    expectingIn += entry.Money.Amount;
                }
                else
                {
                    expectingOut += entry.Money.Amount;

                    if (occurrence <= today)
                    {
                        spentSoFar += entry.Money.Amount;
                    }
                }
            }
        }

        SnapshotPayment? nextPayment = null;

        foreach (AgendaItem item in AgendaService.Upcoming(data, today, 90))
        {
            if (item.Flow == CashFlow.Out && item.Money is { } money)
            {
                nextPayment = new SnapshotPayment(item.Title, money.Amount, item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            }
        }

        return new Snapshot(
            1,
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            "TRY",
            "alfred://open",
            todayItems.Count(item => !item.IsDone),
            next,
            new SnapshotMonth(today.ToString("MMMM", CultureInfo.InvariantCulture), expectingIn, expectingOut, spentSoFar),
            nextPayment);
    }

    private sealed record Snapshot(
        int Version,
        string GeneratedUtc,
        string CurrencyCode,
        string LaunchUri,
        int OpenItemCount,
        IReadOnlyList<SnapshotItem> NextItems,
        SnapshotMonth Month,
        SnapshotPayment? NextPayment);

    private sealed record SnapshotItem(string Title, decimal? Amount);

    private sealed record SnapshotMonth(string Label, decimal ExpectingIn, decimal ExpectingOut, decimal SpentSoFar);

    private sealed record SnapshotPayment(string Name, decimal Amount, string DueLocal);
}
