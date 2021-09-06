using System;
using System.Threading;

namespace Core
{
    public abstract class ADisposeBase : IDisposable, IReferencable
    {

        private int _counter = 1;

        public int ReferenceCount => _counter;
        public bool IsDisposed { get; private set; }

        protected virtual void Destroy()
        {
        }

        protected virtual void OnAddReference()
        {
        }

        protected virtual void OnReleaseReference()
        {
        }

        public int AddReference()
        {
            OnAddReference();
            int result = Interlocked.Increment(ref _counter);
            return result <= 1 ? throw new InvalidOperationException("Added a reference to a dispsoed object.") : result;
        }

        public int Release()
        {
            OnReleaseReference();
            int result = Interlocked.Decrement(ref _counter);
            if (result == 0)
            {
                Destroy();
                IsDisposed = true;
            }
            else if (result < 0)
            {
                throw new InvalidOperationException("Tried to release an already disposed object.");
            }
            return result;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Release();
            }
        }
    }
}
