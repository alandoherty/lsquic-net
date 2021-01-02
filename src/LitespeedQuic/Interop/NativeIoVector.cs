using System;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeIoVector
    {
        public IntPtr Base;
        public IntPtr Size;
    }
}