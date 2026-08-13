using System;
using System.Runtime.InteropServices;

namespace Alfred.Widgets.Providers;

internal sealed class ComServerRegistration : IDisposable
{
    private const uint LocalServerContext = 0x4;

    private const uint MultipleUse = 0x1;

    private readonly uint _cookie;

    private bool _isRevoked;

    private ComServerRegistration(uint cookie)
    {
        _cookie = cookie;
    }

    public static ComServerRegistration Register(Guid classId, IClassFactory factory)
    {
        var result = CoRegisterClassObject(classId, factory, LocalServerContext, MultipleUse, out var cookie);

        if (result != 0)
        {
            Marshal.ThrowExceptionForHR(result);
        }

        return new ComServerRegistration(cookie);
    }

    public void Dispose()
    {
        if (_isRevoked)
        {
            return;
        }

        _isRevoked = true;
        _ = CoRevokeClassObject(_cookie);
    }

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid classId,
        [MarshalAs(UnmanagedType.IUnknown)] object factory,
        uint classContext,
        uint flags,
        out uint cookie);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint cookie);
}
