using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using DotnetPlayground.Extensions;

namespace DotnetPlayground.Collections;

public class ConcurrentSet<T>(ImmutableHashSet<T> items) : ICollection<T>, ICollection, IReadOnlyCollection<T>
{
    private ImmutableHashSet<T> _items = items;

    public int Count => _items.Count;

    bool ICollection<T>.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    public ConcurrentSet() : this(ImmutableHashSet<T>.Empty)
    {
    }

    public ConcurrentSet(IEqualityComparer<T>? comparer) : this(ImmutableHashSet.Create(comparer))
    {
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _items).GetEnumerator();

    public void Add(T item)
    {
        ImmutableInterlocked.Update(ref _items, static (items, item) => items.Add(item), item);
    }

    public bool Contains(T item) => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        _items.RepresentAs<ICollection<T>>().CopyTo(array, arrayIndex);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        _items.RepresentAs<ICollection>().CopyTo(array, index);
    }

    public bool Remove(T item)
    {
        return ImmutableInterlocked.Update(ref _items, static (items, item) => items.Remove(item), item);
    }

    public void Clear()
    {
        ImmutableInterlocked.Update(ref _items, static items => items.Clear());
    }
}