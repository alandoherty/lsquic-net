namespace LitespeedQuic
{
   /// <summary>
   /// Defines the QUIC version available in the library.
   /// </summary>
   public enum QuicVersion : uint
   {
      /// <summary>
      /// Q043.  Support for processing PRIORITY frames.  Since this library has supported PRIORITY frames from the beginning, this version is
      /// exactly the same as LSQVER_042.
      /// </summary>
      Q043,

      /// <summary>
      /// Q046.  Use IETF Draft-17 compatible packet headers.
      /// </summary>
      Q046,

      /// <summary>
      /// Q050.  Variable-length QUIC server connection IDs.  Use CRYPTO frames for handshake.  IETF header format matching invariants-06.
      /// Packet number encryption.  Initial packets are obfuscated.
      /// </summary>
      Q050,

      /// <summary>
      /// IETF QUIC Draft-27
      /// </summary>
      ID27,

      /// <summary>
      /// IETF QUIC Draft-28
      /// </summary>
      ID28,

      /// <summary>
      /// IETF QUIC Draft-29
      /// </summary>
      ID29,

      /// <summary>
      /// Special version to trigger version negotiation. [draft-ietf-quic-transport-11], Section 3.
      /// </summary>
      VersionNegotiation
   }
}