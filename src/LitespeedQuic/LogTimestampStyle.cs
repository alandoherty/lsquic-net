namespace LitespeedQuic
{
    public enum LoggerTimestampStyle
    {
        /// <summary>
        /// No timestamp is generated.
        /// </summary>
        None,
        
        /// <summary>
        /// The timestamp consists of 24 hours, minutes, seconds, and milliseconds. Example: 13:43:46.671
        /// </summary>
        HHMMSSMS,
        
        /// <summary>
        /// Like above, plus date, e.g: 2017-03-21 13:43:46.671
        /// </summary>
        YYYYMMDD_HHMMSSMS,
        
        /// <summary>
        /// This is Chrome-like timestamp used by proto-quic. The timestamp includes month, date, hours, minutes, seconds, and microseconds.
        ///
        /// Example: 1223/104613.946956 (instead of 12/23 10:46:13.946956).
        ///
        /// This is to facilitate reading two logs side-by-side.
        /// </summary>
        ChromeLike,
        
        /// <summary>
        /// The timestamp consists of 24 hours, minutes, seconds, and microseconds. Example: 13:43:46.671123
        /// </summary>
        HHMMSSUS,
        
        /// <summary>
        /// Date and time using microsecond resolution, e.g: 2017-03-21 13:43:46.671123
        /// </summary>
        YYYYMMDD_HHMMSSUS
    }
}