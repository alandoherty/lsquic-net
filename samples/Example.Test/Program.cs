using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LitespeedQuic;
using LitespeedQuic.Interop;

namespace Example.Test
{
    class Program
    {
        static async Task Main(string[] args) {
            // initialise library
            Library.Initialize();
            Library.SetLogger(s => Console.Write(s));
            Library.SetLogLevel(LogLevel.Debug);
            
            try {
                // create a new client engine
                Engine engine = new Engine(EngineFlags.None);

                Connection connection = await engine.ConnectAsync(new DnsEndPoint("google.co.uk", 443));

                while (true) {
                    NativeMethods.lsquic_engine_process_conns(engine.Pointer);
                    await Task.Delay(5);
                }
            } finally {
                Library.Cleanup();
            }
        }
    }
}