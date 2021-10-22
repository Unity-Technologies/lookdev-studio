using System;
using System.Collections.Generic;

using NUnit.Framework;

using CatalogContainerTest;

public class TestCatalog {
    [Test]
    public void TestContainsKey () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        testCatalog.Add(3);

        Assert.That(testCatalog.ContainsKey(3), Is.EqualTo(true ));
        Assert.That(testCatalog.ContainsKey(4), Is.EqualTo(false));
    }

    [Test]
    public void TestEnsureCapacity () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        testCatalog.Add(3);

        // The capacity should be the next prime equal or greater than the argument

        Assert.That(testCatalog.EnsureCapacity(1),  Is.EqualTo(3));
        Assert.That(testCatalog.EnsureCapacity(2),  Is.EqualTo(3));
        Assert.That(testCatalog.EnsureCapacity(3),  Is.EqualTo(3));
        Assert.That(testCatalog.EnsureCapacity(4),  Is.EqualTo(7));
        Assert.That(testCatalog.EnsureCapacity(5),  Is.EqualTo(7));
        Assert.That(testCatalog.EnsureCapacity(6),  Is.EqualTo(7));
        Assert.That(testCatalog.EnsureCapacity(7),  Is.EqualTo(7));
        Assert.That(testCatalog.EnsureCapacity(8),  Is.EqualTo(11));
        Assert.That(testCatalog.EnsureCapacity(9),  Is.EqualTo(11));
        Assert.That(testCatalog.EnsureCapacity(10), Is.EqualTo(11));
        Assert.That(testCatalog.EnsureCapacity(11), Is.EqualTo(11));
        Assert.That(testCatalog.EnsureCapacity(12), Is.EqualTo(17));
    }

    [Test]
    public void TestRemove () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        testCatalog.Add   (3);
        Assume.That(testCatalog.Count, Is.EqualTo(1));

        testCatalog.Remove(3);
        Assert.That(testCatalog.Count, Is.EqualTo(0));

        testCatalog.Add   (4);

        Assume.That(testCatalog.Count, Is.EqualTo(1));
        Assert.That(testCatalog.ContainsKey(3), Is.EqualTo(false));
        Assert.That(testCatalog.ContainsKey(4), Is.EqualTo(true ));

        testCatalog.Add   (5);
        testCatalog.Add   (6);

        // After reaching 3 elements, capacity has not increased,
        // which means that '3's spot has been reused
        Assert.That(testCatalog.EnsureCapacity(3), Is.EqualTo(3));

        testCatalog.Add   (7);
        Assert.That(testCatalog.Count, Is.EqualTo(4));
        Assert.That(testCatalog.EnsureCapacity(3), Is.EqualTo(7));
    }

    [Test]
    public void TestTrimExcess () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        // Add 8 elements (0, 1, ..., 7)
        for (int i = 0; i < 8; i++)
            testCatalog.Add (i);

        // When adding, capacity increases from 3 -> 7 -> 17   (double and make prime)
        Assume.That(testCatalog.EnsureCapacity(1), Is.EqualTo(17));

        testCatalog.TrimExcess(4);
        // No trimming, 11 is needed for the 8 elements
        Assert.That(testCatalog.EnsureCapacity(1), Is.EqualTo(11));

        // Remove 5 elements from the end
        for (int i = 7; i >= 3; i--)
            Assume.That(testCatalog.Remove(i));

        testCatalog.TrimExcess(4);

        // For 4 elements, capacity of 7 is needed
        Assert.That(testCatalog.EnsureCapacity(1), Is.EqualTo(7));

        testCatalog.TrimExcess();

        // For the 3 remaining elements, only 3 is needed
        Assert.That(testCatalog.EnsureCapacity(1), Is.EqualTo(3));
    }

    [Test]
    public void TestTryGetValue () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        testCatalog.Add(3);
        testCatalog.Add(4);

        int value;

        Assert.That(testCatalog.TryGetValue(3, out value), Is.EqualTo(true));
        Assert.That(value, Is.EqualTo(3));

        Assert.That(testCatalog.TryGetValue(4, out value), Is.EqualTo(true));
        Assert.That(value, Is.EqualTo(4));

        Assert.That(testCatalog.TryGetValue(5, out value), Is.EqualTo(false));
    }

    [Test]
    public void TestItem () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        testCatalog.Add(3);
        testCatalog.Add(4);

        Assert.That(() => _ = testCatalog[3], Throws.Nothing);
        Assert.That(() => _ = testCatalog[4], Throws.Nothing);

        Assert.That(() => _ = testCatalog[5], Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void TestSet () {
        var testCatalog = new Catalog<int, (int, string)> (null, tup => tup.Item1);

        testCatalog.Add((9001, "Nine thousand one"));

        // One item works
        Assume.That(() => _ = testCatalog[9001], Throws.Nothing);
        Assume.That(testCatalog[9001], Is.EqualTo((9001, "Nine thousand one")));

        // Test changing the item
        testCatalog.Set((9001, "More than nine thousand"));

        Assume.That(() => _ = testCatalog[9001], Throws.Nothing);

        Assert.That(testCatalog[9001], Is.EqualTo((9001, "More than nine thousand")));
    }

    [Test]
    public void TestIEnumerator () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        var sourceArray = new[] { 3, 4, 5, 6, 7, 8, 9, 1, 0, -1, -20, 2, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var values = new HashSet<int> ( sourceArray );

        foreach (var v in values) {
            testCatalog.Add(v);
        }

        IEnumerator<int> enumerator = testCatalog.GetEnumerator();

        for ( int i = 0; i < sourceArray.Length; i++) {
            Assert.That(enumerator.MoveNext(), Is.EqualTo(true));
            Assert.That(values.Remove(enumerator.Current), Is.EqualTo(true));
        }

        Assert.That(enumerator.MoveNext(), Is.EqualTo(false));
        Assert.That(values.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestCopyTo () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        var sourceArray = new[] { 3, 4, 5, 6, 7, 8, 9, 1, 0, -1, -20, 2, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

        foreach (var v in sourceArray)
            testCatalog.Add(v);

        var targetArray = new int[sourceArray.Length];

        testCatalog.CopyTo(targetArray, 0);

        //Array.Sort(sourceArray);
        //Array.Sort(targetArray);

        Assert.That(targetArray, Is.EquivalentTo(sourceArray));
    }

    [Test]
    public void TestSerialization () {
        var container = UnityEngine.ScriptableObject.CreateInstance<CatalogContainer>();

        try {
            var testCatalog = container.catalog;

            var sourceArray = System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(
                    new[] { 3, 4, 5, 6, 7, 8, 9, 1, 0, -1, -20, 2, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                    v => new TestStructure { Key=v, Data=v.ToString() }));

            foreach (var v in sourceArray)
                testCatalog.Add(v);

            var so         = new UnityEditor.SerializedObject(container);
            var sourceProp = so.FindProperty(CatalogContainer.nameof_source);

            var targetArray = new TestStructure[sourceArray.Length];

            for (int i = 0; i < sourceArray.Length; i++) {
                var prop     = sourceProp.GetArrayElementAtIndex(i);
                var keyProp  = prop.FindPropertyRelative(nameof(TestStructure.Key));
                var dataProp = prop.FindPropertyRelative(nameof(TestStructure.Data));

                targetArray[i] = new TestStructure { Key=keyProp.intValue, Data=dataProp.stringValue };
            }

            //Array.Sort<TestStructure>(sourceArray);
            //Array.Sort<TestStructure>(targetArray);

            Assert.That(targetArray, Is.EquivalentTo(sourceArray));
        }
        finally {
            UnityEngine.Object.DestroyImmediate(container);
        }
    }

    [Test]
    public void TestDeserialization () {
        var container = UnityEngine.ScriptableObject.CreateInstance<CatalogContainer>();

        try {
            var testCatalog = container.catalog;

            var sourceArray = System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(
                    new[] { 3, 4, 5, 6, 7, 8, 9, 1, 0, -1, -20, 2, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 },
                    v => new TestStructure { Key=v, Data=v.ToString() }));

            var so         = new UnityEditor.SerializedObject(container);
            var sourceProp = so.FindProperty(CatalogContainer.nameof_source);

            sourceProp.arraySize = sourceArray.Length;

            for (int i = 0; i < sourceArray.Length; i++) {
                var prop     = sourceProp.GetArrayElementAtIndex(i);
                var keyProp  = prop.FindPropertyRelative(nameof(TestStructure.Key));
                var dataProp = prop.FindPropertyRelative(nameof(TestStructure.Data));

                keyProp .intValue    = sourceArray[i].Key;
                dataProp.stringValue = sourceArray[i].Data;
            }

            so.ApplyModifiedProperties();

            var targetArray = new TestStructure[sourceArray.Length];

            testCatalog.CopyTo(targetArray, 0);

            //Array.Sort<TestStructure>(sourceArray);
            //Array.Sort<TestStructure>(targetArray);

            Assert.That(targetArray, Is.EquivalentTo(sourceArray));
        }
        finally {
            UnityEngine.Object.DestroyImmediate(container);
        }
    }

    [Test]
    public void TestKeys () {
        var testCatalog = new Catalog<int, (int, string)> (null, tup => tup.Item1);

        var sourceArray = new[] { 
            (3, "three"),
            (4, "four"),
            (5, "five")
        };

        var keys = new HashSet<int> ( System.Linq.Enumerable.Select( sourceArray, tuple => tuple.Item1 ) );

        foreach (var t in sourceArray) {
            testCatalog.Add(t);
        }

        IEnumerator<int> enumerator = testCatalog.Keys.GetEnumerator();

        for ( int i = 0; i < sourceArray.Length; i++) {
            Assert.That(enumerator.MoveNext(), Is.EqualTo(true));
            Assert.That(keys.Remove(enumerator.Current), Is.EqualTo(true));
        }

        Assert.That(enumerator.MoveNext(), Is.EqualTo(false));
        Assert.That(keys.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestCount () {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        var sourceArray = new[] { 3, 4, 5, 6, 7, 8, 9, 1, 0, -1, -20, 2, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

        for (int i = 0; i < sourceArray.Length; i++) {
            Assert.That(testCatalog.Count, Is.EqualTo(i));
            testCatalog.Add(sourceArray[i]);
        }
    }

    [Test]
    public void TestClear() {
        var testCatalog = new Catalog<int, int> (null, (int key) => key);

        var sourceArray = new[] { 3, 4, 5 };

        for (int i = 0; i < sourceArray.Length; i++)
            testCatalog.Add(sourceArray[i]);

        testCatalog.Clear();

        Assert.That(testCatalog.Count, Is.EqualTo(0));
    }
}
