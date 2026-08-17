using Alfred.Core.Ledger;
using Xunit;

namespace Alfred.Core.Tests.Ledger;

public class AmountHintsTests
{
    [Theory]
    [InlineData("rent 42k", "rent", 42_000)]
    [InlineData("rent 42K", "rent", 42_000)]
    [InlineData("coffee budget 1.5k", "coffee budget", 1_500)]
    [InlineData("coffee budget 1,5k", "coffee budget", 1_500)]
    [InlineData("sale 2m", "sale", 2_000_000)]
    [InlineData("sale 1.5m", "sale", 1_500_000)]
    public void ReadsShorthandMultipliers(string text, string cleaned, decimal amount)
    {
        Assert.True(AmountHints.TryExtract(text, out string rest, out decimal value));
        Assert.Equal(cleaned, rest);
        Assert.Equal(amount, value);
    }

    [Theory]
    [InlineData("rent 42.000", "rent", 42_000)]
    [InlineData("rent 42.000,50", "rent", 42_000.50)]
    [InlineData("water 42,50", "water", 42.50)]
    [InlineData("elektrik 340", "elektrik", 340)]
    public void ReadsTurkishFormattedAmounts(string text, string cleaned, decimal amount)
    {
        Assert.True(AmountHints.TryExtract(text, out string rest, out decimal value));
        Assert.Equal(cleaned, rest);
        Assert.Equal(amount, value);
    }

    [Theory]
    [InlineData("dinner ₺500", "dinner", 500)]
    [InlineData("dinner 500tl", "dinner", 500)]
    [InlineData("dinner 500 TL", "dinner", 500)]
    [InlineData("dinner 500₺", "dinner", 500)]
    [InlineData("500tl", "", 500)]
    [InlineData("dinner 3k tl", "dinner", 3_000)]
    public void ReadsCurrencyMarkers(string text, string cleaned, decimal amount)
    {
        Assert.True(AmountHints.TryExtract(text, out string rest, out decimal value));
        Assert.Equal(cleaned, rest);
        Assert.Equal(amount, value);
    }

    [Theory]
    [InlineData("42kk")]
    [InlineData("k42")]
    [InlineData("meet at 15:30")]
    [InlineData("₺500 electrician")]
    [InlineData("call mom")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("version 1.2.3.4k")]
    public void RejectsNonAmounts(string text)
    {
        Assert.False(AmountHints.TryExtract(text, out _, out _));
    }

    [Fact]
    public void PrefersTheTrailingPosition()
    {
        Assert.True(AmountHints.TryExtract("save 500 for rent 42k", out string rest, out decimal value));
        Assert.Equal("save 500 for rent", rest);
        Assert.Equal(42_000, value);
    }

    [Theory]
    [InlineData("I owe Ata Pinar total 1500TL", 1_500, "TRY")]
    [InlineData("dinner ₺500", 500, "TRY")]
    [InlineData("hosting $20", 20, "USD")]
    [InlineData("hosting 20 usd", 20, "USD")]
    [InlineData("flight €120", 120, "EUR")]
    [InlineData("flight 120 eur", 120, "EUR")]
    [InlineData("book £15", 15, "GBP")]
    [InlineData("rent 42k try", 42_000, "TRY")]
    [InlineData("sale $1.5k", 1_500, "USD")]
    public void ReadsCurrencies(string text, decimal amount, string currency)
    {
        AmountMatch? match = AmountHints.Match(text);

        Assert.NotNull(match);
        Assert.Equal(amount, match.Amount);
        Assert.Equal(currency, match.CurrencyCode);
    }

    [Theory]
    [InlineData("elektrik 340")]
    [InlineData("rent 42k")]
    public void LeavesCurrencyOpenWithoutAMarker(string text)
    {
        AmountMatch? match = AmountHints.Match(text);

        Assert.NotNull(match);
        Assert.Null(match.CurrencyCode);
    }

    [Fact]
    public void ReportsTheMatchedSpan()
    {
        AmountMatch? match = AmountHints.Match("rent 42k");

        Assert.NotNull(match);
        Assert.Equal("rent", match.Cleaned);
        Assert.Equal("42k", "rent 42k".Substring(match.Start, match.Length).Trim());
    }
}
