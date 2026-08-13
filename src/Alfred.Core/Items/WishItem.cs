using Alfred.Core.Ledger;

namespace Alfred.Core.Items;

public sealed class WishItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public Money? Price { get; set; }

    public string? BrandSlug { get; set; }

    public string? Link { get; set; }

    public bool Acquired { get; set; }
}
