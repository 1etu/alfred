namespace Alfred.UIKit.Controls;

public sealed class ToolbarAction
{
    public ToolbarAction(string name, string glyphKey, Action invoke)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(glyphKey);
        ArgumentNullException.ThrowIfNull(invoke);

        Name = name;
        GlyphKey = glyphKey;
        Invoke = invoke;
    }

    public string Name { get; }

    public string GlyphKey { get; }

    public Action Invoke { get; }
}
