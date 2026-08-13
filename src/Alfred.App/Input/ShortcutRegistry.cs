using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Alfred.App.Preferences;

namespace Alfred.App.Input;

public sealed class ShortcutRegistry
{
    private readonly ObservableCollection<Shortcut> _shortcuts = [];
    private readonly Dictionary<string, Shortcut> _byId = [];
    private readonly UserPreferences _preferences;
    private UIElement? _surface;

    public ShortcutRegistry(UserPreferences preferences)
    {
        _preferences = preferences;

        Registered = CollectionViewSource.GetDefaultView(_shortcuts);
        Registered.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Shortcut.Category)));
        Registered.SortDescriptions.Add(new SortDescription(nameof(Shortcut.Category), ListSortDirection.Ascending));
        Registered.SortDescriptions.Add(new SortDescription(nameof(Shortcut.Name), ListSortDirection.Ascending));
    }

    public ICollectionView Registered { get; }

    public Shortcut Register(string id, string name, string category, KeyGesture gesture, Action invoke)
    {
        if (_byId.ContainsKey(id))
        {
            throw new InvalidOperationException($"Shortcut '{id}' is already registered.");
        }

        Shortcut shortcut = new(id, name, category, gesture, invoke);

        if (_preferences.Shortcuts.TryGetValue(id, out string? stored) &&
            ShortcutGesture.Deserialize(stored) is KeyGesture custom &&
            FindByGesture(custom, shortcut) is null)
        {
            shortcut.Apply(custom);
        }

        if (FindByGesture(shortcut.Gesture, shortcut) is Shortcut taken)
        {
            throw new InvalidOperationException(
                $"{string.Join('+', shortcut.Keys)} is already bound to '{taken.Name}'.");
        }

        _byId.Add(id, shortcut);
        _shortcuts.Add(shortcut);
        Rebuild();

        return shortcut;
    }

    public void Attach(UIElement surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        if (_surface is not null)
        {
            throw new InvalidOperationException("The registry is already attached to a surface.");
        }

        _surface = surface;
        Rebuild();
    }

    public bool TryRebind(Shortcut shortcut, KeyGesture gesture, out string? conflict)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        ArgumentNullException.ThrowIfNull(gesture);

        if (FindByGesture(gesture, shortcut) is Shortcut taken)
        {
            conflict = $"Already used by {taken.Name}";
            return false;
        }

        shortcut.Apply(gesture);
        Persist(shortcut);
        Rebuild();

        conflict = null;
        return true;
    }

    public void Reset(Shortcut shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut);

        if (FindByGesture(shortcut.DefaultGesture, shortcut) is not null)
        {
            return;
        }

        shortcut.Apply(shortcut.DefaultGesture);
        Persist(shortcut);
        Rebuild();
    }

    private void Persist(Shortcut shortcut)
    {
        if (shortcut.IsCustomised)
        {
            _preferences.Shortcuts[shortcut.Id] = ShortcutGesture.Serialize(shortcut.Gesture);
        }
        else
        {
            _preferences.Shortcuts.Remove(shortcut.Id);
        }

        PreferencesStore.Save(_preferences);
    }

    private void Rebuild()
    {
        if (_surface is null)
        {
            return;
        }

        _surface.InputBindings.Clear();

        foreach (Shortcut shortcut in _shortcuts)
        {
            _surface.InputBindings.Add(new KeyBinding(shortcut.Command, shortcut.Gesture));
        }
    }

    private Shortcut? FindByGesture(KeyGesture gesture, Shortcut exclude)
    {
        foreach (Shortcut shortcut in _shortcuts)
        {
            if (ReferenceEquals(shortcut, exclude))
            {
                continue;
            }

            if (shortcut.Gesture.Key == gesture.Key && shortcut.Gesture.Modifiers == gesture.Modifiers)
            {
                return shortcut;
            }
        }

        return null;
    }
}
