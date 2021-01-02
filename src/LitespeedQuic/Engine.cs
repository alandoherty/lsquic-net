using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LitespeedQuic.Interop;

namespace LitespeedQuic
{
    public delegate void SessionResumptionEventHandler(object sender, SessionResumptionEventArgs e);
        
    /// <summary>
    /// Provides a wrapper around an lsquic engine, from here you can accept connections as a server or create connections as a client.
    /// </summary>
    public sealed class Engine : IDisposable
    {
        private readonly Thread _thread;
        private readonly IntPtr _ptr;
        private readonly IntPtr _apiPtr;
        private readonly IntPtr _callbacksPtr;
        private readonly EngineFlags _flags;
        private readonly EngineSynchronizationContext _syncContext;
        private readonly TaskFactory _factory;
        private readonly NativeEngineCallbacks _callbacks;
        private readonly NativeEngineApi _api;
        
        private BlockingCollection<SendOrPostData> _postQueue = new BlockingCollection<SendOrPostData>();
        private Dictionary<int, Connection> _connections = new Dictionary<int, Connection>();

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

        #region Threading
        struct SendOrPostData
        {
            public SendOrPostCallback Callback;
            public object State;
        }
        
        internal bool IsEngineThread => _thread == Thread.CurrentThread;

        internal void PostToEngineThread(SendOrPostCallback d, object state)
        {
            _postQueue.Add(new SendOrPostData() {
                Callback = d,
                State = state
            });
        }

        private void EngineThread()
        {
            // set the sync context
            SynchronizationContext.SetSynchronizationContext(_syncContext);
            
            while (true) {
                SendOrPostData data = _postQueue.Take();

                data.Callback(data.State);
            }
        }
        #endregion
        
