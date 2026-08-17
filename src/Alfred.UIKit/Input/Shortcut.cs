using System.Windows.Input;

namespace Alfred.UIKit.Input;

public sealed class Shortcut : Observable
{
    private IReadOnlyList<string> _keys;

    internal Shortcut(string id, string name, string category, KeyGesture gesture, Action invoke)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(gesture);
        ArgumentNullException.ThrowIfNull(invoke);

        Id = id;
        Name = name;
        Category = category;
        DefaultGesture = gesture;
        Gesture = gesture;
        _keys = ShortcutGesture.Describe(gesture.Modifiers, gesture.Key);
        Command = new ShortcutCommand(invoke);
    }

    public string Id { get; }

    public string Name { get; }

    public string Category { get; }

    public KeyGesture DefaultGesture { get; }

    public ICommand Command { get; }

    public KeyGesture Gesture { get; private set; }

    public IReadOnlyList<string> Keys
    {
        get => _keys;
        private set => Set(ref _keys, value);
    }

    public IReadOnlyList<string> PendingKeys { get; internal set => Set(ref field, value); } = [];

    public bool IsCapturing { get; internal set => Set(ref field, value); }

    public bool IsCustomised =>
        Gesture.Key != DefaultGesture.Key || Gesture.Modifiers != DefaultGesture.Modifiers;

    internal void Apply(KeyGesture gesture)
    {
        Gesture = gesture;
        Keys = ShortcutGesture.Describe(gesture.Modifiers, gesture.Key);
        Raise(nameof(Gesture));
        Raise(nameof(IsCustomised));
    }
}
