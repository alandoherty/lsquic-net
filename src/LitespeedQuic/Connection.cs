using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace LitespeedQuic
{
    /// <summary>
    /// Provides a wrapper around a lsquic connection.
    /// </summary>
    public class Connection : IDisposable
    {
        private readonly IntPtr _ptr;
        private readonly Socket _socket;
        private GCHandle _handle;
        
        public void Dispose() {
            throw new NotImplementedException();
        }

        public Connection(IntPtr ptr, Socket socket) {
            _socket = socket;
            _ptr = ptr;
        }
    }
}