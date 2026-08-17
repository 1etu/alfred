using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Alfred.Theme.Defaults;

public static partial class DefaultThemes
{
    public static Theme Dark { get; } = new()
    {
        Name = "Dark",
        Variant = ThemeVariant.Dark,
        Colors = new ReadOnlyDictionary<string, Color>(new Dictionary<string, Color>
        {
            [ThemeKeys.ShellBackground] = Rgb(0x171614),
            [ThemeKeys.SheetBackground] = Rgb(0x211F1E),
            [ThemeKeys.TitleBarBackground] = Rgb(0x171614),
            [ThemeKeys.Hairline] = Rgb(0x33312E),

            [ThemeKeys.TextPrimary] = Rgb(0xEDEAE6),
            [ThemeKeys.TextSecondary] = Rgb(0x9A958E),

            [ThemeKeys.SidebarBackground] = Rgb(0x171614),
            [ThemeKeys.SidebarText] = Rgb(0xC9C5BF),
            [ThemeKeys.SidebarBadge] = Rgb(0x8A857D),
            [ThemeKeys.SidebarBadgeSelected] = Rgb(0xA8A29A),
            [ThemeKeys.SidebarRowHover] = Argb(0x0DFFFFFF),
            [ThemeKeys.SidebarRowSelected] = Rgb(0x2B2927),
            [ThemeKeys.SidebarBackgroundGlass] = Argb(0x4D1A1918),
            [ThemeKeys.SheetHover] = Rgb(0x2A2826),

            [ThemeKeys.ChipPaymentsBack] = Rgb(0x3A2624),
            [ThemeKeys.ChipPaymentsText] = Rgb(0xF09B95),
            [ThemeKeys.ChipTodosBack] = Rgb(0x22303F),
            [ThemeKeys.ChipTodosText] = Rgb(0x8FB6F5),
            [ThemeKeys.ChipRemindersBack] = Rgb(0x32283E),
            [ThemeKeys.ChipRemindersText] = Rgb(0xC7A1EE),
            [ThemeKeys.ChipMealsBack] = Rgb(0x27331F),
            [ThemeKeys.ChipMealsText] = Rgb(0xA4D07E),
            [ThemeKeys.ChipPlansBack] = Rgb(0x2B2941),
            [ThemeKeys.ChipPlansText] = Rgb(0xA9A0F0),
            [ThemeKeys.ChipNeutralBack] = Rgb(0x2C2A28),
            [ThemeKeys.ChipNeutralText] = Rgb(0xA29D95),

            [ThemeKeys.StatBlueBack] = Rgb(0x232B38),
            [ThemeKeys.StatAmberBack] = Rgb(0x37301F),
            [ThemeKeys.StatGreenBack] = Rgb(0x25321F),

            [ThemeKeys.CardBackground] = Rgb(0x282725),
            [ThemeKeys.ChromeGlyph] = Rgb(0x9A958E),
            [ThemeKeys.Accent] = Rgb(0x4C8DFF),
            [ThemeKeys.AccentSoft] = Argb(0x2E4C8DFF),
            [ThemeKeys.TrashTint] = Rgb(0x9A958E),

            [ThemeKeys.SwitchTrackOff] = Rgb(0x3D3B37),
            [ThemeKeys.SwitchTrackOn] = Rgb(0x30D158),
            [ThemeKeys.SwitchKnob] = Rgb(0xFFFFFF),

            [ThemeKeys.FolderRed] = Rgb(0xFF6369),
            [ThemeKeys.FolderOrange] = Rgb(0xFF8B3E),
            [ThemeKeys.FolderYellow] = Rgb(0xFFC53D),
            [ThemeKeys.FolderGreen] = Rgb(0x3DD68C),
            [ThemeKeys.FolderTeal] = Rgb(0x25D0B4),
            [ThemeKeys.FolderBlue] = Rgb(0x6E9BFF),
            [ThemeKeys.FolderPurple] = Rgb(0xB784E0),
            [ThemeKeys.FolderPink] = Rgb(0xE86BC1),

            [ThemeKeys.CheckRing] = Rgb(0x57534C),
            [ThemeKeys.CheckFill] = Rgb(0x3B82F6),
            [ThemeKeys.MoneyIn] = Rgb(0x4ADE80),
            [ThemeKeys.MoneyOut] = Rgb(0xF87171),
            [ThemeKeys.Overdue] = Rgb(0xF87171),

            [ThemeKeys.ScrollThumbBrush] = Argb(0x4DFFFFFF),
            [ThemeKeys.ScrollThumbHoverBrush] = Argb(0x82FFFFFF),

            [ThemeKeys.SegmentTrack] = Rgb(0x2B2A27),
            [ThemeKeys.SegmentSelected] = Rgb(0x403E3A),

            [ThemeKeys.WindowButtonClose] = Rgb(0xFF5F57),
            [ThemeKeys.WindowButtonMinimize] = Rgb(0xFEBC2E),
            [ThemeKeys.WindowButtonZoom] = Rgb(0x28C840),
            [ThemeKeys.WindowButtonIdle] = Rgb(0x48453F),
            [ThemeKeys.WindowButtonGlyph] = Argb(0x99000000),
            [ThemeKeys.WindowButtonGlyphHighlight] = Argb(0x3DFFFFFF),
        }),
    };
}
