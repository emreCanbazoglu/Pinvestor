using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This exists to avoid this ConditionalWeakTable bug:
/// https://forum.unity.com/threads/occasional-hard-crash.1257519/
/// https://forum.unity.com/threads/conditionalweaktable-getting-corrupted.1306215/
/// https://issuetracker.unity3d.com/issues/incremental-gc-pushes-invalid-stack-range-on-windows
/// https://issuetracker.unity3d.com/issues/editor-crashes-intermittently-on-mono-object-isinst-when-closing-it-in-batch-mode-or-when-building
/// </summary>
public class WeakTable<K, V> : IEnumerable<V> where K : class
{
    public readonly List<Pair> list = new List<Pair>();
    public class Pair
    {
        public WeakReference<K> key;
        public V value;
    }

    public void Add(K key, V value)
    {
        V _;
        if (TryGetValue(key, out _))
            throw new Exception("Key already exists.");
        var pair = new Pair()
        {
            key = new WeakReference<K>(key),
            value = value
        };
        list.Add(pair);
    }

    public bool TryGetValue(K key, out V value)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var pair = list[i];
            K otherKey;
            if (!pair.key.TryGetTarget(out otherKey))
            {
                list.RemoveAt(i);
            }
            else if (otherKey == key)
            {
                value = pair.value;
                return true;
            }
        }
        value = default(V);
        return false;
    }

    public bool Remove(K key)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var pair = list[i];
            K otherKey;
            if (!pair.key.TryGetTarget(out otherKey))
            {
                list.RemoveAt(i);
            }
            else if (otherKey == key)
            {
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public IEnumerator<V> GetEnumerator()
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            K _;
            if (list[i].key.TryGetTarget(out _))
                yield return list[i].value;
            else
                list.RemoveAt(i);
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class WeakList<T> where T : class
{
    public readonly List<WeakReference<T>> list = new List<WeakReference<T>>();

    public void Add(T value)
    {
        list.Add(new WeakReference<T>(value));
    }
    public bool Remove(T value)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            T otherValue;
            if (!list[i].TryGetTarget(out otherValue))
            {
                list.RemoveAt(i);
            }
            else if (otherValue == value)
            {
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public bool Contains(T value)
    {
        return IndexOf(value) != -1;
    }
    public int IndexOf(T value)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            T otherValue;
            if (!list[i].TryGetTarget(out otherValue))
                list.RemoveAt(i);
            else if (otherValue == value)
                return i;
        }
        return -1;
    }
}
