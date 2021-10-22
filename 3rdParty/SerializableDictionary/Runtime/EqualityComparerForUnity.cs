using System.Collections.Generic;

public class EqualityComparerForUnity<T> {

	public static IEqualityComparer<T> Default { get; } = CreateComparer();

	private static IEqualityComparer<T> CreateComparer() {

        if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            return (IEqualityComparer<T>) new UnityObjectEqualityComparer();

        return EqualityComparer<T>.Default;
    }

    private class UnityObjectEqualityComparer : EqualityComparer<UnityEngine.Object> {
        public override bool Equals      (UnityEngine.Object left, UnityEngine.Object right) => left?.Equals(right) ?? right == null;
        public override int  GetHashCode (UnityEngine.Object obj)                            => obj?.GetHashCode() ?? 0;
    }
}
