namespace FluffyByte.SimpleUnityServer.Utilities
{
    // ThreadSafeList<T>: A thread-safe wrapper for List<T> using C# 9.0 Lock
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    public class ThreadSafeList<T> : IList<T>
    {
        private readonly List<T> _list = [];
        private readonly Lock _lock = new();

        public int Count
        {
            get { lock (_lock) { return _list.Count; } }
        }

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get { lock (_lock) { return _list[index]; } }
            set { lock (_lock) { _list[index] = value; } }
        }

        public void Add(T item)
        {
            lock (_lock) { _list.Add(item); }
        }

        public void Clear()
        {
            lock (_lock) { _list.Clear(); }
        }

        public bool Contains(T item)
        {
            lock (_lock) { return _list.Contains(item); }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock) { _list.CopyTo(array, arrayIndex); }
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock (_lock) { snapshot = []; }
            return snapshot.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            lock (_lock) { return _list.IndexOf(item); }
        }

        public void Insert(int index, T item)
        {
            lock (_lock) { _list.Insert(index, item); }
        }

        public bool Remove(T item)
        {
            lock (_lock) { return _list.Remove(item); }
        }

        public void RemoveAt(int index)
        {
            lock (_lock) { _list.RemoveAt(index); }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}