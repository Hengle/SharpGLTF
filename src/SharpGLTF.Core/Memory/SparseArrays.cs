﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Special accessor to wrap over a base accessor and a sparse accessor
    /// </summary>
    /// <typeparam name="T">An unmanage structure type.</typeparam>
    [System.Diagnostics.DebuggerDisplay("Sparse {typeof(T).Name} Accessor {Count}")]
    public struct SparseArray<T> : IList<T>, IReadOnlyList<T>
        where T : unmanaged
    {
        #region lifecycle

        public SparseArray(IList<T> bottom, IList<T> top, IntegerArray topMapping)
        {
            _BottomItems = bottom;
            _TopItems = top;
            _Mapping = new Dictionary<int, int>();

            for (int val = 0; val < topMapping.Count; ++val)
            {
                var key = (int)topMapping[val];
                _Mapping[key] = val;
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IList<T> _BottomItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IList<T> _TopItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Dictionary<int, int> _Mapping;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private T[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _BottomItems.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _Mapping.TryGetValue(index, out int topIndex) ? _TopItems[topIndex] : _BottomItems[index];
            set
            {
                if (_Mapping.TryGetValue(index, out int topIndex)) _TopItems[topIndex] = value;
            }
        }

        public IEnumerator<T> GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        public bool Contains(T item) { return IndexOf(item) >= 0; }

        public int IndexOf(T item) { return EncodedArrayUtils.FirstIndexOf(this, item); }

        public void CopyTo(T[] array, int arrayIndex) { EncodedArrayUtils.CopyTo(this, array, arrayIndex); }

        void IList<T>.Insert(int index, T item) { throw new NotSupportedException(); }

        void IList<T>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<T>.Add(T item) { throw new NotSupportedException(); }

        void ICollection<T>.Clear() { throw new NotSupportedException(); }

        bool ICollection<T>.Remove(T item) { throw new NotSupportedException(); }

        #endregion
    }
}