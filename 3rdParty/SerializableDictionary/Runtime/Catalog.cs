using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

public class Catalog { }

public class Catalog<TKey, TEntry> : Catalog, IEnumerable<TEntry> {

    /// Writes dictionary to the source array
    public void OnBeforeSerialize() {
        if (source == null)
            throw new NullReferenceException("Catalog has lost connection with its source array, can not serialize");

        if (version != 0) {
            version = 0;
            ref var array = ref source();

            Dictionary<TKey, int> relativePositions = null;

            if (array != null) {
                relativePositions = new Dictionary<TKey, int>(array.Length);
                for (int i = 0; i < array.Length; i++) {
                    var key = keyFrom(array[i]);
                    if (key != null)
                        relativePositions[key] = i;
                }
            }

            if (array == null || array.Length != count)
                array = new TEntry[count];

            if (count != 0)
                CopyTo(array, 0);

            if (relativePositions != null)
                Array.Sort(array, CompareEntryPositions);

            // Try to make the order as stable as possible
            int CompareEntryPositions (TEntry left, TEntry right) {
                return relativePositions.TryGetValue( keyFrom( left ), out var leftIndex )
                       ? relativePositions.TryGetValue( keyFrom( right ), out var rightIndex )
                         ? leftIndex - rightIndex
                         : -1
                       : relativePositions.TryGetValue( keyFrom( right ), out _ )
                         ? 1
                         : 0;
            }
        }
    }

    /// Fills buckets from the source array
    public void OnAfterDeserialize() {
        if (source == null)
            throw new NullReferenceException("Catalog has lost connection with its source array, can not serialize");

        var array = source();

        if (array != null && array.Length != 0) {
            var newCapacity = GoodDictionarySize(array.Length);
            (buckets, bucketEntries) = CreateBucketsAndEntries( newCapacity );
            FillBuckets(array, buckets, bucketEntries);
            count         = array.Length;
            freeList      = -1;
            freeCount     = 0;
        }
        else if (count != 0)
            Clear();

        version = 0;
    }

    public delegate ref TEntry[] CollectionReferenceGetter();
    public delegate TKey         KeyGetter( TEntry entry );

    private CollectionReferenceGetter source;
    private KeyGetter                 keyFrom;
    private IEqualityComparer<TKey>   comparer;

    //
    //  https://blog.markvincze.com/back-to-basics-dictionary-part-2-net-implementation/
    //
    private struct BucketEntry {
        public int hashCode;
        public int next;
        public TEntry value;
    }

    private BucketEntry[] bucketEntries;
    private int        [] buckets;
    private int           version;
    private int           freeList;
    private int           freeCount;

    private int           count;

    internal static readonly int[] primes = {3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369};

    private int GoodDictionarySize (int candidate) {
        var index = Array.BinarySearch( primes, candidate );
        if (index >= 0)
            return candidate;

        index = ~index;
        if (index < primes.Length)
            return primes[index];

        return candidate; // Big primes didn't seem as interresting https://www.codeproject.com/articles/500644/understanding-generic-dictionary-in-depth
    }

    // Increase capacity to at least match 'capacity'
    public int EnsureCapacity (int capacity) {
        int resultCapacity = buckets.Length;
        if (capacity > resultCapacity) {
            resultCapacity = GoodDictionarySize(capacity);
            SetCapacity( resultCapacity );
        }
        return resultCapacity;
    }

    // Set the smallest advisable capacity that will
    // still hold all the elements and match 'capacity'
    public void TrimExcess (int capacity) {
        int newCapacity = GoodDictionarySize( Math.Max(capacity, count) );
        if ( buckets.Length != newCapacity )
            SetCapacity( newCapacity );
    }

    public void TrimExcess () {
        TrimExcess( count );
    }

    private void SetCapacity (int capacity) {
        var (newBuckets, newBucketEntries) = CreateBucketsAndEntries( capacity );
        FillBuckets(buckets, bucketEntries, newBuckets, newBucketEntries);
        (buckets, bucketEntries) = (newBuckets, newBucketEntries);
        freeList      = -1;
        freeCount     = 0;
    }

    private static (int[] buckets, BucketEntry[] bucketEntries) CreateBucketsAndEntries (int capacity) {
        var newBuckets       = new int        [capacity];
        var newBucketEntries = new BucketEntry[capacity];
        for (int i = 0; i < capacity; i++)
            newBuckets[i] = -1;
        return (newBuckets, newBucketEntries);
    }

