using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using DotnetPlayground.Extensions;

namespace DotnetPlayground.Collections;

public class ConcurrentCollection<T>(ImmutableArray<T> items) : ICollection<T>, ICollection, IReadOnlyCollection<T>
{
    private ImmutableArray<T> _items = items;

    public int Count => _items.Length;

    bool ICollection<T>.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    public ConcurrentCollection() : this(ImmutableArray<T>.Empty)
    {
    }

    public ImmutableArray<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>) _items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _items).GetEnumerator();

    public void Add(T item)
    {
        ImmutableInterlocked.Update(ref _items, static (items, item) => items.Add(item), item);
    }

    public bool AddIfNotContained(T item)
    {
        return ImmutableInterlocked.Update(
            ref _items,
            static (items, item) => items.Contains(item) ? items : items.Add(item),
            item);
    }

    public bool Contains(T item) => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        _items.RepresentAs<ICollection>().CopyTo(array, index);
    }

    public bool Remove(T item)
    {
        return ImmutableInterlocked.Update(ref _items, static (items, item) => items.Remove(item), item);
    }

    public bool RemoveAll(Predicate<T> predicate)
    {
        return ImmutableInterlocked.Update(ref _items, static (items, predicate) => items.RemoveAll(predicate), predicate);
    }

    public void Clear()
    {
        ImmutableInterlocked.InterlockedExchange(ref _items, ImmutableArray<T>.Empty);
    }
}