using System;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    public delegate int LogCallback(IntPtr loggerCtx, IntPtr buffer, IntPtr bufferLen);

    /// <summary>
    /// Provides an interface struct for engine stream callbacks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeLoggingCallbacks
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public LogCallback Log;
    }
}