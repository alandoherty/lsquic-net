using System;

namespace LitespeedQuic
{
    /// <summary>
    /// Defines the global flags used in library initialization.
    /// </summary>
    [Flags]
    public enum GlobalFlags : uint
    {
        /// <summary>
        /// Enable the usage of clients globally.
        /// </summary>
        Client = (1 << 0),
        
        /// <summary>
        /// Enable the usage of servers globally.
        /// </summary>
        Server = (1 << 1),
        
        /// <summary>
        /// Enables the usage of both client and servers globally.
        /// </summary>
        ClientAndServer = Client | Server
    }
}