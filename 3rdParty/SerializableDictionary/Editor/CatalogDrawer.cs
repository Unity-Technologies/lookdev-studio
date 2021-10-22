using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using UnityEditor;
using UnityEditorInternal;

[CustomPropertyDrawer(typeof(Catalog), true)]
public class CatalogDrawer : PropertyDrawer {

    private ReorderableList list;
    private int numValues;
    private bool autoKeys;

    private Func<Rect> VisibleRect;

    private GUIContent[] labels;
    private float     [] labelWidths;
    private float     [] widths;

    const float padding       = 3f;
    const float doublePadding = 2f*padding;

    protected new FieldInfo fieldInfo;

    private void EnsureListExists (SerializedProperty property) {
        if (list?.serializedProperty == null) {

            var arrayProp = property.Copy();
            if (!arrayProp.isArray)
                arrayProp.Next(true);

            if (!arrayProp.isArray)
                return;

            if (fieldInfo == null) {
                if (base.fieldInfo == null)
                    fieldInfo = SerializedPropertyHelper.GetFieldInfoOfProperty(arrayProp);
                else
                    fieldInfo = base.fieldInfo;
            }

            list = new ReorderableList(property.serializedObject, arrayProp, true, false, true, true);

            list.elementHeightCallback = (int index) => {
                var element    = list.serializedProperty.GetArrayElementAtIndex(index);
                var end        = element.GetEndProperty(true);
                var maxHeight = 0f;

                // Move to the first field of the element
                element.Next(true);

                for (int i = 0; !SerializedProperty.EqualContents(element, end); i++) {
                    maxHeight = Mathf.Max(maxHeight, EditorGUI.GetPropertyHeight(element, true));
                    element.NextVisible(false);
                }
                return maxHeight;
            };

            autoKeys = fieldInfo.GetCustomAttributes(typeof(AutomaticKeysAttribute), true).Length != 0;
            if (autoKeys) {
                list.onAddCallback = (ReorderableList list) => {
                    var newIndex = list.serializedProperty.arraySize;
                    list.serializedProperty.arraySize += 1;

                    var keyProp = list.serializedProperty.GetArrayElementAtIndex(newIndex);
                    keyProp.Next(true);

                    keyProp.stringValue = (new System.Random()).Next().ToString();
                    list.serializedProperty.serializedObject.ApplyModifiedProperties();
                };
            }

            labels      = new GUIContent[0];
            labelWidths = new float[0];
            widths      = new float[0];
        }
    }

    private GUIContent arraySizeLabel;

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        EnsureListExists( property );

        if (list == null)
            return;

        if (numValues == 0 && list.serializedProperty.arraySize > 0) {
            var element    = list.serializedProperty.GetArrayElementAtIndex(0);
            var end        = element.GetEndProperty(true);

            // Move to the first field of the element
            element.Next(true);

            var layoutAttributeArray = fieldInfo.GetCustomAttributes(typeof(ValueLayoutAttribute), true);

            var layoutAttributes = (ValueLayoutAttribute) (layoutAttributeArray.Length > 0 ? layoutAttributeArray[0] : null);

            var labelsList      = new List<GUIContent>();
            var labelWidthsList = new List<float>();
            var widthsList      = new List<float>();

            var firstVisible = autoKeys ? 1 : 0;

            for (int i = 0; !SerializedProperty.EqualContents(element, end); i++) {
                numValues++;
                var fLabel = layoutAttributes?.GetLabel(i);
                var width  = layoutAttributes?.GetWidth(i) ?? GetPropertyDefaultWidth(element) ?? 0f;

                labelsList     .Add( new GUIContent (string.IsNullOrEmpty(fLabel) ? element.displayName : fLabel) );
                labelWidthsList.Add( GUI.skin.label.CalcSize(labelsList[i]).x + doublePadding );
#if UNITY_2020_2_OR_NEWER
                widthsList     .Add( width switch {
                    0f                                => 0f,
                    var _ when (i == firstVisible ||
                                i == numValues      ) => width + padding,
                    _                                 => width + doublePadding,
                });
#else
                widthsList.Add(
                    width == 0f
                      ? 0f
                      : (i == firstVisible || i == numValues)
                        ? width+padding
                        : width+doublePadding );
#endif
                element.NextVisible(false);
            }

            numValues--;

            labels      = labelsList     .ToArray();
            labelWidths = labelWidthsList.ToArray();
            widths      = widthsList     .ToArray();

            list.drawElementCallback = (rect, index, isActive, focused) => DrawListItems(rect, index, isActive, focused, numValues);
        }

        var firstLine = position;
        firstLine.height = list.headerHeight + 1;

        var arraySizeRect = new Rect(firstLine.xMax - 48f, firstLine.y, 48f, EditorGUIUtility.singleLineHeight);

        firstLine.width -= 48f;

        property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(firstLine, property.isExpanded, label);
        EditorGUI.EndFoldoutHeaderGroup();


        SerializedProperty arraySizeProp;
        if (property.isArray)
            arraySizeProp = property.FindPropertyRelative("Array.size");
        else {
            var p = property.Copy();
            p.Next(true);
            arraySizeProp = p.FindPropertyRelative("Array.size");
        }
        EditorGUI.PropertyField (arraySizeRect, arraySizeProp, GUIContent.none);

        if (arraySizeLabel == null)
            arraySizeLabel = new GUIContent("", "Array Size");

        EditorGUI.LabelField(arraySizeRect, arraySizeLabel);


