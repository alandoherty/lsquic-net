using System;

namespace LitespeedQuic
{
    /// <summary>
    /// Provides constants for the lsquic library.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// A special version number to indicate the latest version that the library supports.
        /// </summary>
        public static readonly uint LatestVersion = (uint)Enum.GetValues(typeof(QuicVersion)).Length;
        
        /// <summary>
        /// The mask of supported Quic versions.
        /// </summary>
        public static readonly uint SupportedVersionMask = (uint)((1 << Enum.GetValues(typeof(QuicVersion)).Length) - 1);

        /// <summary>
        /// The mask of experimental Quic versions.
        /// </summary>
        public static readonly uint ExperimentalVersionMask = (1 << (int) QuicVersion.VersionNegotiation);

        /// <summary>
        /// The mask of deprecated versions.
        /// </summary>
        public static readonly uint DeprecatedVersionMask = 0;
        
        /// <summary>
        /// The default version mask.
        /// </summary>
        public static readonly uint DefaultVersionMask =  Constants.SupportedVersionMask & ~Constants.DeprecatedVersionMask &
                                                          ~Constants.ExperimentalVersionMask;

        /// <summary>
        /// The minimum flow control window is set to 16KB on both peers, this is the amount that can be sent before the handshake is completed.
        /// </summary>
        public static readonly int MinimumFlowControlWindow = (16 * 1024);
    }
}