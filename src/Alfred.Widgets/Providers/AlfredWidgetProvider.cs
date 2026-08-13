using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Alfred.Widgets.Cards;
using Alfred.Widgets.Snapshots;
using Microsoft.Windows.Widgets.Providers;

namespace Alfred.Widgets.Providers;

[Guid("28303D2C-B08F-4CDE-94CE-B0DDE78E0E80")]
internal sealed class AlfredWidgetProvider : IWidgetProvider, IWidgetProvider2
{
    public static readonly ManualResetEventSlim NoWidgetsRemain = new(false);

    private const string OpenVerb = "open";

    private static readonly Uri DefaultLaunchUri = new("alfred://open");

    private static readonly Lock Gate = new();

    private static readonly Dictionary<string, string> DefinitionIdsByWidgetId = new(StringComparer.Ordinal);

    public AlfredWidgetProvider()
    {
        foreach (var widgetInfo in WidgetManager.GetDefault().GetWidgetInfos())
        {
            Remember(widgetInfo.WidgetContext.Id, widgetInfo.WidgetContext.DefinitionId);
        }
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        Remember(widgetContext.Id, widgetContext.DefinitionId);
        RenderWidget(widgetContext.Id);
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        lock (Gate)
        {
            DefinitionIdsByWidgetId.Remove(widgetId);

            if (DefinitionIdsByWidgetId.Count == 0)
            {
                NoWidgetsRemain.Set();
            }
        }
    }

    public void Activate(WidgetContext widgetContext)
    {
        Remember(widgetContext.Id, widgetContext.DefinitionId);
        RenderWidget(widgetContext.Id);
    }

    public void Deactivate(string widgetId)
    {
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Remember(contextChangedArgs.WidgetContext.Id, contextChangedArgs.WidgetContext.DefinitionId);
        RenderWidget(contextChangedArgs.WidgetContext.Id);
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (!string.Equals(actionInvokedArgs.Verb, OpenVerb, StringComparison.Ordinal))
        {
            return;
        }

        OpenAlfred();
    }

    public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        RenderWidget(customizationRequestedArgs.WidgetContext.Id);
    }

    private static void Remember(string widgetId, string definitionId)
    {
        lock (Gate)
        {
            DefinitionIdsByWidgetId[widgetId] = definitionId;
            NoWidgetsRemain.Reset();
        }
    }

    private static string? FindDefinitionId(string widgetId)
    {
        lock (Gate)
        {
            return DefinitionIdsByWidgetId.TryGetValue(widgetId, out var definitionId) ? definitionId : null;
        }
    }

    private static void RenderWidget(string widgetId)
    {
        var definitionId = FindDefinitionId(widgetId);

        if (definitionId is null)
        {
            return;
        }

        var card = CardContent.Build(definitionId, SnapshotFile.Read());

        var options = new WidgetUpdateRequestOptions(widgetId)
        {
            Template = card.Template,
            Data = card.Data,
        };

        WidgetManager.GetDefault().UpdateWidget(options);
    }

    private static void OpenAlfred()
    {
        var launchUri = SnapshotFile.Read()?.LaunchUri ?? DefaultLaunchUri;

        var startInfo = new ProcessStartInfo(launchUri.AbsoluteUri)
        {
            UseShellExecute = true,
        };

        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Win32Exception)
        {
        }
    }
}
