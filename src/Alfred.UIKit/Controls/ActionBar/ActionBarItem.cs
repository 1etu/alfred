namespace Alfred.UIKit.Controls;

public sealed class ActionBarItem
{
    public ActionBarItem(string name, string glyphKey, Action invoke, bool isProminent = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(glyphKey);
        ArgumentNullException.ThrowIfNull(invoke);

        Name = name;
        GlyphKey = glyphKey;
        Invoke = invoke;
        IsProminent = isProminent;
    }

    public string Name { get; }

    public string GlyphKey { get; }

    public Action Invoke { get; }

    public bool IsProminent { get; }
}
