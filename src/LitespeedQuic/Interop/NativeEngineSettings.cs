using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeEngineSettings
    {
        public uint VersionMask;
        public uint DefaultConnectionFlowWindow;
        public uint DefaultStreamFlowWindow;
        public uint MaximumStreamFlowWindow;
        public uint MaximumIncomingStreams;
        public ulong HandshakeTimeout;
        public ulong IdleConnectionTimeout;
        public int SilentClose;
        public uint MaximumHeaderListSize;
        public string UserAgentId;
        public uint MaximumIncomingInchoate;
        public int SupportPush;
    }
}