﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Squared.Threading {
    public interface IWorkQueue {
        /// <returns>The number of work items handled.</returns>
        int Step (int maximumCount);
    }

    public interface IWorkItem {
        void Execute ();
    }

    public delegate void OnWorkItemComplete<T> (ref T item)
        where T : IWorkItem;

    internal struct InternalWorkItem<T>
        where T : IWorkItem
    {
        public readonly WorkQueue<T>          Queue;
        public readonly OnWorkItemComplete<T> OnComplete;
        public          T                     Data;

        internal InternalWorkItem (WorkQueue<T> queue, ref T data, OnWorkItemComplete<T> onComplete) {
            Queue = queue;
            Data = data;
            OnComplete = onComplete;
        }
        
        // TODO: Add cheap blocking wait primitive
    }

    public class WorkQueue<T> : IWorkQueue
        where T : IWorkItem 
    {
        public struct Marker {
            private readonly WorkQueue<T> Queue;
            private readonly long Start;

            public Marker (WorkQueue<T> queue) {
                Queue = queue;
                Start = Interlocked.Read(ref Queue.ItemsExecuted);
            }

            public long ItemsExecuted {
                get {
                    return Interlocked.Read(ref Queue.ItemsExecuted) - Start;
                }
            }

            /// <summary>
            /// Waits for a set number of items to be executed.
            /// This will hang unless other threads step the queue.
            /// </summary>
            public void Wait (int itemCount) {
                var targetCount = Start + itemCount;

                while (ItemsExecuted < targetCount) {
                    Monitor.Enter(Queue.Token);
                    Monitor.Wait(Queue.Token);
                }
            }
        }

        private readonly object Token = new object();

        private readonly ConcurrentQueue<InternalWorkItem<T>> Queue = 
            new ConcurrentQueue<InternalWorkItem<T>>();

        private long ItemsExecuted;

        public WorkQueue () {
        }

        public void Enqueue (T data, OnWorkItemComplete<T> onComplete = null) {
            Queue.Enqueue(new InternalWorkItem<T>(this, ref data, onComplete));
        }

        public void Enqueue (ref T data, OnWorkItemComplete<T> onComplete = null) {
            Queue.Enqueue(new InternalWorkItem<T>(this, ref data, onComplete));
        }

        public Marker Mark () {
            return new Marker(this);
        }

        public int Step (int maximumCount) {
            InternalWorkItem<T> item;
            int result = 0;

            while (
                (result < maximumCount) &&
                Queue.TryDequeue(out item)
            ) {
                item.Data.Execute();
                if (item.OnComplete != null)
                    item.OnComplete(ref item.Data);
                Interlocked.Increment(ref ItemsExecuted);

                result++;
            }
            
            if (result > 0) {
                lock (Token)
                    Monitor.PulseAll(Token);
            }

            return result;
        }
    }
}
