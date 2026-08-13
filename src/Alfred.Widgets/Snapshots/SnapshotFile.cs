using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Alfred.Widgets.Snapshots;

internal static class SnapshotFile
{
    private const int SupportedVersion = 1;

    public static string Location { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Alfred",
        "widgets",
        "snapshot.json");

    public static WidgetSnapshot? Read()
    {
        var document = ReadDocument();

        if (document is null)
        {
            return null;
        }

        if (document.Version != SupportedVersion)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(document.CurrencyCode))
        {
            return null;
        }

        return ToSnapshot(document, document.CurrencyCode);
    }

    private static SnapshotDocument? ReadDocument()
    {
        if (!File.Exists(Location))
        {
            return null;
        }

        try
        {
            using var stream = File.Open(Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return JsonSerializer.Deserialize(stream, SnapshotJson.Default.SnapshotDocument);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static WidgetSnapshot ToSnapshot(SnapshotDocument document, string currencyCode)
    {
        return new WidgetSnapshot(
            document.GeneratedUtc,
            ToLaunchUri(document.LaunchUri),
            Math.Max(document.OpenItemCount, 0),
            ToRows(document.NextItems, currencyCode),
            ToMonthTotals(document.Month, currencyCode),
            ToUpcomingPayment(document.NextPayment, currencyCode));
    }

    private static Uri? ToLaunchUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!uri.Scheme.Equals("alfred", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri;
    }

    private static List<SnapshotRow> ToRows(IReadOnlyList<SnapshotDocument.ItemEntry>? entries, string currencyCode)
    {
        if (entries is null || entries.Count == 0)
        {
            return [];
        }

        var rows = new List<SnapshotRow>(entries.Count);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Title))
            {
                continue;
            }

            MoneyAmount? amount = entry.Amount is null ? null : new MoneyAmount(entry.Amount.Value, currencyCode);
            rows.Add(new SnapshotRow(entry.Title, amount));
        }

        return rows;
    }

    private static MonthTotals? ToMonthTotals(SnapshotDocument.MonthEntry? entry, string currencyCode)
    {
        if (entry is null)
        {
            return null;
        }

        return new MonthTotals(
            string.IsNullOrWhiteSpace(entry.Label) ? string.Empty : entry.Label,
            new MoneyAmount(entry.ExpectingIn, currencyCode),
            new MoneyAmount(entry.ExpectingOut, currencyCode),
            new MoneyAmount(entry.SpentSoFar, currencyCode));
    }

    private static UpcomingPayment? ToUpcomingPayment(SnapshotDocument.PaymentEntry? entry, string currencyCode)
    {
        if (entry is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            return null;
        }

        return new UpcomingPayment(entry.Name, new MoneyAmount(entry.Amount, currencyCode), entry.DueLocal);
    }
}
