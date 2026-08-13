namespace Alfred.Core.Items;

public enum MealSlot
{
    Breakfast,
    Lunch,
    Dinner,
    Snack,
}

public sealed class Meal
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public required DateOnly Day { get; set; }

    public required MealSlot Slot { get; set; }

    public bool Eaten { get; set; }
}
