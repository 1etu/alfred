using System.Windows.Input;

namespace Alfred.UIKit.Input;

internal sealed class ShortcutCommand : ICommand
{
    private readonly Action _invoke;

    public ShortcutCommand(Action invoke)
    {
        _invoke = invoke;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _invoke();
}