        private IntPtr MarshalSocketAddressForIP(IPEndPoint endPoint) {
            if (endPoint.AddressFamily == AddressFamily.InterNetwork) {
                Span<byte> portBytes = stackalloc byte[sizeof(ushort)];
                BitConverter.TryWriteBytes(portBytes, (ushort) endPoint.Port);
                portBytes.Reverse();
                
                NativeSocketAddress4 sockAddr = default;
                sockAddr.Address = BitConverter.ToUInt32(endPoint.Address.GetAddressBytes(), 0);
                sockAddr.Port = BitConverter.ToUInt16(portBytes);
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
        /// <param name="sni">The server name information for TLS, if <see cref="DnsEndPoint"/> is used this can be automatically determined.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the endpoint is not supported.</exception>
        public async ValueTask<Connection> ConnectAsync(EndPoint endpoint, QuicVersion? version = null, string sni = null, CancellationToken cancellationToken = default) {
            if (IsServer)
                throw new InvalidOperationException("The engine is in server mode");
            
            // use the provided version or latest if value is null
            QuicVersion versionConnect = version ?? (QuicVersion) Constants.LatestVersion;
            
            // resolve provided endpoint into an IP endpoint if required, also validate the provided endpoint is workable
            if (endpoint is DnsEndPoint dnsEndpoint) {
                var addresses = await Dns.GetHostAddressesAsync(dnsEndpoint.Host).ConfigureAwait(false);
                
                if (sni == null)
                    sni = dnsEndpoint.Host;
                
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
            //socket.Blocking = false;
            socket.Connect((IPEndPoint)endpoint);
            
            // allocate connection id
            int id = 0;

            lock (_connections) {
                while (_connections.ContainsKey(id)) {
                    id++;
                }

                _connections[id] = null;
            }

            // get the local/remote pointers
            IntPtr localPtr = MarshalSocketAddressForIP((IPEndPoint)socket.LocalEndPoint);
            IntPtr remotePtr = MarshalSocketAddressForIP((IPEndPoint)socket.RemoteEndPoint);

            IntPtr conn;
            
            try {
                //TODO: session resumption
                //TODO: token
                conn = NativeMethods.lsquic_engine_connect(_ptr, versionConnect, localPtr, remotePtr,
                    new IntPtr(id),
                    new IntPtr(id), sni, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            } finally {
                Marshal.FreeHGlobal(localPtr);
                Marshal.FreeHGlobal(remotePtr);
            }
                
            _connections[id] = new Connection(this, id, conn, socket);
            return _connections[id];
        }

        private unsafe int OnPacketsOut(IntPtr packetsOutContextPtr, IntPtr outPacketsPtr, uint packetsOutCount)
        {
            NativePacketOut* packetOut = default;

            for (int i = 0; i < packetsOutCount; i++) {
                // get the packet structure at the base pointer + iteration
                packetOut = (NativePacketOut*)IntPtr.Add(outPacketsPtr, sizeof(NativePacketOut) * i);
                
                // get the connection by using the peer context (which is just an integer into a dictionary)
                Connection conn;
                
                lock(_connections)
                    conn = _connections[packetOut->PeerContext.ToInt32()];

                // send out each packet
                for (int j = 0; j < packetOut->IoVectorLength.ToInt32(); j++) {
                    NativeIoVector* vec = (NativeIoVector*)packetOut->IoVector;
                    var buffer = new ReadOnlySpan<byte>(vec->Base.ToPointer(), vec->Size.ToInt32());

                    conn.Socket.Send(buffer);
                }
            }
            
            return (int)packetsOutCount;
        }

        private IntPtr LookupCertificate(IntPtr lookupctx, IntPtr sockaddr, string sni) {
            throw new NotImplementedException();
        }

        #region Stream Callbacks
        private void OnHandshake(IntPtr connCtx, HandshakeStatus status) {
            Console.WriteLine($"[ENGINE] OnHandshake {connCtx} {status}");
        }

        private void OnNewToken(IntPtr conn, IntPtr token, IntPtr size) {
            throw new NotImplementedException();
        }

        private unsafe void OnSessionResumed(IntPtr connPtr, IntPtr tokenPtr, IntPtr size) {
            // get the connection by using the peer context (which is just an integer into a dictionary)
            Connection conn;
            IntPtr connCtx = NativeMethods.lsquic_conn_get_ctx(connPtr);
                
            lock(_connections)
                conn = _connections[connCtx.ToInt32()];
            
            // invoke handler
            conn.OnSessionResumption(new SessionResumptionEventArgs() {
                Token = new ReadOnlySpan<byte>(tokenPtr.ToPointer(), size.ToInt32())
            });
        }

        private void OnStreamClose(IntPtr stream, IntPtr streamctx) {
            Console.WriteLine($"[ENGINE] OnStreamClose {streamctx} {stream}");
        }

        private void OnStreamWrite(IntPtr stream, IntPtr streamctx) {
            Console.WriteLine($"[ENGINE] OnStreamWrite {streamctx} {stream}");
        }

        private void OnStreamRead(IntPtr stream, IntPtr streamctx) {
            Console.WriteLine($"[ENGINE] OnStreamRead {streamctx} {stream}");
        }

        private IntPtr OnNewStream(IntPtr interfacectx, IntPtr stream)
        {
            Console.WriteLine($"[ENGINE] OnNewStream {interfacectx} {stream}");
            //TODO: stream context
            return IntPtr.Zero;
        }
       

        private void OnConnectionClosed(IntPtr conn)
        {
            Console.WriteLine($"[ENGINE] OnConnectionClosed {conn}");
        }

        private IntPtr OnNewConnection(IntPtr interfaceContext, IntPtr conn) {
            Console.WriteLine($"[ENGINE] OnNewConnection {interfaceContext} {conn}");
            return IntPtr.Zero;
        }
        
        public void RemoveConnection(int id)
        {
            _connections.Remove(id);
        }
        #endregion
        
        /// <summary>
        /// Create a new lsquic engine.
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <param name="settings">The settings, if null the default settings will be used.</param>
        public Engine(EngineFlags flags, EngineSettings settings = null) {
            // create thread
            _thread = new Thread(EngineThread);
            _thread.IsBackground = true;
            _syncContext = new EngineSynchronizationContext(this);
            
            // create a factory
            SynchronizationContext previousCtx = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(_syncContext);
            _factory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
            SynchronizationContext.SetSynchronizationContext(previousCtx);
            
            // create callbacks, this must be stored in a field so the GC does not collect the delegates
            _callbacks = default;
            _callbacks.OnNewConnection = OnNewConnection;
            _callbacks.OnConnectionClosed = OnConnectionClosed;
            _callbacks.OnNewStream = OnNewStream;
            _callbacks.OnStreamRead = OnStreamRead;
            _callbacks.OnStreamWrite = OnStreamWrite;
            _callbacks.OnStreamClose = OnStreamClose;
            _callbacks.OnSessionResumed = OnSessionResumed;
            _callbacks.OnNewToken = OnNewToken;
            _callbacks.OnHandshake = OnHandshake;
            
            // marshal callback structure
            _callbacksPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeEngineCallbacks>());
            Marshal.StructureToPtr(_callbacks, _callbacksPtr, false);
            
            // create api, this must be stored in a field so the GC does not collect the delegates
            _api = default;
            _api.StreamInterfaces = _callbacksPtr;
            _api.StreamInterfacesContext = IntPtr.Zero;
            _api.OnPacketsOut = OnPacketsOut;
            _api.OnLookupCertificate = LookupCertificate;
            _api.LookupCertificateContext = IntPtr.Zero;
            _api.OnGetSslContext = ctx => IntPtr.Zero;

            // invoke LSQUIC to create engine
            _ptr = NativeMethods.lsquic_engine_new(flags, in _api);
            
            if (_ptr == IntPtr.Zero)
                throw new InvalidOperationException();
            
            // start thread
            _thread.Start();
        }
        
        #region Disposing
        private void ReleaseUnmanagedResources() {
            // destroy the engine
            NativeMethods.lsquic_engine_destroy(_ptr);
            
            // free callbacks
            Marshal.FreeHGlobal(_callbacksPtr);
            
            // free api
            Marshal.FreeHGlobal(_apiPtr);
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