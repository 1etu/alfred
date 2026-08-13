using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using Alfred.Widgets.Snapshots;

namespace Alfred.Widgets.Cards;

internal static class CardContent
{
    private const int VisibleRowCount = 3;

    private const string MissingLine = "Erm, we don't have it yet :(";

    private const string QuietLine = "Nothing else waiting.";

    public static WidgetCard Build(string definitionId, WidgetSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return BuildEmptyState(definitionId);
        }

        return definitionId switch
        {
            WidgetDefinition.Today => BuildToday(snapshot),
            WidgetDefinition.MoneyThisMonth => BuildMoneyThisMonth(snapshot),
            WidgetDefinition.NextPayment => BuildNextPayment(snapshot),
            _ => BuildEmptyState(definitionId),
        };
    }

    private static WidgetCard BuildToday(WidgetSnapshot snapshot)
    {
        var rows = new JsonArray();

        foreach (var row in snapshot.NextItems.Take(VisibleRowCount))
        {
            rows.Add(new JsonObject
            {
                ["title"] = row.Title,
                ["amount"] = row.Amount?.ToExactText() ?? string.Empty,
            });
        }

        var data = new JsonObject
        {
            ["openCount"] = snapshot.OpenItemCount.ToString(CultureInfo.CurrentCulture),
            ["openLabel"] = snapshot.OpenItemCount == 1 ? "open item" : "open items",
            ["rows"] = rows,
            ["hasRows"] = rows.Count > 0,
            ["quietLine"] = QuietLine,
        };

        return new WidgetCard(CardTemplate.Read("Today"), data.ToJsonString());
    }

    private static WidgetCard BuildMoneyThisMonth(WidgetSnapshot snapshot)
    {
        if (snapshot.Month is null)
        {
            return BuildEmptyState(WidgetDefinition.MoneyThisMonth);
        }

        var data = new JsonObject
        {
            ["monthLabel"] = snapshot.Month.Label,
            ["expectingIn"] = snapshot.Month.ExpectingIn.ToRoundedText(),
            ["expectingOut"] = snapshot.Month.ExpectingOut.ToRoundedText(),
            ["spentSoFar"] = snapshot.Month.SpentSoFar.ToRoundedText(),
        };

        return new WidgetCard(CardTemplate.Read("MoneyThisMonth"), data.ToJsonString());
    }

    private static WidgetCard BuildNextPayment(WidgetSnapshot snapshot)
    {
        if (snapshot.NextPayment is null)
        {
            return BuildEmptyState(WidgetDefinition.NextPayment);
        }

        var payment = snapshot.NextPayment;

        var data = new JsonObject
        {
            ["name"] = payment.Name,
            ["amount"] = payment.Amount.ToRoundedText(),
            ["dueLine"] = DescribeDue(payment.DaysUntil(DateOnly.FromDateTime(DateTime.Now))),
        };

        return new WidgetCard(CardTemplate.Read("NextPayment"), data.ToJsonString());
    }

    private static WidgetCard BuildEmptyState(string definitionId)
    {
        var data = new JsonObject
        {
            ["line"] = MissingLine,
            ["subject"] = DescribeWidget(definitionId),
        };

        return new WidgetCard(CardTemplate.Read("EmptyState"), data.ToJsonString());
    }

    private static string DescribeDue(int daysUntil) => daysUntil switch
    {
        < 0 => "Overdue",
        0 => "Due today",
        1 => "Due tomorrow",
        _ => string.Create(CultureInfo.CurrentCulture, $"Due in {daysUntil} days"),
    };

    private static string DescribeWidget(string definitionId) => definitionId switch
    {
        WidgetDefinition.Today => "Today",
        WidgetDefinition.MoneyThisMonth => "Money this month",
        WidgetDefinition.NextPayment => "Next payment",
        _ => "Alfred",
    };
}
