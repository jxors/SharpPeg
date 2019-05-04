using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpPeg
{
    public static class DictionaryExtensions
    {
        public static Dictionary<K, V> ExtendWith<K, V>(this Dictionary<K, V> me, K key, V value)
        {
            var clone = me.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            clone[key] = value;

            return clone;
        }

        public static Dictionary<K, V> ExtendWith<K, V>(this Dictionary<K, V> me, IEnumerable<K> keys, V value)
        {
            var clone = me.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var key in keys)
            {
                clone[key] = value;
            }

            return clone;
        }
    }
}
