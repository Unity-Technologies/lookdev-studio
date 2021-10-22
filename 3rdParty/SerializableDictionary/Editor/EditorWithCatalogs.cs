using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

// Warning, compared with Editor, one ui feature will be missing from the default inspector, AudioFilterGUI
public class EditorWithCatalogs : Editor {

    private class SerializedPropertyComparer : IEqualityComparer<SerializedProperty> {
        public bool Equals     (SerializedProperty left, SerializedProperty right) => SerializedProperty.EqualContents(left, right);
        public int  GetHashCode(SerializedProperty prop) => prop.GetObjectCode();
    }

    private Dictionary<SerializedProperty, bool> propertyIsCatalog = new Dictionary<SerializedProperty, bool>(new SerializedPropertyComparer());

    public bool CheckIfNeedsCatalog (SerializedProperty prop) {
        if (!propertyIsCatalog.TryGetValue(prop, out var result)) {
            var fieldInfo = SerializedPropertyHelper.GetFieldInfoOfProperty (prop);
            var isCatalog = (fieldInfo?.GetCustomAttributes(typeof(CatalogDataAttribute), true).Length ?? 0) != 0;
            propertyIsCatalog[prop] = result = isCatalog;
        }
        return result;
    }

    public new bool DrawDefaultInspector() {
        using (new LocalizationGroup(target)) {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(true)) do
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    if (CheckIfNeedsCatalog( iterator ))
                        CatalogGUILayout.CatalogField(iterator);
                    else
                        EditorGUILayout.PropertyField(iterator, true);
            while (iterator.NextVisible(false));

            serializedObject.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
    }

}
