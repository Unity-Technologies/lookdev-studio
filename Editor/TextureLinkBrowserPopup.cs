using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{
    public class TextureLinkBrowserPopup : PopupWindowContent
    {

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Texture menu", EditorStyles.boldLabel);

            if (GUILayout.Button("Select"))
            {
                TextureLinkBrowser.Inst.DisplayCurrentTexture();
                editorWindow.Close();
            }

            if (GUILayout.Button("Duplicate"))
            {
                TextureLinkBrowser.Inst.DuplicateTexture();
                editorWindow.Close();
            }

        }


        public override void OnOpen()
        {
            //base.OnOpen();
        }

        public override void OnClose()
        {
            //base.OnClose();
        }

        
        public override Vector2 GetWindowSize()
        {
            return new Vector2(115, 70);
        }
        
    }
}