using System;
using System.Threading;

namespace LitespeedQuic
{
    /// <summary>
    /// This is a <see cref="SynchronizationContext"/> implementation that ensures code running on a Engine is threads correctly.
    /// </summary>
    public class EngineSynchronizationContext : SynchronizationContext
    {
        private readonly Engine _engine;

        /// <summary>
        /// Gets the engine for this synchronization context.
        /// </summary>
        public Engine Engine {
            get {
                return _engine;
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return new EngineSynchronizationContext(_engine);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _engine.PostToEngineThread(d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (_engine.IsEngineThread) {
                d(state);
            } else {
                throw new NotSupportedException();
            }
        }

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }

        internal EngineSynchronizationContext(Engine engine)
        {
            _engine = engine;
        }
    }
}