
#if UNITY_EDITOR

using UnityEngine;
using System.Net.Sockets;

using UnityEditor;
using UnityEditor.ProjectWindowCallback;

using System.IO;

using System.Collections;
using System.Collections.Generic;

namespace LookDev.Editor
{
    public class MayaCommands : ICommands
    {

        private string hostName = "localhost";
        private int port = 6000;

        private int byteBufferSize = 4096;


        static string currentAssetPath;
        static string currentAssetFullPath;


        static string GetFBXPathOnSelectedGameObject()
        {
            if (Selection.activeGameObject != null)
            {
                string pathToPrefab = string.Empty;
                string fullPath = string.Empty;

                if (PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) == PrefabAssetType.Model)
                {
                    pathToPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.activeGameObject);
                }
                else if (PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) == PrefabAssetType.Variant || PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) == PrefabAssetType.Regular)
                {
                    GameObject go = PrefabUtility.GetCorrespondingObjectFromOriginalSource(Selection.activeGameObject);

                    if (go != null)
                        pathToPrefab = AssetDatabase.GetAssetPath((Object)go);
                }
                else
                {
                    Debug.LogError("Failed to load. only Model can be loaded.");
                    return string.Empty;
                }

                if (pathToPrefab != string.Empty)
                {
                    fullPath = Path.GetFullPath(pathToPrefab);
                }

                if (Path.GetExtension(fullPath).ToLower() != ".fbx")
                    fullPath = string.Empty;

                fullPath = fullPath.Replace("\\", "/");

                Debug.Log(fullPath);

                if (File.Exists(fullPath))
                {
                    currentAssetFullPath = fullPath;
                    currentAssetPath = pathToPrefab;
                }

                return fullPath;

            }
            else
                return string.Empty;
        }


        [MenuItem("GameObject/Maya/Load Model To Maya", false, 20)]
        static void LoadSelectedObjectToMaya()
        {
            string targetFBX = GetFBXPathOnSelectedGameObject();

            //DCCLauncher.Load(DCCType.MAYA, "C:/MayaCommand/MayaCommand/Assets/hammer.fbx");
            DCCLauncher.Load(DCCType.MAYA, targetFBX);

            // Load Materials and Textures

        }

        [MenuItem("GameObject/Maya/Save Model From Maya", false, 20)]
        static void SaveSelectedObjectFromMaya()
        {
            if (File.Exists(currentAssetPath))
            {
                DCCLauncher.Save(DCCType.MAYA, currentAssetFullPath);
                AssetDatabase.ImportAsset(currentAssetPath, ImportAssetOptions.Default);
            }
        }


        /*
        [MenuItem("Assets/Maya/Create Material")]
        static void CreateMaterial()
        {

            Material newMaterial = new Material(Shader.Find("HDRP/Autodesk Interactive/AutodeskInteractive"));
            ProjectWindowUtil.CreateAsset(newMaterial, "NewMaterial.mat");

        }
        */


        [MenuItem("Assets/Maya/Load Material to Maya")]
        static void LoadSelectedMaterialToMaya()
        {
            if (Selection.activeObject.GetType() == typeof(Material))
            {
                DCCLauncher.CreateMaterial(DCCType.MAYA, Selection.activeObject.name);

                LinkTexturesOnSelectedMaterialToMaya();
                LinkFloatPropertiesOnSelectedMaterialToMaya();
                LinkColorPropertiesOnSelectedMaterialToMaya();
                LinkVectorPropertiesOnSelectedMaterialToMaya();

            }
        }


        //[MenuItem("Assets/Maya/Link the material's Textures to Maya")]
        static void LinkTexturesOnSelectedMaterialToMaya()
        {
            Material curMat = (Material)Selection.activeObject;

            string targetMaterialName = curMat.name;

            int propertyCount = curMat.shader.GetPropertyCount();

            for (int i = 0; i < curMat.shader.GetPropertyCount(); i++)
            {
                if (curMat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    Debug.Log(curMat.shader.GetPropertyName(i));

                    string texPropertyName = curMat.shader.GetPropertyName(i);
                    Texture linkedTexture = curMat.GetTexture(texPropertyName);

                    string linkedTexturePath = string.Empty;

                    if (linkedTexture != null)
                    {
                        linkedTexturePath = AssetDatabase.GetAssetPath(linkedTexture);
                        if (linkedTexturePath != string.Empty)
                        {
                            linkedTexturePath = Path.GetFullPath(linkedTexturePath);
                            linkedTexturePath = linkedTexturePath.Replace("\\", "/");
                        }
                    }

                    Debug.Log(linkedTexturePath);


                    string sourceTextureName = Path.GetFileNameWithoutExtension(linkedTexturePath);
                    // Create textures
                    DCCLauncher.CreateTexture(DCCType.MAYA, sourceTextureName, linkedTexturePath);

                    // Find AttributeName on Maya
                    string targetAttributeName = FindMaterialAttributeNameForTexture(texPropertyName);

                    // Link texture to Material
                    DCCLauncher.LinkTextureToMaterial(DCCType.MAYA, sourceTextureName, "outColor", targetMaterialName, targetAttributeName);

                }
            }
        }


        //[MenuItem("Assets/Maya/Link the material's Float Properties to Maya")]
        static void LinkFloatPropertiesOnSelectedMaterialToMaya()
        {
            Material curMat = (Material)Selection.activeObject;

            string targetMaterialName = curMat.name;

            int propertyCount = curMat.shader.GetPropertyCount();

            string mergedCommands = string.Empty;

            for (int i = 0; i < curMat.shader.GetPropertyCount(); i++)
            {
                if (curMat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Float)
                {
                    Debug.Log(curMat.shader.GetPropertyName(i));

                    string floatPropertyName = curMat.shader.GetPropertyName(i);
                    float linkedFloat = curMat.GetFloat(floatPropertyName);

                    // Find AttributeName on Maya
                    string targetAttributeName = FindMaterialAttributeNameForFloatValue(floatPropertyName);

                    // setAttr "NewMat.metallic" 1;
                    string cmd = string.Format("setAttr \"{0}.{1}\" {2};", targetMaterialName, targetAttributeName, linkedFloat.ToString());
                    mergedCommands = mergedCommands + cmd;

                    Debug.Log(cmd);
                }
            }

            if (mergedCommands != string.Empty)
                DCCLauncher.SendCommand(DCCType.MAYA, mergedCommands);
        }


        //[MenuItem("Assets/Maya/Link the material's Color Properties to Maya")]
        static void LinkColorPropertiesOnSelectedMaterialToMaya()
        {
            Material curMat = (Material)Selection.activeObject;

            string targetMaterialName = curMat.name;

            int propertyCount = curMat.shader.GetPropertyCount();

            string mergedCommands = string.Empty;

            for (int i = 0; i < curMat.shader.GetPropertyCount(); i++)
            {
                if (curMat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color)
                {
                    Debug.Log(curMat.shader.GetPropertyName(i));

                    string colorPropertyName = curMat.shader.GetPropertyName(i);
                    Color linkedColor = curMat.GetColor(colorPropertyName);


                    Debug.Log(linkedColor);




                    // Find AttributeName on Maya
                    string targetAttributeName = FindMaterialAttributeName("MAYA_ColorLinkInfo", colorPropertyName);

                    string cmd = string.Empty;


                    // Exceptional case!!
                    if (targetAttributeName == "emissive")
                    {
                        cmd = string.Format("setAttr \"{0}.{1}\" -type double3 {2} {3} {4};", targetMaterialName, targetAttributeName, linkedColor.r, linkedColor.g, linkedColor.b);
                        mergedCommands = mergedCommands + cmd;

                        Color baseLinearColor;
                        float intensity;

                        DecomposeHdrColor(linkedColor, out baseLinearColor, out intensity);


                        cmd = string.Format("setAttr \"{0}.{1}\" -type double3 {2} {3} {4};", targetMaterialName, targetAttributeName, baseLinearColor.r, baseLinearColor.g, baseLinearColor.b);
                        mergedCommands = mergedCommands + cmd;


                        // emissive_intensity
                        string emissiveIntensityCmd = string.Format("setAttr \"{0}.{1}\" {2};", targetMaterialName, "emissive_intensity", intensity);
                        mergedCommands = mergedCommands + emissiveIntensityCmd;
                    }
                    else
                    {
                        cmd = string.Format("setAttr \"{0}.{1}\" -type double3 {2} {3} {4};", targetMaterialName, targetAttributeName, linkedColor.r, linkedColor.g, linkedColor.b);
                        mergedCommands = mergedCommands + cmd;
                    }

                }
            }

            if (mergedCommands != string.Empty)
                DCCLauncher.SendCommand(DCCType.MAYA, mergedCommands);
        }


        //[MenuItem("Assets/Maya/Link the material's Vector Properties to Maya")]
        static void LinkVectorPropertiesOnSelectedMaterialToMaya()
        {
            Material curMat = (Material)Selection.activeObject;

            string targetMaterialName = curMat.name;

            int propertyCount = curMat.shader.GetPropertyCount();

            string mergedCommands = string.Empty;

            for (int i = 0; i < curMat.shader.GetPropertyCount(); i++)
            {
                if (curMat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Vector)
                {
                    Debug.Log(curMat.shader.GetPropertyName(i));

                    string vectorPropertyName = curMat.shader.GetPropertyName(i);
                    Color linkedVector = curMat.GetColor(vectorPropertyName);


                    Debug.Log(linkedVector);


                    // Find AttributeName on Maya
                    string targetAttributeName = FindMaterialAttributeName("MAYA_VectorLinkInfo", vectorPropertyName);

                    // setAttr "NewMat.uv_offset" -type double2 1 1;
                    string cmd = string.Format("setAttr \"{0}.{1}\" -type double2 {2} {3};", targetMaterialName, targetAttributeName, linkedVector.r, linkedVector.g);
                    mergedCommands = mergedCommands + cmd;

                    Debug.Log(cmd);

                }
            }

            if (mergedCommands != string.Empty)
                DCCLauncher.SendCommand(DCCType.MAYA, mergedCommands);
        }



        //[MenuItem("Assets/Maya/Link the material's Range Properties to Maya")]
        static void LinkRangePropertiesOnSelectedMaterialToMaya()
        {
            Material curMat = (Material)Selection.activeObject;

            string targetMaterialName = curMat.name;

            int propertyCount = curMat.shader.GetPropertyCount();

            string mergedCommands = string.Empty;

            for (int i = 0; i < curMat.shader.GetPropertyCount(); i++)
            {
                if (curMat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Range)
                {
                    Debug.Log(curMat.shader.GetPropertyName(i));

                    /*
                    string vectorPropertyName = curMat.shader.GetPropertyName(i);
                    Color linkedVector = curMat.GetColor(vectorPropertyName);


                    Debug.Log(linkedVector);


                    // Find AttributeName on Maya
                    string targetAttributeName = FindMaterialAttributeName("MAYA_VectorLinkInfo", vectorPropertyName);

                    // setAttr "NewMat.uv_offset" -type double2 1 1;
                    string cmd = string.Format("setAttr \"{0}.{1}\" -type double2 {2} {3};", targetMaterialName, targetAttributeName, linkedVector.r, linkedVector.g);
                    mergedCommands = mergedCommands + cmd;

                    Debug.Log(cmd);
                    */

                }
            }

            if (mergedCommands != string.Empty)
                DCCLauncher.SendCommand(DCCType.MAYA, mergedCommands);
        }


        static float GetHDRIntensity(Color emissiveColor)
        {
            const byte k_MaxByteForOverexposedColor = 191; //internal Unity const
            float maxColorComponent = emissiveColor.maxColorComponent;
            float scaleFactor = k_MaxByteForOverexposedColor / maxColorComponent;
            float intensity = Mathf.Log(255f / scaleFactor) / Mathf.Log(2f);

            return intensity;
        }


        public static void DecomposeHdrColor(Color linearColorHdr, out Color baseLinearColor, out float exposure)
        {
            const byte k_MaxByteForOverexposedColor = 191; //internal Unity const

            baseLinearColor = linearColorHdr;
            var maxColorComponent = linearColorHdr.maxColorComponent;
            // replicate Photoshops's decomposition behaviour
            if (maxColorComponent == 0f || maxColorComponent <= 1f && maxColorComponent >= 1 / 255f)
            {
                exposure = 0f;
                baseLinearColor = linearColorHdr;
                /*
                baseLinearColor.r = (byte)Mathf.RoundToInt(linearColorHdr.r * 255f);
                baseLinearColor.g = (byte)Mathf.RoundToInt(linearColorHdr.g * 255f);
                baseLinearColor.b = (byte)Mathf.RoundToInt(linearColorHdr.b * 255f);
                */
            }
            else
            {
                // calibrate exposure to the max float color component
                var scaleFactor = k_MaxByteForOverexposedColor / maxColorComponent;
                exposure = Mathf.Log(255f / scaleFactor) / Mathf.Log(2f);
                // maintain maximal integrity of byte values to prevent off-by-one errors when scaling up a color one component at a time

                baseLinearColor.r = (float)(System.Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.r))) / 255f;
                baseLinearColor.g = (float)(System.Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.g))) / 255f;
                baseLinearColor.b = (float)(System.Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.b))) / 255f;
            }
        }




        static string FindMaterialAttributeNameForTexture(string propertyName)
        {
            string attributeName = string.Empty;

            string[] guids = AssetDatabase.FindAssets("MAYA_TextureLinkInfo", new string[] { "Assets" });

            if (guids.Length != 0)
            {
                string textureMappingFile = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));

                if (File.Exists(textureMappingFile))
                {
                    string[] textureMappingLines = File.ReadAllLines(textureMappingFile);

                    foreach (string textureMappingLine in textureMappingLines)
                    {
                        string[] tokens = textureMappingLine.Split(',');

                        if (tokens.Length == 2)
                        {
                            string unityPropertyName = tokens[0];

                            if (propertyName.ToLower().Trim() == unityPropertyName.ToLower().Trim())
                            {
                                attributeName = tokens[1];
                                break;
                            }
                        }

                    }
                }
            }

            return attributeName;
        }



        // MAYA_FloatLinkInfo

        static string FindMaterialAttributeNameForFloatValue(string propertyName)
        {
            string attributeName = string.Empty;

            string[] guids = AssetDatabase.FindAssets("MAYA_FloatLinkInfo", new string[] { "Assets" });

            if (guids.Length != 0)
            {
                string textureMappingFile = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));

                if (File.Exists(textureMappingFile))
                {
                    string[] textureMappingLines = File.ReadAllLines(textureMappingFile);

                    foreach (string textureMappingLine in textureMappingLines)
                    {
                        string[] tokens = textureMappingLine.Split(',');

                        if (tokens.Length == 2)
                        {
                            string unityPropertyName = tokens[0];

                            if (propertyName.ToLower().Trim() == unityPropertyName.ToLower().Trim())
                            {
                                attributeName = tokens[1];
                                break;
                            }
                        }

                    }
                }
            }

            return attributeName;
        }


        static string FindMaterialAttributeName(string infoFileName, string propertyName)
        {
            string attributeName = string.Empty;

            string[] guids = AssetDatabase.FindAssets(infoFileName, new string[] { "Assets" });

            if (guids.Length != 0)
            {
                string textureMappingFile = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));

                if (File.Exists(textureMappingFile))
                {
                    string[] textureMappingLines = File.ReadAllLines(textureMappingFile);

                    foreach (string textureMappingLine in textureMappingLines)
                    {
                        string[] tokens = textureMappingLine.Split(',');

                        if (tokens.Length == 2)
                        {
                            string unityPropertyName = tokens[0];

                            if (propertyName.ToLower().Trim() == unityPropertyName.ToLower().Trim())
                            {
                                attributeName = tokens[1];
                                break;
                            }
                        }

                    }
                }
            }

            return attributeName;
        }



        static IEnumerator CheckMaterialChanged()
        {
            yield return null;
        }

        public MayaCommands(string inHostName, int inPort, int inBufferSize)
        {
            hostName = inHostName;
            port = inPort;
            byteBufferSize = inBufferSize;
        }

        public void SendPredesinedMessageToMaya(CommandType commandType, string[] args)
        {
            string cmd = string.Empty;

            if (args == null)
            {
                Debug.LogError("Command argument is null");
                return;
            }

            switch (commandType)
            {
                case CommandType.IMPORT_FBX:
                    // file -f -import -type \"FBX\" -ignoreVersion -ra true -mergeNamespacesOnClash false -pr -importFrameRate true -importTimeRange \"override\" $targetFile
                    cmd = string.Format("file -f -import -type \"FBX\" -ignoreVersion -ra true -mergeNamespacesOnClash false -pr -importFrameRate true -importTimeRange \"override\" \"{0}\";", args[0]);
                    break;

                case CommandType.EXPORT_FBX:
                    // file -force -options \"\" -type \"FBX export\" -pr -ea ($outFBX)
                    cmd = string.Format("file -force -options \"\" -type \"FBX export\" -pr -ea \"{0}\"", args[0]);
                    break;

                case CommandType.CUSTOM_COMMAND:
                    cmd = string.Format("{0}", args[0]);
                    break;

                case CommandType.CREATE_MATERIAL:
                    //CreateStingrayPBSMaterialNode(string $matName)
                    cmd = string.Format("CreateStingrayPBSMaterialNode(\"{0}\");", args[0]);
                    break;

                case CommandType.CREATE_TEXTURE:
                    cmd = string.Format("CreateTextureNode(\"{0}\",\"{1}\");", args[0], args[1]);
                    break;

                case CommandType.LINK_TEXTURETOMATERIAL:
                    // LinkTextureToMaterial(string $texAttrName, string $matAttrName)
                    cmd = string.Format("LinkTextureToMaterial(\"{0}\",\"{1}\");", args[0], args[1]);
                    break;
            }

            SendMessageToMaya(cmd);
        }


        public string SendMessageToMaya(string strCmd)
        {
            string responseData = string.Empty;


            Socket sock = null;

            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                sock.Connect(hostName, port);

                byte[] buf = System.Text.Encoding.Default.GetBytes(strCmd);

                sock.Send(buf, 0, buf.Length, SocketFlags.None);

                sock.Close();

            }
            catch
            {
                sock.Close();
            }




            /*
            TcpClient tc = null;
            NetworkStream ns = null;

            try
            {
                tc = new TcpClient(hostName, port);

                byte[] buf = System.Text.Encoding.Default.GetBytes(strCmd);

                ns = tc.GetStream();
                ns.Write(buf, 0, buf.Length);

                byte[] data = new byte[byteBufferSize];

                int bytes = ns.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                ns.Flush();

                ns.Close();
                tc.Close();

                data.Initialize();
            }
            catch(System.Exception ex)
            {
                Debug.LogError(string.Format("Failed To send the Message : {0}", ex.Message));
            }
            finally
            {
                ns.Close();
                tc.Close();
            }
            */


            return responseData;
        }

        public bool IsConnectedMaya()
        {
            TcpClient tc = null;

            try
            {
                tc = new TcpClient(hostName, port);

                if (tc.Connected)
                {
                    tc.Close();
                    return true;
                }
                else
                {
                    tc.Close();
                    return false;
                }
            }
            catch
            {
                if (tc != null)
                    tc.Close();
                return false;
            }
        }

        public bool IsMayaRunning()
        {
            System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcesses();

            for (int i = 0; i < procs.Length; i++)
            {
                if (procs[i].ProcessName.ToLower() == "maya")
                    return true;
            }

            return false;
        }

        public void KillMayaProcess()
        {
            System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcesses();

            for (int i = 0; i < procs.Length; i++)
            {
                if (procs[i].ProcessName.ToLower() == "maya")
                {
                    procs[i].Kill();
                }
            }
        }


    }
}
#endif

