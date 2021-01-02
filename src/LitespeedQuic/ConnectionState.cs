namespace LitespeedQuic
{
    /// <summary>
    /// Defines the connection state.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// The connection is currently performing a handshake and is unusable, this is only relevant for client connections.
        /// </summary>
        Handshake,
        
        /// <summary>
        /// The connection is open.
        /// </summary>
        Open,
        
        /// <summary>
        /// The connection is being wound down, this is only applicable to HTTP/3 and GQUIC. New streams cannot be generated.
        /// </summary>
        GoAway,
        
        /// <summary>
        /// The connection is closed completely.
        /// </summary>
        Closed
    }
}