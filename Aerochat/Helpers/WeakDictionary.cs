using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.Helpers
{
    /// <summary>
    ///     Weak dictionary where key/value pairs are culled if the value object is garbage collected.
    ///     <see href="https://github.com/bhaeussermann/weak-dictionary/blob/main/src/WeakDictionary/WeakDictionary.cs">Original implementation</see>
    /// </summary>
    public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TValue : class
    {
        private readonly IDictionary<TKey, WeakReference> _internalDictionary = new Dictionary<TKey, WeakReference>();
        private readonly ConditionalWeakTable<TValue, Finalizer> _conditionalWeakTable = new ConditionalWeakTable<TValue, Finalizer>();

        public TValue this[TKey key]
        {
            get => (TValue)this._internalDictionary[key].Target;
            set
            {
                Remove(key);
                Add(key, value);
            }
        }

        public ICollection<TKey> Keys => this._internalDictionary.Keys;

        public ICollection<TValue> Values => this._internalDictionary.Values.Select(r => (TValue)r.Target).ToArray();

        public int Count => this._internalDictionary.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            this._internalDictionary.Add(key, new WeakReference(value));
            var finalizer = new Finalizer(key);
            finalizer.ValueFinalized += k => Remove(k);
            this._conditionalWeakTable.Add(value, finalizer);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear() => this._internalDictionary.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this._internalDictionary.TryGetValue(item.Key, out var valueReference) && valueReference.Target.Equals(item.Value);
        }

        public bool ContainsKey(TKey key) => this._internalDictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, WeakReference> keyValuePair in this._internalDictionary)
            {
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(keyValuePair.Key, (TValue)keyValuePair.Value.Target);
            }
        }

        public bool Remove(TKey key) => this._internalDictionary.Remove(key);

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this._internalDictionary.TryGetValue(item.Key, out var valueReference)
                && valueReference.Target.Equals(item.Value)
                && this._internalDictionary.Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (this._internalDictionary.TryGetValue(key, out var valueReference))
            {
                value = (TValue)valueReference.Target;
                return true;
            }

            value = default;
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this._internalDictionary.Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, (TValue)kvp.Value.Target)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Finalizer
        {
            private readonly TKey valueKey;

            public Finalizer(TKey valueKey)
            {
                this.valueKey = valueKey;
            }

            ~Finalizer()
            {
                ValueFinalized?.Invoke(this.valueKey);
            }

            public event ValueFinalizedDelegate ValueFinalized;
        }

        private delegate void ValueFinalizedDelegate(TKey valueKey);
    }
}
