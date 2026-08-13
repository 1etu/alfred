using System;
using System.Runtime.InteropServices;

namespace Alfred.Widgets.Providers;

[ComImport]
[Guid("00000001-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr outerUnknown, ref Guid interfaceId, out IntPtr instance);

    [PreserveSig]
    int LockServer(bool shouldLock);
}
