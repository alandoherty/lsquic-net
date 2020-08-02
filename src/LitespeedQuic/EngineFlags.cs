using System;

namespace LitespeedQuic
{
    /// <summary>
    /// Defines flags for initialising.
    /// </summary>
    [Flags]
    public enum EngineFlags : uint
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// The engine is created in server mode.
        /// </summary>
        Server = (1 << 0),
        
        /// <summary>
        /// The engine is created with HTTP support.
        /// </summary>
        Http = (1 << 1),
        
        /// <summary>
        /// The engine is created in server and HTTP mode.
        /// </summary>
        HttpServer = Server | Http
    }
}