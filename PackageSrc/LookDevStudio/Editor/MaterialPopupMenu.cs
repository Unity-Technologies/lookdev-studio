using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{
    public class MaterialPopupMenu : EditorWindow
    {

        static MaterialPopupMenu m_MaterialMenuPopup;
        static public void OpenPopup(Rect initPos)
        {
            if (m_MaterialMenuPopup == null)
            {
                m_MaterialMenuPopup = CreateInstance<MaterialPopupMenu>();
            }

            m_MaterialMenuPopup.position = initPos;
            m_MaterialMenuPopup.minSize = m_MaterialMenuPopup.maxSize;

            m_MaterialMenuPopup.ShowPopup();
        }

        private void OnGUI()
        {
            
            if (Event.current.keyCode == KeyCode.Escape)
            {
                m_MaterialMenuPopup?.Close();
            }

            if (GUILayout.Button("Link Textures"))
            {
                m_MaterialMenuPopup?.Close();

            }

            if (GUILayout.Button("Rename"))
            {
                m_MaterialMenuPopup?.Close();

            }
        }
    }
}