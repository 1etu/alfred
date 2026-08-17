using Alfred.Core.Storage;
using Alfred.Localization;

namespace Alfred.App.ViewModels;

public sealed class MealsViewModel : PageViewModel
{
    public MealsViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavMeals), "MealsIcon")
    {
        Vault = vault;
    }

    internal Vault Vault { get; }
}
