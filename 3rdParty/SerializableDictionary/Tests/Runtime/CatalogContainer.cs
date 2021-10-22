using UnityEngine;

namespace CatalogContainerTest {
    public class CatalogContainer : UnityEngine.ScriptableObject, UnityEngine.ISerializationCallbackReceiver {
        public const string nameof_source = nameof(source);

        [UnityEngine.SerializeField] private TestStructure[] source;
        public Catalog<int, TestStructure> catalog;

        public CatalogContainer() =>
            catalog = new Catalog<int, TestStructure>( () => ref source, entry => entry.Key );
        void OnEnable() =>
            source ??= new TestStructure[0];

        public void OnBeforeSerialize () => catalog.OnBeforeSerialize();
        public void OnAfterDeserialize() => catalog.OnAfterDeserialize();
    }
}
