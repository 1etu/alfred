using System.Collections.ObjectModel;
using Alfred.Core.Items;
using Alfred.Core.Search;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Suggest;

namespace Alfred.App.ViewModels;

public sealed class MealsViewModel : PageViewModel
{
    private readonly Vault _vault;
    private DateOnly _selectedDay;

    public MealsViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavMeals), "MealsIcon")
    {
        _vault = vault;
        _selectedDay = DateOnly.FromDateTime(DateTime.Now);
        Suggestions = new MealHistorySource(vault);

        _vault.Changed += (_, _) => BuildSlots();
        BuildSlots();
    }

    public ObservableCollection<SlotModel> Slots { get; } = [];

    public ISuggestionSource Suggestions { get; }

    public string SelectedDayLabel => _selectedDay == DateOnly.FromDateTime(DateTime.Now)
        ? LocalizationService.Text(LocalizationKeys.NavToday)
        : _selectedDay.ToString("dddd, d MMMM", LocalizationService.Current.Culture);

    public bool IsFutureDay => _selectedDay > DateOnly.FromDateTime(DateTime.Now);

    public DateOnly SelectedDay
    {
        get => _selectedDay;
        set
        {
            if (Set(ref _selectedDay, value))
            {
                BuildSlots();
                Raise(nameof(SelectedDayLabel));
                Raise(nameof(IsFutureDay));
            }
        }
    }

    internal void Add(MealSlot slot, string title)
    {
        _vault.Data.Meals.Add(new Meal { Title = title, Day = _selectedDay, Slot = slot });
        _vault.Save();
    }

    internal void Remove(Meal meal)
    {
        Recycler.Delete(_vault.Data, meal);
        _vault.Save();
    }

    internal void Persist() => _vault.Save();

    private void BuildSlots()
    {
        Slots.Clear();

        foreach (MealSlot slot in Enum.GetValues<MealSlot>())
        {
            Slots.Add(new SlotModel(slot, this, _vault.Data.Meals
                .Where(meal => meal.Day == _selectedDay && meal.Slot == slot)));
        }
    }

    internal static string SlotName(MealSlot slot) => LocalizationService.Text(slot switch
    {
        MealSlot.Breakfast => LocalizationKeys.MealBreakfast,
        MealSlot.Lunch => LocalizationKeys.MealLunch,
        MealSlot.Dinner => LocalizationKeys.MealDinner,
        _ => LocalizationKeys.MealSnack,
    });

    public sealed class SlotModel
    {
        private readonly MealsViewModel _owner;

        public SlotModel(MealSlot slot, MealsViewModel owner, IEnumerable<Meal> meals)
        {
            Slot = slot;
            _owner = owner;
            Meals = [.. meals.Select(meal => new MealRow(meal, owner))];
        }

        public MealSlot Slot { get; }

        public string Title => SlotName(Slot);

        public ObservableCollection<MealRow> Meals { get; }

        public void Add(string title) => _owner.Add(Slot, title);
    }

    public sealed class MealRow : Observable
    {
        private readonly MealsViewModel _owner;

        public MealRow(Meal meal, MealsViewModel owner)
        {
            Meal = meal;
            _owner = owner;
            CanMarkEaten = !owner.IsFutureDay;
        }

        public Meal Meal { get; }

        public string Title => Meal.Title;

        public bool CanMarkEaten { get; }

        public bool IsEaten
        {
            get => Meal.Eaten;
            set
            {
                Meal.Eaten = value;
                _owner.Persist();
            }
        }

        public void Remove() => _owner.Remove(Meal);
    }

    private sealed class MealHistorySource : ISuggestionSource
    {
        private readonly Vault _vault;

        public MealHistorySource(Vault vault)
        {
            _vault = vault;
        }

        public IReadOnlyList<Suggestion> Suggest(string query, int limit) =>
            [.. _vault.Data.Meals
                .GroupBy(meal => meal.Title, StringComparer.OrdinalIgnoreCase)
                .Select(titles => (Title: titles.First().Title, Count: titles.Count(), Score: FuzzyMatcher.Score(query, titles.Key)))
                .Where(entry => entry.Score > 0)
                .OrderByDescending(entry => entry.Score)
                .ThenByDescending(entry => entry.Count)
                .Take(limit)
                .Select(entry => new Suggestion(entry.Title, null, null, null))];
    }
}
