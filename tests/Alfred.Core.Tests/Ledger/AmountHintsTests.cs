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
}
