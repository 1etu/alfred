using System;

namespace Alfred.Widgets.Snapshots;

internal sealed record UpcomingPayment(string Name, MoneyAmount Amount, DateOnly DueLocal)
{
    public int DaysUntil(DateOnly today) => DueLocal.DayNumber - today.DayNumber;
}
