using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Basic
{
    public class ReadOnlyDictionaryView<TKey, TValue, TValueInterface>
        : IReadOnlyDictionary<TKey, TValueInterface>
        where TValue : TValueInterface
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        public ReadOnlyDictionaryView(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public TValueInterface this[TKey key] => _dictionary[key];
        public int Count => _dictionary.Count;

        public IEnumerable<TKey> Keys => _dictionary.Keys;

        public IEnumerable<TValueInterface> Values => _dictionary.Values.Cast<TValueInterface>();

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValueInterface value)
        {
            if (_dictionary.TryGetValue(key, out var concreteValue))
            {
                value = concreteValue;
                return true;
            }
            value = default;
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValueInterface>> GetEnumerator()
        {
            foreach (var kvp in _dictionary)
                yield return new KeyValuePair<TKey, TValueInterface>(kvp.Key, kvp.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
