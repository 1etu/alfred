using System.Collections.ObjectModel;
using Alfred.App.Interop;
using Alfred.App.Preferences;
using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class WishesViewModel : PageViewModel
{
    private readonly Vault _vault;
    private readonly UserPreferences _preferences;

    public WishesViewModel(Vault vault, UserPreferences preferences)
        : base(LocalizationService.Text(LocalizationKeys.NavWishList), "WishListIcon")
    {
        _vault = vault;
        _preferences = preferences;
        Actions =
        [
            new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionCopy), "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public override IReadOnlyList<ActionBarItem> Actions { get; }

    public ObservableCollection<WishRow> Rows { get; } = [];

    public string Subtitle { get; private set; } = string.Empty;

    public bool IsEmpty => Rows.Count == 0;

    public void Add(string title, decimal? price, string? brandSlug, string? currency)
    {
        _vault.Data.Wishes.Add(new WishItem
        {
            Title = title,
            Price = price is { } amount ? new Money(amount, currency ?? _preferences.DefaultCurrency) : null,
            BrandSlug = brandSlug,
        });

        _vault.Save();
    }

    internal void Remove(WishItem wish)
    {
        Recycler.Delete(_vault.Data, wish);
        _vault.Save();
    }

    internal void Persist()
    {
        _vault.Save();
        Refresh();
    }

    private void CopyToClipboard() =>
        Clipboards.Set(string.Join(Environment.NewLine, Rows.Select(row => $"- {row.Title}  {row.Price}")));

    private void Refresh()
    {
        Rows.Clear();

        foreach (WishItem wish in _vault.Data.Wishes.OrderBy(wish => wish.Acquired))
        {
            Rows.Add(new WishRow(wish, this));
        }

        decimal wanted = _vault.Data.Wishes
            .Where(wish => !wish.Acquired && wish.Price is { } price && price.Currency == _preferences.DefaultCurrency)
            .Sum(wish => wish.Price!.Value.Amount);

        Subtitle = wanted > 0
            ? LocalizationService.Text(
                LocalizationKeys.WishesTotal,
                MoneyFormat.Compact(new Money(wanted, _preferences.DefaultCurrency)))
            : LocalizationService.Text(LocalizationKeys.WishesCaption);

        Raise(nameof(Subtitle));
        Raise(nameof(IsEmpty));
    }

    public sealed class WishRow : Observable
    {
        private readonly WishesViewModel _owner;

        public WishRow(WishItem wish, WishesViewModel owner)
        {
            Wish = wish;
            _owner = owner;
        }

        public WishItem Wish { get; }

        public string Title => Wish.Title;

        public string? Price => Wish.Price is { } price ? MoneyFormat.Compact(price) : null;

        public string? BrandSlug => Wish.BrandSlug;

        public bool IsAcquired
        {
            get => Wish.Acquired;
            set
            {
                Wish.Acquired = value;
                _owner.Persist();
            }
        }

        public void Remove() => _owner.Remove(Wish);
    }
}
