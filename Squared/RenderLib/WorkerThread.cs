﻿#pragma warning disable 0420 // a reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace Squared.Render.Internal {
    public class WorkerThread : IDisposable {
        public readonly Thread Thread;
        public readonly Action<WorkerThread> Function;

        public volatile object Tag = null;

        private volatile int _PendingWork = 0;
        private volatile int _ThreadRunning = 0;
        private volatile int _ThreadWaiting = 0;
        private volatile Exception _PendingError = null;

        private readonly AutoResetEvent _WakeSignal = new AutoResetEvent(false);
        private readonly ManualResetEventSlim _CompletedSignal = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _StartedSignal = new ManualResetEventSlim(false);

        public WorkerThread (Action<WorkerThread> function) {
            Function = function;

            Thread = new Thread(WorkerFn);
            Thread.IsBackground = true;
            Thread.Start();
        }

        public void RequestWork () {
            if (_ThreadWaiting == 0)
                WaitForPendingWork();

            Interlocked.Increment(ref _PendingWork);

            if (_ThreadRunning == 0)
                _StartedSignal.Wait();

            _CompletedSignal.Reset();
            _WakeSignal.Set();
        }

        public void WaitForPendingWork () {
            Exception pe = Interlocked.Exchange(ref _PendingError, null);
            if (pe != null)
                throw pe;

            while ((_ThreadWaiting == 0) || (_PendingWork != 0))
                _CompletedSignal.Wait();
        }

        private void WorkerFn () {
            Interlocked.Increment(ref _ThreadWaiting);
            while (true) {
                if (Interlocked.Exchange(ref _ThreadRunning, 1) == 0)
                    _StartedSignal.Set();

                _WakeSignal.WaitOne();
                Interlocked.Decrement(ref _ThreadWaiting);

                while (_PendingWork > 0) {
                    try {
                        Function(this);
                    } catch (Exception ex) {
                        if (Debugger.IsAttached)
                            Debugger.Break();

                        _PendingError = new Exception("An error occurred in a worker thread", ex);
                    }

                    Interlocked.Decrement(ref _PendingWork);
                }

                Interlocked.Increment(ref _ThreadWaiting);
                _CompletedSignal.Set();
            }
        }

        public void Dispose () {
#if !PSM
            Thread.Abort();
#endif
            
            _WakeSignal.Set();

            Thread.Join();

            _CompletedSignal.Set();
        }
    }
}
