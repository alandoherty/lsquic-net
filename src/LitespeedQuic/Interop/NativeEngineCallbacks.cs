using System;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    public delegate IntPtr NewConnectionCallback(IntPtr interfaceCtx, IntPtr conn);
    public delegate void ConnectionClosedCallback(IntPtr conn);
    public delegate IntPtr NewStreamCallback(IntPtr interfaceCtx, IntPtr stream);
    public delegate void StreamCallback(IntPtr stream, IntPtr streamCtx);
    public delegate void HandshakeCallback(IntPtr interfaceCtx, HandshakeStatus status);
    public delegate void GoawayCallback(IntPtr conn);
    public delegate void NewTokenCallback(IntPtr conn, IntPtr token, IntPtr size);
    public delegate void SessionResumedCallback(IntPtr conn, IntPtr token, IntPtr size);
    
    /// <summary>
    /// Provides an interface struct for engine stream callbacks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeEngineCallbacks
    {
        
        /// <summary>
        /// Called when a new connection has been created. In server mode, this means that the handshake has been successful.
        /// In client mode, on the other hand, this callback is called as soon as connection object is created inside the engine, but before the handshake is done.
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public NewConnectionCallback OnNewConnection;
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public GoawayCallback OnGoaway;
        
        /// <summary>
        /// Connection is closed.
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ConnectionClosedCallback OnConnectionClosed;
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public NewStreamCallback OnNewStream;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public StreamCallback OnStreamRead;
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public StreamCallback OnStreamWrite;
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public StreamCallback OnStreamClose;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public HandshakeCallback OnHandshake;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public NewTokenCallback OnNewToken;
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public SessionResumedCallback OnSessionResumed;
    }
}