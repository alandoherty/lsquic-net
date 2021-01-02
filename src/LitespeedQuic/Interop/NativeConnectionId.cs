using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeConnectionId
    {
        public byte Length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Bytes;
    }
}