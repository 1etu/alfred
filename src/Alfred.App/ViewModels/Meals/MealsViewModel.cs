using Alfred.Core.Storage;

namespace Alfred.App.ViewModels;

public sealed class MealsViewModel : PageViewModel
{
    public MealsViewModel(Vault vault)
        : base("Meals", "MealsIcon")
    {
        Vault = vault;
    }

    internal Vault Vault { get; }
}