    private static void FillBuckets (int[] oldBuckets, BucketEntry[] oldBucketEntries, int[] newBuckets, BucketEntry[] newBucketEntries) {
        var newCapacity = newBuckets.Length;
        var oldCapacity = oldBuckets.Length;
        var newCount      = 0;
        for (int i = 0; i < oldCapacity; i++) {
            var bucketIndex = oldBuckets[i];
            while (bucketIndex != -1) {
                BucketEntry     oldEntry = oldBucketEntries[bucketIndex];
                ref BucketEntry newEntry = ref newBucketEntries[newCount];

                var newTargetBucket = TargetBucket( oldEntry.hashCode, newCapacity );

                newEntry.hashCode  = oldEntry.hashCode;
                newEntry.next      = newBuckets[newTargetBucket]; // If the bucket already contained an item, it will be the next in the collision resolution chain.
                newEntry.value     = oldEntry.value;

                newBuckets[newTargetBucket] = newCount;

                newCount++;

                bucketIndex = oldEntry.next;
            }
        }
    }
    private void FillBuckets (TEntry[] source, int[] newBuckets, BucketEntry[] newBucketEntries) {
        var newLength    = newBuckets.Length;
        var sourceLength = source.Length;

        for (int i = 0; i < sourceLength; i++) {
            ref BucketEntry newEntry = ref newBucketEntries[i];

            newEntry.value    = source[i];
            newEntry.hashCode = comparer.GetHashCode( keyFrom( newEntry.value ) );

            var targetBucket  = TargetBucket( newEntry.hashCode, newLength );
            newEntry.next     = newBuckets[targetBucket];

            newBuckets[targetBucket] = i;
        }
    }

    private int Grow () => EnsureCapacity( count * 2 );

    private        int TargetBucket (int hashCode)                 => (hashCode & 0x7FFFFFFF) /* no negative values */ % buckets.Length;
    private static int TargetBucket (int hashCode, int numBuckets) => (hashCode & 0x7FFFFFFF) /* no negative values */ % numBuckets;

    private void Insert(TKey key, TEntry value, bool throwOnOverwrite) {

        int hashCode     = comparer.GetHashCode( key );
        int targetBucket = TargetBucket( hashCode );

        // Look at all the bucket entries in the target bucket.
        // The next field of the entry points to the next entry in the chain, in case of collision.
        // If there are no more items in the chain, its value is -1.

        // Find existing entry
        for (int i = buckets[targetBucket]; i >= 0; i = bucketEntries[i].next) {
            if (bucketEntries[i].hashCode == hashCode && comparer.Equals(keyFrom( bucketEntries[i].value ), key)) {
                if (throwOnOverwrite)
                    throw new System.ArgumentException($"The key: '{key}' already exists in the dictionary"); 
                bucketEntries[i].value = value;
                version++;
                return;
            }
        }

        int index;
        if (freeCount > 0) {
            // Reuse entries
            index = freeList;
            freeList = bucketEntries[index].next;
            freeCount--;
            count++;
        }
        else { // No free entries, means that 'count' will also point to the
               // next unused entry in bucketEntries
            if (count == bucketEntries.Length) {
                Grow();
                targetBucket = TargetBucket( hashCode );
            }
            index = count;
            count++;
        }

        ref BucketEntry bucketEntry = ref bucketEntries[index];

        bucketEntry.hashCode  = hashCode;
        bucketEntry.next      = buckets[targetBucket]; // If the bucket already contained an item, it will be the next in the collision resolution chain.
        bucketEntry.value     = value;
        buckets[targetBucket] = index; // The bucket will point to this entry from now on.

        version++;
    }

    public bool Remove (TKey key) {
        int hashCode     = comparer.GetHashCode( key );
        int targetBucket = TargetBucket( hashCode );

        ref var index = ref buckets[targetBucket];

        if (index == -1)
            return false;

        for (; index >= 0; index = ref bucketEntries[index].next) {
            ref var bucketEntry = ref bucketEntries[index];
            if (bucketEntry.hashCode == hashCode && comparer.Equals(keyFrom( bucketEntry.value ), key)) {
                var removedIndex = index;
                index = bucketEntry.next;

                if (freeCount > 0) // Make removed entry part of free list
                    bucketEntry.next = freeList;

                freeList = removedIndex;
                freeCount++;
                count--;

                version++;
                return true;
            }
        }

        if (key == null)
            throw new ArgumentNullException();

        return false;
    }

    public bool TryGetValue (TKey key, out TEntry value) {
        int hashCode     = comparer.GetHashCode( key );
        int targetBucket = TargetBucket( hashCode );

        for (int i = buckets[targetBucket]; i >= 0; i = bucketEntries[i].next)
            if (bucketEntries[i].hashCode == hashCode && comparer.Equals(keyFrom( bucketEntries[i].value ), key)) {
                value = bucketEntries[i].value;
                return true;
            }

        value = default(TEntry);
        return false;
    }

    public TEntry this[TKey key] {
        get {
            TEntry entry;
            if (!TryGetValue(key, out entry))
                throw new KeyNotFoundException(key.ToString());
            return entry;
        }
        // the setter is left out because the key is a part of the entry, so
        // the usage would have to state the key twice, which just doesn't look
        // right, and opens op the possibility that the specified key isn't
        // contained in the entry provided
        //
        // use Set()
    }

    // This requires the caller to use the Init method.
    // Used when the 'this' and keyGetter isn't available
    // at the beginning of construction.
    protected Catalog () {
        this.Keys = new KeyCollection(this);
    }

