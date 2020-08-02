using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeSocketAddress4
    {
        public short Family;
        public ushort Port;
        public uint Address;
        public ulong Zero;
    }
}