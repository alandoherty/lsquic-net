using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LitespeedQuic.Interop;

namespace LitespeedQuic
{
    /// <summary>
    /// Provides a wrapper around a lsquic connection.
    /// </summary>
    public class Connection : IDisposable
    {
        private readonly Engine _engine;
        private readonly IntPtr _ptr;
        private readonly Socket _socket;
        private readonly int _id;
        private readonly byte[] _receiveBuffer;
        
        private EndPoint _receiveEndpoint;
        private ConnectionState _state;
        
        #region Events
        /// <summary>
        /// Invoked when the connection receives data that can be used to resume the connection later.
        /// </summary>
        public event SessionResumptionEventHandler SessionResumption;

        internal void OnSessionResumption(SessionResumptionEventArgs e)
        {
            SessionResumption?.Invoke(this, e);
        }
        #endregion

        /// <summary>
        /// Gets the connection ID.
        /// </summary>
        public byte[] ConnectionId {
            get {
                IntPtr connIdPtr = NativeMethods.lsquic_conn_id(_ptr);

                byte[] bytes = new byte[GetConnectionIdSize(connIdPtr)];
                TryWriteConnectionId(connIdPtr, bytes.AsSpan(), out _);
                return bytes;
            }
        }

        /// <summary>
        /// Gets the socket.
        /// </summary>
        internal Socket Socket {
            get {
                return _socket;
            }
        }

        internal void HandleGoaway()
        {
            // this is called when a goaway is received
            _state = ConnectionState.GoAway;
            
            //TODO: probably more stuff here
        }
        
        internal void HandleClose() {
            // this is called when LSQUIC finally closes the connection, either for user reasons or related
            // to networking/etc
            _state = ConnectionState.Closed;
            
            // remove from parent
            _engine.RemoveConnection(_id);
        }
        
        public void Dispose() {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe byte GetConnectionIdSize(IntPtr idPtr)
        {
            if (idPtr == IntPtr.Zero)
                throw new NullReferenceException("The connection pointer is invalid");
            
            // check the size of the ID
            //NOTE: this is a fast uint8, which might be larger than 8 bits on some arch/configs
            return *(byte*)idPtr.ToPointer();
        }
        
        private unsafe bool TryWriteConnectionId(IntPtr idPtr, Span<byte> bytes, out int bytesWritten)
        {
            // check the size of the ID
            byte idSize = GetConnectionIdSize(idPtr);

            if (idSize > bytes.Length) {
                bytesWritten = default;
                return false;
            }

            // copy connection data to span
            new Span<byte>(IntPtr.Add(idPtr, 1).ToPointer(), idSize)
                .CopyTo(bytes);
            
            bytesWritten = idSize;
            return true;
        }
        
        /// <summary>
        /// Try and write the connection ID to the provided buffer, the typical maximum is 20 bytes.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bytesWritten">The number of bytes written.</param>
        /// <returns>If the bytes were successfully written.</returns>
        public bool TryWriteConnectionId(Span<byte> buffer, out int bytesWritten)
        {
            // get pointer to connection ID
            IntPtr idPtr = NativeMethods.lsquic_conn_id(_ptr);

            return TryWriteConnectionId(idPtr, buffer, out bytesWritten);
        }
        
        unsafe void PacketIn(ReadOnlySpan<byte> buffer, IPEndPoint localEndpoint, IPEndPoint remoteEndpoint)
        {
            fixed (byte* b = buffer) {
                NativeSocketAddress4 localAddr = new NativeSocketAddress4(localEndpoint);
                NativeSocketAddress4 peerAddr= new NativeSocketAddress4(remoteEndpoint);
            
                NativeMethods.lsquic_engine_packet_in(_engine.Pointer, new IntPtr(b), new UIntPtr((uint)buffer.Length),
                    new IntPtr(&localAddr), new IntPtr(&peerAddr), new IntPtr(_id), 0);
            }
        }

        public Connection(Engine engine, int id, IntPtr ptr, Socket socket) {
            _socket = socket;
            _ptr = ptr;
            _id = id;
            _engine = engine;
            _receiveBuffer = new byte[8192];
            _receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
            _socket.BeginReceiveMessageFrom(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ref _receiveEndpoint, Callback, null);
        }

        private void Callback(IAsyncResult ar)
        {
            SocketFlags flags = SocketFlags.None;
            int result = _socket.EndReceiveMessageFrom(ar, ref flags, ref _receiveEndpoint, out IPPacketInformation _);

            PacketIn(_receiveBuffer.AsSpan(0, result), (IPEndPoint)_socket.LocalEndPoint, (IPEndPoint)_receiveEndpoint);
            
            // end receive and begin a new one
            _receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
            _socket.BeginReceiveMessageFrom(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ref _receiveEndpoint, Callback, null);
        }
    }
}