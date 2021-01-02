using System;

namespace LitespeedQuic
{
    /// <summary>
    /// Contains event data for session resumption, this can be used to connect quicker next time.
    /// </summary>
    public ref struct SessionResumptionEventArgs
    {
        /// <summary>
        /// The session resumption data.
        /// </summary>
        public ReadOnlySpan<byte> Token;
    }
}