namespace Alfred.Core.Ledger;

public sealed record Category(string Id, string Name, CashFlow Flow);

public static class Categories
{
    public static IReadOnlyList<Category> All { get; } =
    [
        new("housing", "Housing", CashFlow.Out),
        new("utilities", "Utilities", CashFlow.Out),
        new("groceries", "Groceries", CashFlow.Out),
        new("dining", "Dining", CashFlow.Out),
        new("transport", "Transport", CashFlow.Out),
        new("health", "Health", CashFlow.Out),
        new("fun", "Fun", CashFlow.Out),
        new("family", "Family", CashFlow.Out),
        new("debt", "Debt", CashFlow.Out),
        new("smokes", "Smokes", CashFlow.Out),
        new("gifts", "Gifts", CashFlow.Out),
        new("work", "Work", CashFlow.Out),
        new("salary", "Salary", CashFlow.In),
        new("freelance", "Freelance", CashFlow.In),
        new("other-income", "Other income", CashFlow.In),
    ];
}