        if (property.isExpanded) {
            position.y += firstLine.height;

            if (VisibleRect == null) {
                 var tyGUIClip = Type.GetType("UnityEngine.GUIClip,UnityEngine");
                 if (tyGUIClip != null) {
                    var piVisibleRect = tyGUIClip.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (piVisibleRect != null) {

                        var getMethod = piVisibleRect.GetGetMethod(true) ?? piVisibleRect.GetGetMethod(false);
                        VisibleRect = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), getMethod);
                    }
                 }
            }

            var vRect = VisibleRect();
            vRect.y      -= position.y;
            vRect.height -= position.y;

            if (elementIndex == null)
                elementIndex = new GUIContent();

            list.DoList(position/*, TODO unity is working on the VisibleRect being bugged vRect*/); // https://fogbugz.unity3d.com/default.asp?1322766_8rsv0h3intsq5nb8
        }
    }

    static float? GetPropertyDefaultWidth (SerializedProperty prop) {
#if UNITY_2020_2_OR_NEWER
        return prop.propertyType switch {
            SerializedPropertyType.Integer    => 85f,    // Max for a negative 32-bit number
            //SerializedPropertyType.Integer    => 110f, // appropriate for narrow Range slider
            SerializedPropertyType.Boolean    => 15f,
            SerializedPropertyType.Float      => 60f,    // appropriate for -8888.88
            SerializedPropertyType.Color      => 50f,
            SerializedPropertyType.LayerMask  => 110f, // appropriate for 'Ignore Raycast'
            SerializedPropertyType.Vector2    => 160f, // appropriate for 99.99
            SerializedPropertyType.Vector3    => 160f,
            SerializedPropertyType.Vector4    => 160f,
            SerializedPropertyType.Character  => 18f,
            SerializedPropertyType.Gradient   => 100f,
            SerializedPropertyType.Quaternion => 180f, // appropriatee for 288.88
            SerializedPropertyType.Vector2Int => 160f,
            SerializedPropertyType.Vector3Int => 160f,
            _ => null
        };
#else
        switch (prop.propertyType) {
            case SerializedPropertyType.Integer:
                return 85f;
            //case SerializedPropertyType.Integer => 110f, // appropriate for narrow Range slider
            case SerializedPropertyType.Boolean:
                return 15f;
            case SerializedPropertyType.Float:
                return 60f;
            case SerializedPropertyType.Color:
                return 50f;
            case SerializedPropertyType.LayerMask:
                return 110f;
            case SerializedPropertyType.Vector2:
                return 160f;
            case SerializedPropertyType.Vector3:
                return 160f;
            case SerializedPropertyType.Vector4:
                return 160f;
            case SerializedPropertyType.Character:
                return 18f;
            case SerializedPropertyType.Gradient:
                return 100f;
            case SerializedPropertyType.Quaternion:
                return 180f;
            case SerializedPropertyType.Vector2Int:
                return 160f;
            case SerializedPropertyType.Vector3Int:
                return 160f;
            default:
                return null;
        }
#endif
    }

    private static void IndentExpandedProperty (SerializedProperty property, ref Rect rect) {
        switch (property.propertyType) {
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.Vector4:
                rect.x     += 10f;
                rect.width -= 10f;
                break;
            default:
                break;
        }
    }

    private static GUIContent elementIndex;

    // Possible improvement for auto-flex fields;
    //  - Estimate min size based on type (bool doesn't need as much space as string!)
    //  - If the inspector is too narrow, increase number of lines per entry (but that requires GetPropertyHeight to know the width!)
    void DrawListItems(Rect totalRect, int index, bool isActive, bool isFocused, int numValues) {
        SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);

        EditorGUI.BeginProperty(totalRect, elementIndex, element);

        var prevLabelWidth = EditorGUIUtility.labelWidth;

        var numFlexWidthValues = numValues + (autoKeys ? 0 : 1);

        var availableSpace = totalRect.width;
        for (int i = 0; i < widths.Length; i++) {
            var w = widths[i];
            if (w != 0) {
                availableSpace -= labelWidths[i] + w;
                numFlexWidthValues -= 1;
            }
        }

        var widthPerValue = availableSpace / numFlexWidthValues;
        var walkingRect = totalRect;
        walkingRect.width  = widthPerValue - padding;
        walkingRect.y += 1f;
        walkingRect.height -= 2f;

        element.Next(true);
        if (autoKeys)
            element.NextVisible(false);

        var startAt = (autoKeys ? 1 : 0 );
        for (int i = startAt; i <= numValues; i++) {
            if (i == startAt + 1) {
                walkingRect.x     += padding;
                walkingRect.width -= padding;
            }
            if (i == numValues)
                walkingRect.width += padding;

            var indentedRect = walkingRect;
            IndentExpandedProperty(element, ref indentedRect);

            EditorGUIUtility.labelWidth = labelWidths[i];

            var w = widths[i];
            if (w != 0) {
                var wPad = (i == startAt   ? 0f : padding) +  // First line doesn't pad before
                           (i == numValues ? 0f : padding)  ; // Last line doesn't pad after
                var wWidth = w + labelWidths[i] - wPad;
                EditorGUI.PropertyField(new Rect(indentedRect.x, indentedRect.y, wWidth, indentedRect.height), element, labels[i], true);
                walkingRect.x += (w + EditorGUIUtility.labelWidth);
            }
            else {
                EditorGUI.PropertyField(indentedRect, element, labels[i], true);
                walkingRect.x += widthPerValue;
            }

            element.NextVisible(false);
        }

        EditorGUIUtility.labelWidth = prevLabelWidth;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        EnsureListExists( property );

        if (property.isExpanded) {
            if (!property.isArray)
                property.Next(true);
            return list.GetHeight() + 21f;
        }
        else
            return EditorGUIUtility.singleLineHeight;
    }
}

