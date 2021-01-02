using System;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
   
    public static class NativeMethods
    {
        public const string SharedLibraryPath = "liblsquic.so";

        #region Globals
        /// <summary>
        /// Initialize LSQUIC.  This must be called before any other LSQUIC function is called.  Returns 0 on success and -1 on failure.
        /// </summary>
        /// <param name="flags"></param>
        [DllImport(SharedLibraryPath)]
        public static extern int lsquic_global_init(GlobalFlags flags);
        
        /// <summary>
        /// Clean up global state created by @ref lsquic_global_init.  Should be called after all LSQUIC engine instances are gone.
        /// </summary>
        [DllImport(SharedLibraryPath)]
        public static extern void lsquic_global_cleanup();
        #endregion

        /// <summary>
        /// The engine can be instantiated either in server mode (when <see cref="EngineFlags.Server"/> is set) or client mode. If you need both server and client in your program, create two engines (or as many as you’d like).
        /// Specifying <see cref="EngineFlags.Http"/> flag enables the HTTP functionality: HTTP/2-like for Google QUIC connections and HTTP/3 functionality for IETF QUIC connections.
        /// </summary>
        /// <param name="flags">The engine flags.</param>
        /// <param name="api">The engine API.</param>
        /// <returns></returns>
        [DllImport(SharedLibraryPath)]
        public static extern IntPtr lsquic_engine_new(EngineFlags flags, in NativeEngineApi api);
        
        [DllImport(SharedLibraryPath)]
        public static extern IntPtr lsquic_engine_connect(IntPtr engine, QuicVersion version, IntPtr localAddr, IntPtr remoteAddr, IntPtr peerCtx, IntPtr connCtx,
            [MarshalAs(UnmanagedType.LPStr)]string sni, ushort basePlpMtu, IntPtr sessionResume, IntPtr sessionResumeLen, IntPtr token, IntPtr tokenLen);
        
        [DllImport(SharedLibraryPath)]
        public static extern int lsquic_engine_packet_in(IntPtr engine, IntPtr data, UIntPtr size, IntPtr localAddr,
            IntPtr peerAddr, IntPtr peerCtx, int ecn);
        
        [DllImport(SharedLibraryPath)]
        public static extern void lsquic_engine_cooldown(IntPtr engine);

        [DllImport(SharedLibraryPath)]
        public static extern IntPtr lsquic_conn_id(IntPtr conn);

        [DllImport(SharedLibraryPath)]
        public static extern IntPtr lsquic_conn_get_ctx(IntPtr conn);
        
        [DllImport(SharedLibraryPath)]
        public static extern IntPtr lsquic_conn_set_ctx(IntPtr conn, IntPtr ctx);
        
        [DllImport(SharedLibraryPath)]
        public static extern void lsquic_engine_destroy(IntPtr engine);

        [DllImport(SharedLibraryPath)]
        public static extern int lsquic_engine_earliest_adv_tick(IntPtr engine, IntPtr diff);

        [DllImport(SharedLibraryPath)]
        public static extern void lsquic_engine_process_conns(IntPtr engine);      
        
        #region Logging
        [DllImport(SharedLibraryPath)]
        public static extern void lsquic_logger_init(IntPtr callbacks, IntPtr loggerCtx, LoggerTimestampStyle timestampStyle);
        
        [DllImport(SharedLibraryPath)]
        public static extern int lsquic_set_log_level([MarshalAs(UnmanagedType.LPStr)]string level);
        
        [DllImport(SharedLibraryPath)]
        public static extern int lsquic_logger_lopt([MarshalAs(UnmanagedType.LPStr)]string specs);
        #endregion
    }
}