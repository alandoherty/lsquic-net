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
            //Library.SetLogger(s => Console.Write(s));
            Library.SetLogLevel(LogLevel.Debug);
            
            try {
                // create a new client engine
                Engine engine = new Engine(EngineFlags.Http);
                
                Connection connection = await engine.ConnectAsync(new DnsEndPoint("google.co.uk", 443), QuicVersion.Q046);

                connection.SessionResumption += (o, e) => Console.WriteLine(Encoding.ASCII.GetString(e.Token));
                //Connection connection = await engine.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001));
                
                while (true) {
                    NativeMethods.lsquic_engine_process_conns(engine.Pointer);

                    AdvanceTick(engine, out int tick);
                    await Task.Delay(1);
                }
            } finally {
                Library.Cleanup();
            }
        }

        unsafe static bool AdvanceTick(Engine engine, out int diff)
        {
            int diffTemp;
            bool connectionsToProcess = NativeMethods.lsquic_engine_earliest_adv_tick(engine.Pointer, new IntPtr(&diffTemp)) == 1;
            diff = diffTemp;
            return connectionsToProcess;
        }
    }
}