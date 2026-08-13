using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Items;
using Alfred.Core.Storage;

namespace Alfred.App.ViewModels;

public sealed class MealsViewModel : Observable
{
    private readonly Vault _vault;
    private DateOnly _selectedDay;

    public MealsViewModel(Vault vault)
    {
        _vault = vault;
        _selectedDay = DateOnly.FromDateTime(DateTime.Now);
        BuildWeek();
        BuildSlots();
    }

    public ObservableCollection<DayChip> Week { get; } = [];

    public ObservableCollection<SlotModel> Slots { get; } = [];

    public string SelectedDayLabel => _selectedDay == DateOnly.FromDateTime(DateTime.Now)
        ? "Today"
        : _selectedDay.ToString("dddd, d MMMM", CultureInfo.InvariantCulture);

    internal DateOnly SelectedDay
    {
        get => _selectedDay;
        set
        {
            if (Set(ref _selectedDay, value))
            {
                foreach (DayChip chip in Week)
                {
                    chip.RefreshSelected();
                }

                BuildSlots();
                Raise(nameof(SelectedDayLabel));
            }
        }
    }

    internal void Add(MealSlot slot, string title)
    {
        _vault.Data.Meals.Add(new Meal { Title = title, Day = _selectedDay, Slot = slot });
        _vault.Save();
        BuildSlots();
    }

    internal void Remove(Meal meal)
    {
        Recycler.Delete(_vault.Data, meal);
        _vault.Save();
        BuildSlots();
    }

    internal void Persist() => _vault.Save();

    private void BuildWeek()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        for (int offset = 0; offset < 7; offset++)
        {
            Week.Add(new DayChip(today.AddDays(offset), this));
        }
    }

    private void BuildSlots()
    {
        Slots.Clear();

        foreach (MealSlot slot in Enum.GetValues<MealSlot>())
        {
            Slots.Add(new SlotModel(slot, this, _vault.Data.Meals
                .Where(meal => meal.Day == _selectedDay && meal.Slot == slot)));
        }
    }

    public sealed class DayChip : Observable
    {
        private readonly MealsViewModel _owner;

        public DayChip(DateOnly day, MealsViewModel owner)
        {
            Day = day;
            _owner = owner;
        }

        public DateOnly Day { get; }

        public string Name => Day.ToString("ddd", CultureInfo.InvariantCulture);

        public string Number => Day.Day.ToString(CultureInfo.InvariantCulture);

        public bool IsSelected
        {
            get => _owner.SelectedDay == Day;
            set
            {
                if (value)
                {
                    _owner.SelectedDay = Day;
                }
            }
        }

        internal void RefreshSelected() => Raise(nameof(IsSelected));
    }

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

        public string Title => Slot.ToString();

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
        }

        public Meal Meal { get; }

        public string Title => Meal.Title;

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
}
