namespace Alfred.UIKit.Controls;

public interface IToolbarHost
{
    IReadOnlyList<ToolbarAction> Actions { get; }

    string? PrimaryActionName { get; }

    void InvokePrimary();
}
