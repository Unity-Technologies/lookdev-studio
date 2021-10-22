using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    [CreateAssetMenu]
    public class LookDevSession : ScriptableObject
    {
        public Guid Guid;
        public string[] Assets;
        public SerializableDictionary<string, string> Directives = new SerializableDictionary<string, string>();
    }

    [Serializable]
    public struct Guid : IEquatable<Guid>
    {
        [SerializeField, HideInInspector] uint m_Value0;
        [SerializeField, HideInInspector] uint m_Value1;
        [SerializeField, HideInInspector] uint m_Value2;
        [SerializeField, HideInInspector] uint m_Value3;

        public uint Value0 => m_Value0;
        public uint Value1 => m_Value1;
        public uint Value2 => m_Value2;
        public uint Value3 => m_Value3;

        public Guid(uint val0, uint val1, uint val2, uint val3)
        {
            m_Value0 = val0;
            m_Value1 = val1;
            m_Value2 = val2;
            m_Value3 = val3;
        }

        public Guid(string hexString)
        {
            m_Value0 = 0U;
            m_Value1 = 0U;
            m_Value2 = 0U;
            m_Value3 = 0U;
            TryParse(hexString, out this);
        }

        public Guid(GUID guid)
        {
            m_Value0 = (uint) guid.GetType().GetField("m_Value0", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(guid);
            m_Value1 = (uint) guid.GetType().GetField("m_Value1", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(guid);
            m_Value2 = (uint) guid.GetType().GetField("m_Value2", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(guid);
            m_Value3 = (uint) guid.GetType().GetField("m_Value3", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(guid);
        }

        public string ToHexString()
        {
            return $"{m_Value0:X8} {m_Value1:X8} {m_Value2:X8} {m_Value3:X8}";
        }

        static void TryParse(string hexString, out Guid guid)
        {
            guid.m_Value0 = Convert.ToUInt32(hexString.Substring(0, 8), 16);
            guid.m_Value1 = Convert.ToUInt32(hexString.Substring(8, 8), 16);
            guid.m_Value2 = Convert.ToUInt32(hexString.Substring(16, 8), 16);
            guid.m_Value3 = Convert.ToUInt32(hexString.Substring(24, 8), 16);
        }

        public static bool operator ==(Guid x, Guid y) => x.m_Value0 == y.m_Value0 && x.m_Value1 == y.m_Value1 &&
                                                          x.m_Value2 == y.m_Value2 && x.m_Value3 == y.m_Value3;

        public static bool operator !=(Guid x, Guid y) => !(x == y);
        public bool Equals(Guid other) => this == other;
        public override bool Equals(object obj) => obj != null && obj is GUID && Equals((GUID) obj);

        public override int GetHashCode() =>
            (((int) m_Value0 * 397 ^ (int) m_Value1) * 397 ^ (int) m_Value2) * 397 ^ (int) m_Value3;
    }

    [CustomPropertyDrawer(typeof(Guid))]
    public class GuidDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var value0 = property.FindPropertyRelative("m_Value0");
            var value1 = property.FindPropertyRelative("m_Value1");
            var value2 = property.FindPropertyRelative("m_Value2");
            var value3 = property.FindPropertyRelative("m_Value3");

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            EditorGUI.SelectableLabel(position,
                $"{(uint) value0.intValue:X8} {(uint) value1.intValue:X8} {(uint) value2.intValue:X8} {(uint) value3.intValue:X8} ");

            EditorGUI.EndProperty();
        }
    }
}