using System;
using System.Collections.Generic;

namespace Alfred.Widgets.Snapshots;

internal sealed record WidgetSnapshot(
    DateTimeOffset GeneratedUtc,
    Uri? LaunchUri,
    int OpenItemCount,
    IReadOnlyList<SnapshotRow> NextItems,
    MonthTotals? Month,
    UpcomingPayment? NextPayment);
