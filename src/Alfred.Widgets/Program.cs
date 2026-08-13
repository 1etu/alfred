using System;
using Alfred.Widgets.Providers;
using WinRT;

namespace Alfred.Widgets;

internal static class Program
{
    private const string ComServerArgument = "-RegisterProcessAsComServer";

    [MTAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        if (!string.Equals(args[0], ComServerArgument, StringComparison.Ordinal))
        {
            return;
        }

        ComWrappersSupport.InitializeComWrappers();

        using var registration = ComServerRegistration.Register(
            typeof(AlfredWidgetProvider).GUID,
            new WidgetProviderFactory());

        AlfredWidgetProvider.NoWidgetsRemain.Wait();
    }
}
