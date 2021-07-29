using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    public class QuickControl : EditorWindow
    {
        //NOTE: Hiding for the sake of the warning
        //private float m_lastCameraScale = 0f;

        MaterialBrowserPopup contentPopup = new MaterialBrowserPopup();

        public void OnGUI()
        {
            LookDevHelpers.DragDrop();

            LightingPresetSceneChanger.OnGUI();
            
            QuickImportControl();

        }

        void QuickImportControl()
        {
            GUILayout.BeginArea(new Rect(1,position.height - 40, Screen.width, 40));

            EditorGUILayout.BeginHorizontal("Box", GUILayout.Width(position.width - 8f));

            if (GUILayout.Button("IMPORT", GUILayout.Height(32)))
            {
                AssetManageHelpers.ImportAsset();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMatPreset"), "Create Material by Presets"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                var activatorRect = GUILayoutUtility.GetLastRect();
                Vector2 vec = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

                activatorRect.x = activatorRect.x + Event.current.mousePosition.x;
                activatorRect.y = activatorRect.y + Event.current.mousePosition.y;

                PopupWindow.Show(activatorRect, contentPopup);

            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_AssignMat"), "Texture Allocator"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                TextureLinkBrowser.Inst.InitTextureLinkBrowserBySelection();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        public Rect GetCenterPositionOfTheBrowser(Vector2 popupWindowSize)
        {
            int OffsetX = (int)(((float)(position.width) * 0.5f) - ((float)popupWindowSize.x * 0.5f));
            int OffsetY = 100;//(int)((float)Screen.currentResolution.height * 0.5f);

            Rect activatorRect = new Rect(new Vector2(OffsetX, OffsetY), popupWindowSize);

            return activatorRect;
        }

        public Rect GetCenterPositionFromWindow(Vector2 WindowSize)
        {
            int OffsetX = (int)(position.x + ((float)(position.width) * 0.5f) - ((float)WindowSize.x * 0.5f));
            int OffsetY = (int)((float)Screen.currentResolution.height * 0.5f);

            Rect activatorRect = new Rect(new Vector2(OffsetX, OffsetY), WindowSize);

            return activatorRect;
        }


        void QuickAssetControls()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(position.width - 8f));

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DupMat"), "Duplicate Selected Assets"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                AssetManageHelpers.DuplicateSelectedAssets();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_Rename"), "Rename Selected Assets"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                AssetManageHelpers.RenameSelectedAssets();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DelMat"), "Delete Selected Assets"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                AssetManageHelpers.DeleteSelectedAssets();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMatPreset"), "Create Material by the Presets"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                int OffsetX = (int)(((float)(position.x + position.width) * 0.5f) - ((float)contentPopup.GetWindowSize().x * 0.5f));
                int OffsetY = (int)((float)Screen.currentResolution.height * 0.5f);

                Rect activatorRect = new Rect(new Vector2(OffsetX, OffsetY), contentPopup.GetWindowSize());

                PopupWindow.Show(activatorRect, contentPopup);
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_AssignMat"), "Link Textures"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                TextureLinkBrowser.Inst.InitTextureLinkBrowserBySelection();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}