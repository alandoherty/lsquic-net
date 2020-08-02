using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LitespeedQuic.Interop;

namespace LitespeedQuic
{
    /// <summary>
    /// Provides a wrapper around an lsquic engine, from here you can accept connections as a server or create connections as a client.
    /// </summary>
    public sealed class Engine : IDisposable
    {
        private readonly IntPtr _ptr;
        private readonly IntPtr _callbacksPtr;
        private readonly EngineFlags _flags;
        private readonly Socket _socket;

        /// <summary>
        /// Gets the native engine pointer.
        /// </summary>
        public IntPtr Pointer => _ptr;

        /// <summary>
        /// Gets if this is a client engine.
        /// </summary>
        public bool IsClient => (_flags & EngineFlags.Server) == 0;
        
        /// <summary>
        /// Gets if this is a server engine.
        /// </summary>
        public bool IsServer => !IsClient;

        private IntPtr MarshalSocketAddressForIP(IPEndPoint endPoint) {
            if (endPoint.AddressFamily == AddressFamily.InterNetwork) {
                NativeSocketAddress4 sockAddr = default;
                sockAddr.Address = BitConverter.ToUInt32(endPoint.Address.GetAddressBytes(), 0);
                sockAddr.Port = (ushort)endPoint.Port;
                sockAddr.Family = 2;
                
                // allocate and marshal
                IntPtr marshalPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeSocketAddress4>());
                Marshal.StructureToPtr(sockAddr, marshalPtr, false);

                return marshalPtr;
            } else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6) {
                //TODO: ipv6 support
                throw new NotImplementedException();
            } else {
                throw new NotSupportedException("The provided address is of an unsupported IP family");
            }
        }

        /// <summary>
        /// Connects to the provided endpoint with the engine.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="version"></param>
        /// <param name="sni"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the endpoint is not supported.</exception>
        public async Task<Connection> ConnectAsync(EndPoint endpoint, QuicVersion? version = null, string sni = null, CancellationToken cancellationToken = default) {
            if (IsServer)
                throw new InvalidOperationException("The engine is in server mode");
            
            // use the provided version or latest if value is null
            QuicVersion versionConnect = version ?? (QuicVersion) Constants.LatestVersion;
            
            // resolve provided endpoint into an IP endpoint if required, also validate the provided endpoint is workable
            if (endpoint is DnsEndPoint dnsEndpoint) {
                var addresses = await Dns.GetHostAddressesAsync(dnsEndpoint.Host).ConfigureAwait(false);
                
                if (addresses.Length == 0)
                    throw new Exception("Unable to resolve any addresses from the provided endpoint");

                if (dnsEndpoint.AddressFamily == AddressFamily.Unspecified) {
                    endpoint = new IPEndPoint(addresses.First(), dnsEndpoint.Port);
                } else if (dnsEndpoint.AddressFamily == AddressFamily.InterNetwork) {
                    if (addresses.All(a => a.AddressFamily != AddressFamily.InterNetwork))
                        throw new Exception("Unable to resolve any addresses for the specified endpoint family");
                    
                    endpoint = new IPEndPoint(addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork), dnsEndpoint.Port);
                } else if (dnsEndpoint.AddressFamily == AddressFamily.InterNetworkV6) {
                    if (addresses.All(a => a.AddressFamily != AddressFamily.InterNetworkV6))
                        throw new Exception("Unable to resolve any addresses for the specified endpoint family");
                    
                    endpoint = new IPEndPoint(addresses.First(a => a.AddressFamily == AddressFamily.InterNetworkV6), dnsEndpoint.Port);
                } else {
                    throw new NotSupportedException("The specified endpoint family is not supported");
                }
            } else if (endpoint is IPEndPoint) {
            } else {
                throw new NotSupportedException("The provided endpoint is not supported");
            }
            
            // create a udp client
            Socket socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect((IPEndPoint)endpoint);
            
            // get the local/remote pointers
            IntPtr localPtr = MarshalSocketAddressForIP((IPEndPoint)socket.LocalEndPoint);
            IntPtr remotePtr = MarshalSocketAddressForIP((IPEndPoint)socket.RemoteEndPoint);

            IntPtr conn;
            
            try {
                //TODO: session resumption
                //TODO: token
                conn = NativeMethods.lsquic_engine_connect(_ptr, versionConnect, localPtr, remotePtr,
                    IntPtr.Zero,
                    IntPtr.Zero, null, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            } finally {
                Marshal.FreeHGlobal(localPtr);
                Marshal.FreeHGlobal(remotePtr);
            }

            return new Connection(conn, socket);
        }
        
        /// <summary>
        /// Create a new lsquic engine.
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <param name="settings">The settings, if null the default settings will be used.</param>
        public Engine(EngineFlags flags, EngineSettings settings = null) {
            // create callbacks
            NativeEngineCallbacks callbacks = default;
            callbacks.OnNewConnection = OnNewConnection;
            callbacks.OnConnectionClosed = OnConnectionClosed;
            callbacks.OnNewStream = OnNewStream;
            callbacks.OnStreamRead = OnStreamRead;
            callbacks.OnStreamWrite = OnStreamWrite;
            callbacks.OnStreamClose = OnStreamClose;
            callbacks.OnSessionResumed = OnSessionResumed;
            callbacks.OnNewToken = OnNewToken;
            callbacks.OnHandshake = OnHandshake;
            
            // marshal callback structure
            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeEngineCallbacks>());
            Marshal.StructureToPtr(callbacks, _callbacksPtr, false);
            
            // create api structure
            NativeEngineApi api = default;
            api.StreamInterfaces = _callbacksPtr;
            api.StreamInterfacesContext = IntPtr.Zero;
            api.OnPacketsOut = OnPacketsOut;
            api.OnLookupCertificate = LookupCertificate;
            api.LookupCertificateContext = IntPtr.Zero;
            api.OnGetSslContext = ctx => IntPtr.Zero;

            _ptr = NativeMethods.lsquic_engine_new(flags, in api);
            
            if (_ptr == IntPtr.Zero)
                throw new InvalidOperationException();
        }

        private int OnPacketsOut(IntPtr packetsoutctx, IntPtr outpec, uint packetsout) {
            throw new NotImplementedException();
        }

        private IntPtr LookupCertificate(IntPtr lookupctx, IntPtr sockaddr, string sni) {
            throw new NotImplementedException();
        }

        #region Stream Callbacks
        private void OnHandshake(IntPtr interfacectx, HandshakeStatus status) {
            throw new NotImplementedException();
        }

        private void OnNewToken(IntPtr conn, IntPtr token, IntPtr size) {
            throw new NotImplementedException();
        }

        private void OnSessionResumed(IntPtr conn, IntPtr token, IntPtr size) {
            throw new NotImplementedException();
        }

        private void OnStreamClose(IntPtr stream, IntPtr streamctx) {
            throw new NotImplementedException();
        }

        private void OnStreamWrite(IntPtr stream, IntPtr streamctx) {
            throw new NotImplementedException();
        }

        private void OnStreamRead(IntPtr stream, IntPtr streamctx) {
            throw new NotImplementedException();
        }

        private IntPtr OnNewStream(IntPtr interfacectx, IntPtr stream) {
            throw new NotImplementedException();
        }
       

        private void OnConnectionClosed(IntPtr conn) {
            throw new NotImplementedException();
        }

        private IntPtr OnNewConnection(IntPtr interfacectx, IntPtr conn) {
            //throw new NotImplementedException();
            return IntPtr.Zero;
        }
        #endregion
        
        #region Disposing
        private void ReleaseUnmanagedResources() {
            // destroy the engine
            NativeMethods.lsquic_engine_destroy(_ptr);
            
            // free callbacks
            Marshal.FreeHGlobal(_callbacksPtr);
        }
        
        /// <inheritdoc/>
        public void Dispose() {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Engine() {
            ReleaseUnmanagedResources();
        }
        #endregion
    }
}