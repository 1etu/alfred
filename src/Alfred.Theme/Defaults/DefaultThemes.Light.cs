using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Alfred.Theme.Defaults;

public static partial class DefaultThemes
{
    public static Theme Light { get; } = new()
    {
        Name = "Light",
        Variant = ThemeVariant.Light,
        Colors = new ReadOnlyDictionary<string, Color>(new Dictionary<string, Color>
        {
            [ThemeKeys.ShellBackground] = Rgb(0xF1F1F3),
            [ThemeKeys.SheetBackground] = Rgb(0xFFFFFF),
            [ThemeKeys.TitleBarBackground] = Rgb(0xF1F1F3),
            [ThemeKeys.Hairline] = Rgb(0xEFEFF1),

            [ThemeKeys.TextPrimary] = Rgb(0x1D1D1F),
            [ThemeKeys.TextSecondary] = Rgb(0x66686E),

            [ThemeKeys.SidebarBackground] = Rgb(0xF1F1F3),
            [ThemeKeys.SidebarText] = Rgb(0x3A4048),
            [ThemeKeys.SidebarBadge] = Rgb(0x66686E),
            [ThemeKeys.SidebarBadgeSelected] = Rgb(0x5D636D),
            [ThemeKeys.SidebarRowHover] = Argb(0x0A000000),
            [ThemeKeys.SidebarRowSelected] = Rgb(0xFFFFFF),
            [ThemeKeys.SidebarBackgroundGlass] = Argb(0x4DF2F2F1),
            [ThemeKeys.SheetHover] = Rgb(0xF4F4F6),

            [ThemeKeys.ChipPaymentsBack] = Rgb(0xFDEBEA),
            [ThemeKeys.ChipPaymentsText] = Rgb(0xC2453F),
            [ThemeKeys.ChipTodosBack] = Rgb(0xE8F0FE),
            [ThemeKeys.ChipTodosText] = Rgb(0x2F63C7),
            [ThemeKeys.ChipRemindersBack] = Rgb(0xF3EBFD),
            [ThemeKeys.ChipRemindersText] = Rgb(0x7A3FB8),
            [ThemeKeys.ChipMealsBack] = Rgb(0xEAF6E6),
            [ThemeKeys.ChipMealsText] = Rgb(0x4C8A2E),
            [ThemeKeys.ChipPlansBack] = Rgb(0xEEEDFD),
            [ThemeKeys.ChipPlansText] = Rgb(0x5A4FCF),
            [ThemeKeys.ChipNeutralBack] = Rgb(0xF1F1F3),
            [ThemeKeys.ChipNeutralText] = Rgb(0x6E7076),

            [ThemeKeys.StatBlueBack] = Rgb(0xEEF4FE),
            [ThemeKeys.StatAmberBack] = Rgb(0xFDF3E2),
            [ThemeKeys.StatGreenBack] = Rgb(0xEBF6EC),

            [ThemeKeys.CardBackground] = Rgb(0xFFFFFF),
            [ThemeKeys.ChromeGlyph] = Rgb(0x6B7178),
            [ThemeKeys.Accent] = Rgb(0x3574F0),
            [ThemeKeys.AccentSoft] = Argb(0x243574F0),
            [ThemeKeys.TrashTint] = Rgb(0x8A9099),

            [ThemeKeys.SwitchTrackOff] = Rgb(0xD8D8D6),
            [ThemeKeys.SwitchTrackOn] = Rgb(0x34C759),
            [ThemeKeys.SwitchKnob] = Rgb(0xFFFFFF),

            [ThemeKeys.FolderRed] = Rgb(0xE5484D),
            [ThemeKeys.FolderOrange] = Rgb(0xF76B15),
            [ThemeKeys.FolderYellow] = Rgb(0xF5A623),
            [ThemeKeys.FolderGreen] = Rgb(0x30A46C),
            [ThemeKeys.FolderTeal] = Rgb(0x12A594),
            [ThemeKeys.FolderBlue] = Rgb(0x3B82F6),
            [ThemeKeys.FolderPurple] = Rgb(0x8E4EC6),
            [ThemeKeys.FolderPink] = Rgb(0xD6409F),

            [ThemeKeys.CheckRing] = Rgb(0xC4C7CC),
            [ThemeKeys.CheckFill] = Rgb(0x3B82F6),
            [ThemeKeys.MoneyIn] = Rgb(0x1F9D4D),
            [ThemeKeys.MoneyOut] = Rgb(0xD64541),
            [ThemeKeys.Overdue] = Rgb(0xD64541),

            [ThemeKeys.ScrollThumbBrush] = Argb(0x42000000),
            [ThemeKeys.ScrollThumbHoverBrush] = Argb(0x6E000000),

            [ThemeKeys.SegmentTrack] = Rgb(0xEAEAE9),
            [ThemeKeys.SegmentSelected] = Rgb(0xFFFFFF),

            [ThemeKeys.WindowButtonClose] = Rgb(0xFF5F57),
            [ThemeKeys.WindowButtonMinimize] = Rgb(0xFEBC2E),
            [ThemeKeys.WindowButtonZoom] = Rgb(0x28C840),
            [ThemeKeys.WindowButtonIdle] = Rgb(0xDDDCDA),
            [ThemeKeys.WindowButtonGlyph] = Argb(0x99000000),
            [ThemeKeys.WindowButtonGlyphHighlight] = Argb(0x3DFFFFFF),
        }),
    };
}
