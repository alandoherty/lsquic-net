using System;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePacketOut
    {
        public IntPtr IoVector;
        public IntPtr IoVectorLength;
        public IntPtr LocalAddress;
        public IntPtr DestinationAddress;
        public IntPtr PeerContext;
        public IntPtr ConnectionContext;
        public int Ecn;
    }
}