using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Catalog<TKey, SerializableDictionary<TKey, TValue>.Pair>, ISerializationCallbackReceiver {

    [FormerlySerializedAs("list")]
    [UnityEngine.SerializeField] private Pair[] array;

    public SerializableDictionary () : this(EqualityComparerForUnity<TKey>.Default) {}

    public SerializableDictionary (IEqualityComparer<TKey> comparer) : base () {
        Init(() => ref array, entry => entry.Key, comparer);
    }

    public bool TryGetValue (TKey key, out TValue value) {
        if (base.TryGetValue(key, out var pair)) {
            value = pair.Value;
            return true;
        }
        value = default;
        return false;
    }

    public new TValue this[TKey key] {
        get => base[key].Value;
        set => Set( new Pair( key, value ) );
    }

    public IEnumerable<TValue> Values => ((IEnumerable<Pair>)this).Select(p => p.Value);

    [Serializable]
    public struct Pair : IEquatable<Pair> {
        [FormerlySerializedAs("m_Key")]
        public TKey   Key;
        public TValue Value;

        private static readonly IEqualityComparer<TKey>     keyComparer = EqualityComparerForUnity<TKey>  .Default;
        private static readonly IEqualityComparer<TValue> valueComparer = EqualityComparerForUnity<TValue>.Default;

        public Pair (TKey key, TValue value) {
            Key   = key;
            Value = value;
        }

        public          bool Equals (Pair p)     => keyComparer.Equals(Key, p.Key) && valueComparer.Equals(Value, p.Value);
        public override bool Equals (object obj) => obj is Pair && this.Equals((Pair)obj);
        public override int  GetHashCode()       => keyComparer.GetHashCode(Key) * 17 + valueComparer.GetHashCode(Value);

        public static bool operator ==(Pair lhs, Pair rhs) =>   lhs.Equals(rhs);
        public static bool operator !=(Pair lhs, Pair rhs) => !(lhs.Equals(rhs));

        public override string ToString() => $"{Key}:{Value}";
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue1, TValue2> : Catalog<TKey, SerializableDictionary<TKey, TValue1, TValue2>.Triplet>, ISerializationCallbackReceiver {

    [UnityEngine.SerializeField] private Triplet[] array;

    public SerializableDictionary () : this(EqualityComparerForUnity<TKey>.Default) {}

    public SerializableDictionary (IEqualityComparer<TKey> comparer) : base () {
        Init(() => ref array, entry => entry.Key, comparer);
    }

    public bool TryGetValue (TKey key, out TValue1 value1, out TValue2 value2) {
        var containsKey = TryGetValue(key, out var data);
        if (containsKey) {
            value1 = data.Value1;
            value2 = data.Value2;
        }
        else  {
            value1 = default;
            value2 = default;
        }
        return containsKey;
    }

    public void Add (TKey key, TValue1 value1, TValue2 value2) => Add(new Triplet(key, value1, value2));

    [Serializable]
    public struct Triplet : IEquatable<Triplet> {
        public TKey   Key;
        public TValue1 Value1;
        public TValue2 Value2;

        private static readonly IEqualityComparer<TKey>       keyComparer = EqualityComparerForUnity<TKey>   .Default;
        private static readonly IEqualityComparer<TValue1> value1Comparer = EqualityComparerForUnity<TValue1>.Default;
        private static readonly IEqualityComparer<TValue2> value2Comparer = EqualityComparerForUnity<TValue2>.Default;

        public Triplet (TKey key, TValue1 value1, TValue2 value2) {
            Key    = key;
            Value1 = value1;
            Value2 = value2;
        }

        public          bool Equals (Triplet t)  => keyComparer.Equals(Key, t.Key) && value1Comparer.Equals(Value1, t.Value1) && value2Comparer.Equals(Value2, t.Value2);
        public override bool Equals (object obj) => obj is Triplet && this.Equals((Triplet)obj);
        public override int  GetHashCode()       => keyComparer.GetHashCode(Key) * 289 + value1Comparer.GetHashCode(Value1) * 17 + value2Comparer.GetHashCode(Value2);

        public static bool operator ==(Triplet lhs, Triplet rhs) =>   lhs.Equals(rhs);
        public static bool operator !=(Triplet lhs, Triplet rhs) => !(lhs.Equals(rhs));

        public override string ToString() => $"{Key}:{Value1},{Value2}";
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue1, TValue2, TValue3> : Catalog<TKey, SerializableDictionary<TKey, TValue1, TValue2, TValue3>.Quadruplet>, ISerializationCallbackReceiver {

    [UnityEngine.SerializeField] private Quadruplet[] array;

    public SerializableDictionary () : this(EqualityComparerForUnity<TKey>.Default) {}

    public SerializableDictionary (IEqualityComparer<TKey> comparer) : base () {
        Init(() => ref array, entry => entry.Key, comparer);
    }

    public bool TryGetValue (TKey key, out TValue1 value1, out TValue2 value2, out TValue3 value3) {
        var containsKey = TryGetValue(key, out var data);
        if (containsKey) {
            value1 = data.Value1;
            value2 = data.Value2;
            value3 = data.Value3;
        }
        else  {
            value1 = default;
            value2 = default;
            value3 = default;
        }
        return containsKey;
    }

    public void Add (TKey key, TValue1 value1, TValue2 value2, TValue3 value3) => Add(new Quadruplet(key, value1, value2, value3));

    [Serializable]
    public struct Quadruplet : IEquatable<Quadruplet> {
        public TKey   Key;
        public TValue1 Value1;
        public TValue2 Value2;
        public TValue3 Value3;

        private static readonly IEqualityComparer<TKey>       keyComparer = EqualityComparerForUnity<TKey>   .Default;
        private static readonly IEqualityComparer<TValue1> value1Comparer = EqualityComparerForUnity<TValue1>.Default;
        private static readonly IEqualityComparer<TValue2> value2Comparer = EqualityComparerForUnity<TValue2>.Default;
        private static readonly IEqualityComparer<TValue3> value3Comparer = EqualityComparerForUnity<TValue3>.Default;

        public Quadruplet (TKey key, TValue1 value1, TValue2 value2, TValue3 value3) {
            Key    = key;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public          bool Equals (Quadruplet t) => keyComparer.Equals(Key, t.Key) && value1Comparer.Equals(Value1, t.Value1) && value2Comparer.Equals(Value2, t.Value2) && value3Comparer.Equals(Value3, t.Value3);
        public override bool Equals (object obj)   => obj is Quadruplet && this.Equals((Quadruplet)obj);
        public override int  GetHashCode()         => keyComparer.GetHashCode(Key) * 4913 + value1Comparer.GetHashCode(Value1) * 289 + value2Comparer.GetHashCode(Value2) * 17 + value3Comparer.GetHashCode(Value3);

        public static bool operator ==(Quadruplet lhs, Quadruplet rhs) =>   lhs.Equals(rhs);
        public static bool operator !=(Quadruplet lhs, Quadruplet rhs) => !(lhs.Equals(rhs));

        public override string ToString() => $"{Key}:{Value1},{Value2},{Value3}";
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue1, TValue2, TValue3, TValue4> : Catalog<TKey, SerializableDictionary<TKey, TValue1, TValue2, TValue3, TValue4>.Quintuplet>, ISerializationCallbackReceiver {

    [UnityEngine.SerializeField] private Quintuplet[] array;

    public SerializableDictionary () : this(EqualityComparerForUnity<TKey>.Default) {}

    public SerializableDictionary (IEqualityComparer<TKey> comparer) : base () {
        Init(() => ref array, entry => entry.Key, comparer);
    }

    public bool TryGetValue (TKey key, out TValue1 value1, out TValue2 value2, out TValue3 value3, out TValue4 value4) {
        var containsKey = TryGetValue(key, out var data);
        if (containsKey) {
            value1 = data.Value1;
            value2 = data.Value2;
            value3 = data.Value3;
            value4 = data.Value4;
        }
        else  {
            value1 = default;
            value2 = default;
            value3 = default;
            value4 = default;
        }
        return containsKey;
    }

    public void Add (TKey key, TValue1 value1, TValue2 value2, TValue3 value3, TValue4 value4) => Add(new Quintuplet(key, value1, value2, value3, value4));

    [Serializable]
    public struct Quintuplet : IEquatable<Quintuplet> {
        public TKey   Key;
        public TValue1 Value1;
        public TValue2 Value2;
        public TValue3 Value3;
        public TValue4 Value4;

        private static readonly IEqualityComparer<TKey>       keyComparer = EqualityComparerForUnity<TKey>   .Default;
        private static readonly IEqualityComparer<TValue1> value1Comparer = EqualityComparerForUnity<TValue1>.Default;
        private static readonly IEqualityComparer<TValue2> value2Comparer = EqualityComparerForUnity<TValue2>.Default;
        private static readonly IEqualityComparer<TValue3> value3Comparer = EqualityComparerForUnity<TValue3>.Default;
        private static readonly IEqualityComparer<TValue4> value4Comparer = EqualityComparerForUnity<TValue4>.Default;

        public Quintuplet (TKey key, TValue1 value1, TValue2 value2, TValue3 value3, TValue4 value4) {
            Key    = key;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
            Value4 = value4;
        }

        public          bool Equals (Quintuplet t) => keyComparer.Equals(Key, t.Key) && value1Comparer.Equals(Value1, t.Value1) && value2Comparer.Equals(Value2, t.Value2) && value3Comparer.Equals(Value3, t.Value3) && value4Comparer.Equals(Value4, t.Value4);
        public override bool Equals (object obj)   => obj is Quintuplet && this.Equals((Quintuplet)obj);
        public override int  GetHashCode()         => keyComparer.GetHashCode(Key) * 83521 + value1Comparer.GetHashCode(Value1) * 4913 + value2Comparer.GetHashCode(Value2) * 289 + value3Comparer.GetHashCode(Value3) * 17 + value4Comparer.GetHashCode(Value4);

        public static bool operator ==(Quintuplet lhs, Quintuplet rhs) =>   lhs.Equals(rhs);
        public static bool operator !=(Quintuplet lhs, Quintuplet rhs) => !(lhs.Equals(rhs));

        public override string ToString() => $"{Key}:{Value1},{Value2},{Value3},{Value4}";
    }
}
