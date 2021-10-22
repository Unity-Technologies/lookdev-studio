using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

public class CatalogGUI {
    protected static Dictionary<int, CatalogDrawer> drawers = new Dictionary<int, CatalogDrawer>();
    protected static Dictionary<int, GUIContent>    labels  = new Dictionary<int, GUIContent>   ();

    static public void CatalogField (Rect position, SerializedProperty property) {
        var code = property.GetObjectCode();

        if (!labels .TryGetValue(code, out var label )) labels [code] = label  = new GUIContent(property.displayName);
        if (!drawers.TryGetValue(code, out var drawer)) drawers[code] = drawer = new CatalogDrawer();

        drawer.OnGUI (position, property, label);
    }
}

public class CatalogGUILayout : CatalogGUI {

    static public void CatalogField (SerializedProperty property) {
        var code = property.GetObjectCode();

        if (!labels .TryGetValue(code, out var label )) labels [code] = label  = new GUIContent(property.displayName);
        if (!drawers.TryGetValue(code, out var drawer)) drawers[code] = drawer = new CatalogDrawer();

        var position = EditorGUILayout.GetControlRect(true, drawer.GetPropertyHeight(property, null));
        drawer.OnGUI (position, property, label);
    }
}
