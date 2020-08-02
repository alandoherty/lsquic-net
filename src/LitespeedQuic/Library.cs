using System;
using System.Runtime.InteropServices;
using System.Text;
using LitespeedQuic.Interop;

namespace LitespeedQuic
{
    /// <summary>
    /// Library-wide methods for initialization, cleanup and logging.
    /// </summary>
    public static class Library
    {
        private static volatile IntPtr _loggingCallbacks;
        private static volatile bool _libraryInit;
        private static Action<string> _logger;
        
        /// <summary>
        /// Sets the library-wide log options.
        /// </summary>
        /// <param name="options">The log options, see the lsquic documentation.</param>
        /// <returns>If successful.</returns>
        public static bool SetLogOptions(string options) {
            if (!_libraryInit)
                throw new InvalidOperationException("The library must be initialized first");
            
            return NativeMethods.lsquic_logger_lopt(options) == 0;
        }
        
        /// <summary>
        /// Sets the library-wide log level.
        /// </summary>
        /// <param name="level">The level, either debug, info, notice, warning, error, alert, emerg or crit. This is case sensitive.</param>
        /// <returns>If successful.</returns>
        public static bool SetLogLevel(string level) {
            if (!_libraryInit)
                throw new InvalidOperationException("The library must be initialized first");
            
            return NativeMethods.lsquic_set_log_level(level) == 0;
        }

        /// <summary>
        /// Sets the library-wide log level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>If successful.</returns>
        public static bool SetLogLevel(LogLevel level) {
            switch (level) {
                case LogLevel.Debug:
                    return SetLogLevel("debug");
                case LogLevel.Information:
                    return SetLogLevel("info");
                case LogLevel.Notice:
                    return SetLogLevel("notice");
                case LogLevel.Warning:
                    return SetLogLevel("warning");
                case LogLevel.Error:
                    return SetLogLevel("error");
                case LogLevel.Alert:
                    return SetLogLevel("alert");
                case LogLevel.Emergency:
                    return SetLogLevel("emerg");
                case LogLevel.Critical:
                    return SetLogLevel("crit");
                default:
                    throw new InvalidOperationException("The log level is invalid");
            }
        }
        
        /// <summary>
        /// Initializes the library, this must be called before using any classes.
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <returns>If successful.</returns>
        public static bool Initialize(GlobalFlags flags = GlobalFlags.ClientAndServer) {
            if (NativeMethods.lsquic_global_init(flags) != 0)
                return false;
            
            _libraryInit = true;
            return true;
        }
        
        /// <summary>
        /// Initializes the library with logging, this must be called before using any classes.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="timestampStyle">The timestamp style.</param>
        public static void SetLogger(Action<string> logger, LoggerTimestampStyle timestampStyle = LoggerTimestampStyle.ChromeLike) {
            if (!_libraryInit)
                throw new InvalidOperationException("The library must be initialized first");

            // set logger
            _logger = logger;

            // if we haven't got logging callbacks allocated we have initialised the logger yet
            if (_loggingCallbacks == IntPtr.Zero) {
                // create callback structure 
                NativeLoggingCallbacks loggingCallbacks = default;
                loggingCallbacks.Log = LogHandler;

                // allocate memory for the marshalled structure
                _loggingCallbacks = Marshal.AllocHGlobal(Marshal.SizeOf<NativeLoggingCallbacks>());

                // marshal the callbacks into a structure and store in the heap
                Marshal.StructureToPtr(loggingCallbacks, _loggingCallbacks, false);

                // initialise logger
                NativeMethods.lsquic_logger_init(_loggingCallbacks, IntPtr.Zero, timestampStyle);
            }
        }

        private static int LogHandler(IntPtr ctx, IntPtr buffer, IntPtr len) {
#if NETSTANDARD1_6
            string logMsg = Marshal.PtrToStringAnsi(buffer, len.ToInt32());
#elif NETSTANDARD2_1
            string logMsg = Marshal.PtrToStringUTF8(buffer, len.ToInt32());
#else
#error Not supported for platform
#endif

            // call logger
            _logger?.Invoke(logMsg);

            return 0;
        }

        /// <summary>
        /// Cleanup the library, this should be called for a clean exit.
        /// </summary>
        public static void Cleanup() {
            // cleanup logger
            if (_loggingCallbacks != IntPtr.Zero) {
                Marshal.FreeHGlobal(_loggingCallbacks);
                _loggingCallbacks = IntPtr.Zero;
            }
            
            // cleanup library
            if (_libraryInit) {
                NativeMethods.lsquic_global_cleanup();
                _libraryInit = false;
            }
        }
    }
}