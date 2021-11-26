

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using Unity.EditorCoroutines.Editor;

using System.Reflection;
using System.IO;

namespace LookDev.Editor
{

    public enum DCCType
    {
        MAYA,
        MAX,
        BLENDER
    }

    public enum CommandType
    {
        IMPORT_FBX,
        EXPORT_FBX,
        CREATE_MATERIAL,
        CREATE_TEXTURE,
        LINK_TEXTURETOMATERIAL,
        CUSTOM_COMMAND
    }

    public class UnityToDCC : EditorWindow
    {

        EditorCoroutine routine;

        MaxCommands maxCommand;
        MayaCommands mayaCommand;
        BlenderCommands blenderCommands;

        bool isMaxConnected = false;
        bool isMayaConnected = false;
        bool isBlenderConnected = false;

        DCCType dCCType;
        CommandType commandType;


        string userScript;




        private void OnEnable()
        {
            maxCommand = new MaxCommands("localhost", 6001, 4096);
            mayaCommand = new MayaCommands("localhost", 6000, 4096);
            blenderCommands = new BlenderCommands("localhost", 6002, 4096);
        }


        //[MenuItem("DCC Sync/DCC Linker")]
        static void ShowWindow()
        {

            System.Type type = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.InspectorWindow");

            UnityToDCC win = EditorWindow.GetWindow<UnityToDCC>("Unity To DCC", true, type);

            win.Show();

            win.CheckConnectionStatus();
        }



        IEnumerator ConnectToMaya()
        {
            yield return null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Link Status", EditorStyles.boldLabel);

            GUILayout.BeginVertical("Box");

            GUILayout.Label(string.Format("Is Max Connected : {0}", isMaxConnected.ToString()));
            GUILayout.Label(string.Format("Is Maya Connected : {0}", isMayaConnected.ToString()));
            GUILayout.Label(string.Format("Is Blender Connected : {0}", isBlenderConnected.ToString()));


            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh Connections"))
            {
                CheckConnectionStatus();
            }

            GUILayout.EndVertical();

            EditorGUILayout.Space();



            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);

            GUILayout.BeginVertical("Box");

            dCCType = (DCCType)EditorGUILayout.EnumPopup("Target DCC:", dCCType);
            commandType = (CommandType)EditorGUILayout.EnumPopup("Command:", commandType);

            string label = string.Empty;

            switch (commandType)
            {
                case CommandType.IMPORT_FBX:
                case CommandType.EXPORT_FBX:
                    label = "Target FBX:";
                    break;
                case CommandType.CUSTOM_COMMAND:
                    label = "Input Script:";
                    break;
            }


            GUILayout.BeginHorizontal("Box");
            userScript = EditorGUILayout.TextField(label, userScript);
            if (GUILayout.Button("...", GUILayout.Width(40)))
            {
                switch (commandType)
                {
                    case CommandType.IMPORT_FBX:
                        userScript = EditorUtility.OpenFilePanel("Import FBX", Application.dataPath, "fbx");
                        break;

                    case CommandType.EXPORT_FBX:
                        userScript = EditorUtility.SaveFilePanel("Export FBX", Application.dataPath, string.Empty, "fbx");
                        break;

                    case CommandType.CUSTOM_COMMAND:
                        break;
                }
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Run Command"))
            {
                switch (dCCType)
                {
                    case DCCType.MAYA:
                        mayaCommand.SendPredesinedMessageToMaya(commandType, new string[] { userScript });
                        break;

                    case DCCType.MAX:
                        maxCommand.SendPredesinedMessageToMax(commandType, new string[] { userScript });
                        break;

                    case DCCType.BLENDER:
                        blenderCommands.SendPredesinedMessageToBlender(commandType, new string[] { userScript });
                        break;
                }

                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.Focus();
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Called the Command"), 3.5f);
                }


            }

            GUILayout.EndVertical();

            /*
            if (GUILayout.Button("Send Max Script"))
            {
                maxCommand.SendMessageToMax("importFile \"d:/Bag_01_Snaps014.fbx\" #noPrompt");
            }

            if (GUILayout.Button("Send Maya Script"))
            {
                mayaCommand.SendMessageToMaya("CreatePolygonSphere;");
            }
            */
        }

        void CheckConnectionStatus()
        {
            isMaxConnected = maxCommand.IsConnectedMax() ? true : false;
            isMayaConnected = mayaCommand.IsConnectedMaya() ? true : false;
            isBlenderConnected = blenderCommands.IsConnectedBlender() ? true : false;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Focus();
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Updated the connection status"), 3.5f);
            }
        }
    }
}
#endif

