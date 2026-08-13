using System;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace Alfred.Widgets.Providers;

internal sealed class WidgetProviderFactory : IClassFactory
{
    private const int ClassNoAggregation = unchecked((int)0x80040110);

    private const int NoInterface = unchecked((int)0x80004002);

    private static readonly Guid UnknownInterfaceId = new("00000000-0000-0000-C000-000000000046");

    public int CreateInstance(IntPtr outerUnknown, ref Guid interfaceId, out IntPtr instance)
    {
        instance = IntPtr.Zero;

        if (outerUnknown != IntPtr.Zero)
        {
            return ClassNoAggregation;
        }

        if (interfaceId != typeof(AlfredWidgetProvider).GUID && interfaceId != UnknownInterfaceId)
        {
            return NoInterface;
        }

        instance = MarshalInspectable<IWidgetProvider>.FromManaged(new AlfredWidgetProvider());
        return 0;
    }

    public int LockServer(bool shouldLock) => 0;
}
