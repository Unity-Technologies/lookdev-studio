using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    public class ColliderNameRules : ScriptableObject
    {
        static readonly string colliderNameRuleAsset = "Assets/LookDev/Settings/ColliderRule/ColliderAutoGenerate.asset";

        [Header("Collider Prefix Rules")]
        public string boxCollider;
        public string capsuleCollider;
        public string sphereCollider;
        public string meshConvexCollider;
        public string meshCollider;

        public static string DefaultColliderAssetPath
        {
            get
            {
                return colliderNameRuleAsset;
            }
        }
    }
}