using System;

namespace CatalogContainerTest {
    [Serializable]
    public struct TestStructure : IEquatable<TestStructure>, IComparable<TestStructure> {
        public int    Key;
        public string Data;

        public          bool Equals (TestStructure s  ) => (Key == s.Key) && Data.Equals(s.Data);
        public override bool Equals (object        obj) => obj is TestStructure && this.Equals((TestStructure)obj);
        public override int  GetHashCode()              => (Key, Data).GetHashCode();

        public int CompareTo (TestStructure other) { // For sorting
            var r = Key.CompareTo(other.Key);
            return r != 0 ? r : Data.CompareTo(other.Data);
        }

        public static bool operator ==(TestStructure lhs, TestStructure rhs) =>   lhs.Equals(rhs);
        public static bool operator !=(TestStructure lhs, TestStructure rhs) => !(lhs.Equals(rhs));
    }
}
