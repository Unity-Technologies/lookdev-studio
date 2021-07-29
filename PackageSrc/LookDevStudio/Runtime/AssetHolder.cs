using UnityEngine;

namespace LookDev
{
    public class AssetHolder : MonoBehaviour
    {
        private string m_owner;

        public string Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
    }
}