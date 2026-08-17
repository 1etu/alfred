using System.Collections.ObjectModel;
using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class WishesViewModel : Observable, IToolbarHost
{
    private readonly Vault _vault;

    public WishesViewModel(Vault vault)
    {
        _vault = vault;
        Actions =
        [
            new ToolbarAction("Copy list", "CopyGlyph", CopyToClipboard),
        ];

        Refresh();
    }

    public IReadOnlyList<ToolbarAction> Actions { get; }

    public string? PrimaryActionName => "New wish";

    public event EventHandler? PrimaryRequested;

    public void InvokePrimary() => PrimaryRequested?.Invoke(this, EventArgs.Empty);

    private void CopyToClipboard() =>
        Interop.Clipboards.Set(string.Join(Environment.NewLine, Rows.Select(row => $"- {row.Title}  {row.Price}")));

    public ObservableCollection<WishRow> Rows { get; } = [];

    public string Subtitle { get; private set; } = string.Empty;

    public bool IsEmpty => Rows.Count == 0;

    public void Add(string title, decimal? price, string? brandSlug)
    {
        _vault.Data.Wishes.Add(new WishItem
        {
            Title = title,
            Price = price is { } amount ? Money.Lira(amount) : null,
            BrandSlug = brandSlug,
        });

        _vault.Save();
        Refresh();
    }

    internal void Remove(WishItem wish)
    {
        Recycler.Delete(_vault.Data, wish);
        _vault.Save();
        Refresh();
    }

    internal void Persist()
    {
        _vault.Save();
        Refresh();
    }

    private void Refresh()
    {
        Rows.Clear();

        foreach (WishItem wish in _vault.Data.Wishes.OrderBy(wish => wish.Acquired))
        {
            Rows.Add(new WishRow(wish, this));
        }

        decimal wanted = _vault.Data.Wishes
            .Where(wish => !wish.Acquired && wish.Price is not null)
            .Sum(wish => wish.Price!.Value.Amount);

        Subtitle = wanted > 0
            ? $"Everything still wanted adds up to {MoneyFormat.Compact(Money.Lira(wanted))}"
            : "Things you want, with what they cost";

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
