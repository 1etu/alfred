using System;
using System.Collections.Generic;

namespace Alfred.Widgets.Snapshots;

internal sealed record SnapshotDocument(
    int Version,
    DateTimeOffset GeneratedUtc,
    string? CurrencyCode,
    string? LaunchUri,
    int OpenItemCount,
    IReadOnlyList<SnapshotDocument.ItemEntry>? NextItems,
    SnapshotDocument.MonthEntry? Month,
    SnapshotDocument.PaymentEntry? NextPayment)
{
    internal sealed record ItemEntry(string? Title, decimal? Amount);

    internal sealed record MonthEntry(string? Label, decimal ExpectingIn, decimal ExpectingOut, decimal SpentSoFar);

    internal sealed record PaymentEntry(string? Name, decimal Amount, DateOnly DueLocal);
}
