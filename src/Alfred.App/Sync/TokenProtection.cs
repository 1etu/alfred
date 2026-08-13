using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Alfred.App.Sync;

internal static partial class TokenProtection
{
    private const uint UiForbiddenFlag = 0x1;

    public static byte[] Protect(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Transform(value, isProtecting: true);
    }

    public static byte[] Unprotect(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Transform(value, isProtecting: false);
    }

    private static byte[] Transform(byte[] value, bool isProtecting)
    {
        GCHandle pin = GCHandle.Alloc(value, GCHandleType.Pinned);
        try
        {
            var input = new Blob { Length = (uint)value.Length, Bytes = pin.AddrOfPinnedObject() };
            var output = default(Blob);
            bool succeeded = isProtecting
                ? CryptProtectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbiddenFlag, ref output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbiddenFlag, ref output);
            if (!succeeded)
            {
                throw new CryptographicException(Marshal.GetLastPInvokeError());
            }

            return CopyAndRelease(output);
        }
        finally
        {
            pin.Free();
        }
    }

    private static byte[] CopyAndRelease(Blob blob)
    {
        try
        {
            byte[] result = new byte[blob.Length];
            Marshal.Copy(blob.Bytes, result, 0, result.Length);
            return result;
        }
        finally
        {
            LocalFree(blob.Bytes);
        }
    }

    [LibraryImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptProtectData(
        ref Blob input,
        IntPtr description,
        IntPtr entropy,
        IntPtr reserved,
        IntPtr prompt,
        uint flags,
        ref Blob output);

    [LibraryImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptUnprotectData(
        ref Blob input,
        IntPtr description,
        IntPtr entropy,
        IntPtr reserved,
        IntPtr prompt,
        uint flags,
        ref Blob output);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr LocalFree(IntPtr memory);

    [StructLayout(LayoutKind.Sequential)]
    private struct Blob
    {
        public uint Length;
        public IntPtr Bytes;
    }
}
