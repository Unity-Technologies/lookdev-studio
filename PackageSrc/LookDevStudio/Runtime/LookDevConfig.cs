using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookDev
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/LookDevConfig", order = 1)]
    public class LookDevConfig : ScriptableObject
    {
        public MaterialBallArray[] Layouts;
    }
}