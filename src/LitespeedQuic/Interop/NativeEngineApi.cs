using System;
using System.Runtime.InteropServices;

namespace LitespeedQuic.Interop
{
    public delegate int PacketsOutCallback(IntPtr packetsOutCtx, IntPtr outpec, uint packetsOut);
    public delegate IntPtr LookupCertificateCallback (IntPtr lookupCtx, IntPtr sockAddr, string sni);
    public delegate IntPtr GetSslContextCallback (IntPtr peerCtx);
    public delegate IntPtr UpdateConnectionIdsCallback (IntPtr cidCtx, IntPtr peerCtxs, IntPtr cids, uint cidCount);
    public delegate int VerifyCertificateCallback (IntPtr verifyCtx, IntPtr  chain);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeEngineApi
    {
        public IntPtr Settings;
        
        public IntPtr StreamInterfaces;
        public IntPtr StreamInterfacesContext;
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public PacketsOutCallback OnPacketsOut;
        public IntPtr PacketsOutContext;
       
        
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public LookupCertificateCallback OnLookupCertificate;
        public IntPtr LookupCertificateContext;

        [MarshalAs(UnmanagedType.FunctionPtr)] 
        public GetSslContextCallback OnGetSslContext;
        
        public IntPtr HashInterfaces;
        public IntPtr HashInterfacesContext;
        
        public IntPtr MemoryInterfaces;
        public IntPtr MemoryInterfacesContext;

        [MarshalAs(UnmanagedType.FunctionPtr)] 
        public UpdateConnectionIdsCallback NewConnectionIds;
        
        [MarshalAs(UnmanagedType.FunctionPtr)] 
        public UpdateConnectionIdsCallback LiveConnectionIds;
        
        [MarshalAs(UnmanagedType.FunctionPtr)] 
        public UpdateConnectionIdsCallback OldConnectionIds;
        
        public IntPtr ConnectionIdsContext;
        
        public VerifyCertificateCallback OnVerifyCertificate;
        public IntPtr VerifyCertificateCallback;
        
        public IntPtr HeaderInterfaces;
        public IntPtr HeaderInterfacesContext;

        public IntPtr KeylogInterfaces;
        public IntPtr KeylogInterfacesContext;

        public IntPtr AlpnString;
    }
}