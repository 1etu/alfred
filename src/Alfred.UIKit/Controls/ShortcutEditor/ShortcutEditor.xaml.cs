using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alfred.Localization;
using Alfred.UIKit.Input;

namespace Alfred.UIKit.Controls;

public partial class ShortcutEditor : UserControl
{
    public static readonly DependencyProperty ShortcutProperty = DependencyProperty.Register(
        nameof(Shortcut), typeof(Shortcut), typeof(ShortcutEditor),
        new PropertyMetadata(null, OnShortcutChanged));

    public static readonly DependencyProperty RegistryProperty = DependencyProperty.Register(
        nameof(Registry), typeof(ShortcutRegistry), typeof(ShortcutEditor));

    public static readonly DependencyProperty ConflictProperty = DependencyProperty.Register(
        nameof(Conflict), typeof(string), typeof(ShortcutEditor));

    private bool _isCapturing;

    public ShortcutEditor()
    {
        InitializeComponent();
        Loaded += (_, _) => ShowBound();
    }

    public ObservableCollection<string> Caps { get; } = [];

    public Shortcut? Shortcut
    {
        get => (Shortcut?)GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }

    public ShortcutRegistry? Registry
    {
        get => (ShortcutRegistry?)GetValue(RegistryProperty);
        set => SetValue(RegistryProperty, value);
    }

    public string? Conflict
    {
        get => (string?)GetValue(ConflictProperty);
        set => SetValue(ConflictProperty, value);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        BeginCapture();
        e.Handled = true;
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        EndCapture();
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        base.OnPreviewKeyUp(e);

        if (_isCapturing)
        {
            ShowPending(Keyboard.Modifiers, Key.None);
            e.Handled = true;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (!_isCapturing || Shortcut is null || Registry is null)
        {
            return;
        }

        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        ModifierKeys modifiers = Keyboard.Modifiers;

        if (key == Key.Escape && modifiers == ModifierKeys.None)
        {
            EndCapture();
            return;
        }

        if (key is Key.Back or Key.Delete && modifiers == ModifierKeys.None)
        {
            Registry.Reset(Shortcut);
            EndCapture();
            return;
        }

        if (ShortcutGesture.IsModifier(key))
        {
            ShowPending(modifiers, Key.None);
            return;
        }

        if (ShortcutGesture.IsReserved(key, modifiers))
        {
            Conflict = LocalizationService.Text(LocalizationKeys.ShortcutReserved);
            ShowPending(modifiers, key);
            return;
        }

        if (ShortcutGesture.TryCreate(key, modifiers) is not KeyGesture gesture)
        {
            Conflict = LocalizationService.Text(LocalizationKeys.ShortcutNeedsModifier);
            ShowPending(modifiers, key);
            return;
        }

        if (Registry.TryRebind(Shortcut, gesture, out string? conflict))
        {
            EndCapture();
            return;
        }

        Conflict = conflict;
        ShowPending(modifiers, key);
    }

    private static void OnShortcutChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((ShortcutEditor)target).ShowBound();

    private void BeginCapture()
    {
        if (_isCapturing || Shortcut is null)
        {
            return;
        }

        _isCapturing = true;
        Shortcut.IsCapturing = true;
        Conflict = null;
        Keyboard.Focus(this);

        Surface.Background = (Brush)FindResource("SidebarRowSelected");
        Prompt.Visibility = Visibility.Visible;
        Caps.Clear();
    }

    private void EndCapture()
    {
        if (!_isCapturing)
        {
            return;
        }

        _isCapturing = false;

        Shortcut?.IsCapturing = false;

        Conflict = null;
        Surface.Background = Brushes.Transparent;
        Prompt.Visibility = Visibility.Collapsed;
        ShowBound();
    }

    private void ShowPending(ModifierKeys modifiers, Key key)
    {
        Caps.Clear();

        foreach (string cap in ShortcutGesture.Describe(modifiers, key))
        {
            Caps.Add(cap);
        }

        Prompt.Visibility = Caps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshConflict();
    }

    private void ShowBound()
    {
        if (_isCapturing)
        {
            return;
        }

        Caps.Clear();

        if (Shortcut is null)
        {
            return;
        }

        foreach (string cap in Shortcut.Keys)
        {
            Caps.Add(cap);
        }

        RefreshConflict();
    }

    private void RefreshConflict() =>
        ConflictText.Visibility = string.IsNullOrEmpty(Conflict) ? Visibility.Collapsed : Visibility.Visible;
}
