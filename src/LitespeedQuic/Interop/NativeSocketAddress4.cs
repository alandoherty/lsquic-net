using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeSocketAddress4
    {
        public short Family;
        public ushort Port;
        public uint Address;
        public ulong Zero;

        public IPEndPoint EndPoint {
            get {
                Span<byte> bytes = stackalloc byte[4];
                
                // get port
                BitConverter.TryWriteBytes(bytes, Port);
            
                if (BitConverter.IsLittleEndian)
                    bytes.Slice(0,2).Reverse();
                
                // get address
                BitConverter.TryWriteBytes(bytes, Address);
                
                return new IPEndPoint(new IPAddress(bytes), BitConverter.ToUInt16(bytes.Slice(0, 2)));
            }
        }

        public NativeSocketAddress4(IPEndPoint ipEndPoint)
        {
            // set address family
            if (ipEndPoint.AddressFamily == AddressFamily.InterNetwork) {
                Family = 2;
            } else {
                throw new NotSupportedException("The address family is not supported");
            }
            
            // allocate some bytes for temporary usage
            Span<byte> bytes = stackalloc byte[4];
            
            // set port (network host order)
            BitConverter.TryWriteBytes(bytes, (ushort) ipEndPoint.Port);
            
            if (BitConverter.IsLittleEndian)
                bytes.Slice(0,2).Reverse();

            Port = BitConverter.ToUInt16(bytes.Slice(0,2));
            
            // set address
            ipEndPoint.Address.TryWriteBytes(bytes, out _);
            Address = BitConverter.ToUInt32(bytes);
            Zero = 0;
        }
    }
}