    protected void Init(CollectionReferenceGetter source, KeyGetter keyGetter, IEqualityComparer<TKey> comparer) {
        this.source   = source;
        this.keyFrom  = keyGetter;
        this.comparer = comparer;
        Clear();
    }

    public Catalog (CollectionReferenceGetter source, KeyGetter keyGetter) : this(source, keyGetter, EqualityComparerForUnity<TKey>.Default) {}

    public Catalog (CollectionReferenceGetter source, KeyGetter keyGetter, IEqualityComparer<TKey> comparer) {
        this.Keys = new KeyCollection(this);
        Init(source, keyGetter, comparer);
    }

    public void Add( TEntry entry ) => Insert( keyFrom( entry ), entry, true  );
    public void Set( TEntry entry ) => Insert( keyFrom( entry ), entry, false );

    public IEnumerable<TKey> Keys { get; }

    public bool ContainsKey (TKey key) {
        int hashCode     = comparer.GetHashCode( key );
        int targetBucket = TargetBucket( hashCode );

        for (int i = buckets[targetBucket]; i >= 0; i = bucketEntries[i].next)
            if (bucketEntries[i].hashCode == hashCode && comparer.Equals(keyFrom( bucketEntries[i].value ), key))
                return true;

        return false;
    }

    public int  Count   => count;
    public void Clear() {
        bucketEntries = new BucketEntry[3];
        buckets       = new int[] {-1, -1, -1};
        freeList      = -1;
        freeCount     = 0;
        count         = 0;
        version++;
    }

    public void CopyTo (TEntry[] target, int arrayIndex) {
        if (target.Length - arrayIndex < count)
            throw new ArgumentException("arrayIndex");
        for (int i = 0; i < buckets.Length; i++) {
            var index = buckets[i];
            while (index != -1) {
                target[arrayIndex++] = bucketEntries[index].value;
                index = bucketEntries[index].next;
            }
        }
    }

    public class KeyCollection : IEnumerable<TKey> {
        private Catalog<TKey, TEntry> catalog;

        public KeyCollection (Catalog<TKey, TEntry> catalog) => this.catalog = catalog;

        public KeyEnumerator                   GetEnumerator() => new KeyEnumerator(catalog);
        IEnumerator<TKey>    IEnumerable<TKey>.GetEnumerator() => new KeyEnumerator(catalog);
        IEnumerator          IEnumerable      .GetEnumerator() => new KeyEnumerator(catalog);

        public struct KeyEnumerator : IEnumerator<TKey> {
            private Catalog<TKey, TEntry> catalog;
            private int index, bucket;

            public KeyEnumerator (Catalog<TKey, TEntry> catalog) {
                this.catalog = catalog;
                this.index   = -1;
                this.bucket  = -1;
            }

            public TKey             Current   => catalog.keyFrom( catalog.bucketEntries[bucket].value );
            public void             Dispose() => catalog = null;
            public bool            MoveNext() => CatalogEnumerator.MoveNext(ref catalog, ref index, ref bucket);

            object      IEnumerator.Current   => catalog.keyFrom( catalog.bucketEntries[bucket].value );
            void        IEnumerator.Reset  () => CatalogEnumerator.Reset(ref index, ref bucket);
        }
    }

    private static class CatalogEnumerator {
        public static bool MoveNext(ref Catalog<TKey, TEntry> catalog, ref int index, ref int bucket) {
            if (bucket != -1) // advance bucket
                bucket = catalog.bucketEntries[bucket].next;

            if (bucket == -1) { // not in a bucket
                var numBuckets = catalog.buckets.Length;
                for (index += 1; index < numBuckets; index++) {
                    bucket = catalog.buckets[index];
                    if (bucket != -1)
                        return true;
                }
                return false;
            }

            return true;
        }
        public static void Reset(ref int index, ref int bucket) => index = bucket = -1;
    }

    public struct ValueEnumerator : IEnumerator<TEntry> {
        private Catalog<TKey, TEntry> catalog;
        private int index, bucket;

        public ValueEnumerator (Catalog<TKey, TEntry> catalog) {
            this.catalog = catalog;
            this.index   = -1;
            this.bucket  = -1;
        }

        public TEntry           Current   => catalog.bucketEntries[bucket].value;
        public void             Dispose() => catalog = null;
        public bool            MoveNext() => CatalogEnumerator.MoveNext(ref catalog, ref index, ref bucket);

        object      IEnumerator.Current   => catalog.bucketEntries[bucket].value;
        void        IEnumerator.Reset  () => CatalogEnumerator.Reset(ref index, ref bucket);
    }

    public ValueEnumerator                     GetEnumerator() => new ValueEnumerator(this);
    IEnumerator<TEntry>    IEnumerable<TEntry>.GetEnumerator() => new ValueEnumerator(this);
    IEnumerator            IEnumerable        .GetEnumerator() => new ValueEnumerator(this);
}
