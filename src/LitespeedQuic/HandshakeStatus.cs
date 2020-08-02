namespace LitespeedQuic
{
    /// <summary>
    /// Defines the status of a handshake.
    /// </summary>
    public enum HandshakeStatus
    {
        /// <summary>
        /// The handshake failed.
        /// </summary>
        Fail,
        
        /// <summary>
        /// The handshake succeeded without session resumption.
        /// </summary>
        Ok,
        
        /// <summary>
        /// The handshake succeeded with session resumption.
        /// </summary>
        ResumedOk,
        
        /// <summary>
        /// Session resumption failed.  Retry the connection without session resumption.
        /// </summary>
        ResumedFail
    }
}