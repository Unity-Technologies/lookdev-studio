using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace LookDev.Editor
{
    public class SearchPopup : PopupWindowContent
    {
        public override void OnGUI(Rect rect)
        {
            if (GUILayout.Button("Duplicate Assets"))
            {
                AssetManageHelpers.DuplicateSelectedAssets();
            }

            if (GUILayout.Button("Rename Assets"))
            {
                AssetManageHelpers.RenameSelectedAssets();
            }

            if (GUILayout.Button("Delete Assets"))
            {
                AssetManageHelpers.DeleteSelectedAssets();
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
            return new Vector2(170, 75);
        }
        
    }
}