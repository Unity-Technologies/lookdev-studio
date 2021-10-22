using UnityEditor;

public static class SerializedPropertyExtension {

    public static int GetObjectCode(this SerializedProperty p) { // Unique code per serialized object and property path
        return p.propertyPath.GetHashCode() ^ p.serializedObject.GetHashCode();
    }

}
