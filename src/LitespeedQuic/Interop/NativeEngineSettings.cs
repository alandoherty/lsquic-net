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
    }
}