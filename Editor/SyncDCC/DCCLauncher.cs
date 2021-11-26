#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

using UnityEditor;


namespace LookDev.Editor
{
    public static class DCCLauncher
    {
        // Mesh DCCs
        public static string mayaPath; // maya.exe
        public static string maxPath;  // 3dsmax.exe
        public static string blenderPath; // blender.exe

        // Painting Mesh DCCs
        public static string substancePainterPath; // Adobe Substance 3D Painter.exe

        // Texturing DCCs
        public static string photoShopPath; // photoshop.exe


        //public static readonly List<string> DCCPath = new List<string>() { "maya.exe", "3dsmax.exe", "blender.exe" };

        static MayaCommands mayaCommands;
        static MaxCommands maxCommands;
        static BlenderCommands blenderCommands;


        static string SearchDCCFullPath(string targetFile)
        {
            string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            string programFilesX86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

            Stack<string> allFolders = new Stack<string>();
            string[] subDirs = Directory.GetDirectories(programFiles);

            foreach (string subDir in subDirs)
                allFolders.Push(subDir);

            subDirs = Directory.GetDirectories(programFilesX86);

            foreach (string subDir in subDirs)
                allFolders.Push(subDir);

            while (allFolders.Count != 0)
            {
                string currentDir = allFolders.Pop();

                // Check .exe
                string checkFile = Path.Combine(currentDir, targetFile);

                if (File.Exists(checkFile))
                {
                    return checkFile;
                }

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch
                {
                    continue;
                }

                foreach (string subDir in subDirs)
                    allFolders.Push(subDir);
            }

            return string.Empty;
        }


        //[MenuItem("DCC Sync/Get DCC paths")]
        static void GetDCCPath()
        {
            Debug.Log(GetMeshDCCPath(MeshDCCs.Maya));
            Debug.Log(GetTextureDCCPath(PaintingTexDCCs.Photoshop));

        }

        public static string GetMeshDCCPath(MeshDCCs meshDCCType)
        {
            string targetDccFile = string.Empty;

            switch(meshDCCType)
            {
                case MeshDCCs.Maya:
                    targetDccFile = "maya.exe";
                    break;

                case MeshDCCs.Max:
                    targetDccFile = "3dsmax.exe";
                    break;

                /*
                case MeshDCCs.Blender:
                    targetDccFile = "blender.exe";
                    break;
                */

            }

            if (targetDccFile == string.Empty)
                return string.Empty;

            string foundDccPath = SearchDCCFullPath(targetDccFile);

            switch (meshDCCType)
            {
                case MeshDCCs.Maya:
                    mayaPath = foundDccPath;
                    break;

                case MeshDCCs.Max:
                    maxPath = foundDccPath;
                    break;

                /*
                case MeshDCCs.Blender:
                    blenderPath = foundDccPath;
                    break;
                */
            }

            return foundDccPath;
        }

        public static string GetTextureDCCPath(PaintingTexDCCs paintingTexDCCs)
        {
            string targetDccFile = string.Empty;

            switch (paintingTexDCCs)
            {
                case PaintingTexDCCs.Photoshop:
                    targetDccFile = "photoshop.exe";
                    break;
            }

            if (targetDccFile == string.Empty)
                return string.Empty;

            string foundDccPath = SearchDCCFullPath(targetDccFile);

            switch (paintingTexDCCs)
            {
                case PaintingTexDCCs.Photoshop:
                    photoShopPath = foundDccPath;
                    break;
            }

            return foundDccPath;
        }

        public static string GetPaintingMeshDCCPath(PaintingMeshDCCs paintingMeshDCCs)
        {
            string targetDccFile = string.Empty;

            switch (paintingMeshDCCs)
            {
                case PaintingMeshDCCs.Substance_Painter :
                    targetDccFile = "Adobe Substance 3D Painter.exe";
                    break;
            }

            if (targetDccFile == string.Empty)
                return string.Empty;

            string foundDccPath = SearchDCCFullPath(targetDccFile);

            switch (paintingMeshDCCs)
            {
                case PaintingMeshDCCs.Substance_Painter:
                    substancePainterPath = foundDccPath;
                    break;
            }

            return foundDccPath;
        }


        public static void Load(DCCType currentDCCType, string targetFBX)
        {
            if (!File.Exists(targetFBX))
            {
                Debug.LogError(string.Format("Could not find the file : {0}", targetFBX));
                return;
            }


            if (mayaCommands == null)
                mayaCommands = new MayaCommands("localhost", 6000, 4096);

            if (maxCommands == null)
                maxCommands = new MaxCommands("localhost", 6001, 4096);

            if (blenderCommands == null)
                blenderCommands = new BlenderCommands("localhost", 6002, 4096);


            switch (currentDCCType)
            {
                case DCCType.MAYA:
                    
                    if (mayaCommands.IsMayaRunning() && mayaCommands.IsConnectedMaya())
                    {
                        mayaCommands.SendPredesinedMessageToMaya(CommandType.IMPORT_FBX, new string[] { targetFBX });
                    }
                    else
                    {
                        RunMayaWithFBX(targetFBX);
                    }

                    break;

                case DCCType.MAX:

                    if (maxCommands.IsMaxRunning() && maxCommands.IsConnectedMax())
                    {
                        maxCommands.SendPredesinedMessageToMax(CommandType.IMPORT_FBX, new string[] { targetFBX });
                    }
                    else
                    {
                        RunMaxWithFBX(targetFBX);
                    }

                    break;

                case DCCType.BLENDER:

                    if (blenderCommands.IsBlenderRunning() && blenderCommands.IsConnectedBlender())
                    {
                        blenderCommands.SendPredesinedMessageToBlender(CommandType.IMPORT_FBX, new string[] { targetFBX });
                    }
                    else
                    {
                        RunBlenderWithFBX(targetFBX);
                    }

                    break;
            }
        }


        public static void Save(DCCType currentDCCType, string targetFBX)
        {
            GetDCCPath();

            if (mayaCommands == null)
                mayaCommands = new MayaCommands("localhost", 6000, 4096);

            if (maxCommands == null)
                maxCommands = new MaxCommands("localhost", 6001, 4096);

            if (blenderCommands == null)
                blenderCommands = new BlenderCommands("localhost", 6002, 4096);


            switch (currentDCCType)
            {
                case DCCType.MAYA:
                    if (mayaCommands.IsMayaRunning() && mayaCommands.IsConnectedMaya())
                    {
                        mayaCommands.SendPredesinedMessageToMaya(CommandType.EXPORT_FBX, new string[] { targetFBX });
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find the Maya that is running or was connected to Unity.", "Ok");
                        return;
                    }

                    break;

                case DCCType.MAX:
                    break;

                case DCCType.BLENDER:
                    break;
            }

        }


        public static void CreateMaterial(DCCType currentDCCType, string materialName)
        {
            GetDCCPath();

            if (mayaCommands == null)
                mayaCommands = new MayaCommands("localhost", 6000, 4096);

            if (maxCommands == null)
                maxCommands = new MaxCommands("localhost", 6001, 4096);

            if (blenderCommands == null)
                blenderCommands = new BlenderCommands("localhost", 6002, 4096);


            switch (currentDCCType)
            {
                case DCCType.MAYA:
                    //if (mayaCommands.IsMayaRunning() && mayaCommands.IsConnectedMaya())
                    if (mayaCommands.IsMayaRunning())
                    {
                        mayaCommands.SendPredesinedMessageToMaya(CommandType.CREATE_MATERIAL, new string[] { materialName });
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find the Maya that is running or was connected to Unity.", "Ok");
                        return;
                    }

                    break;

                case DCCType.MAX:
                    break;

                case DCCType.BLENDER:
                    break;
            }
        }


        public static void CreateTexture(DCCType currentDCCType, string textureName, string texturePath)
        {
            GetDCCPath();

            if (mayaCommands == null)
                mayaCommands = new MayaCommands("localhost", 6000, 4096);

            if (maxCommands == null)
                maxCommands = new MaxCommands("localhost", 6001, 4096);

            if (blenderCommands == null)
                blenderCommands = new BlenderCommands("localhost", 6002, 4096);


            switch (currentDCCType)
            {
                case DCCType.MAYA:
                    //if (mayaCommands.IsMayaRunning() && mayaCommands.IsConnectedMaya())
                    if (mayaCommands.IsMayaRunning())
                    {
                        mayaCommands.SendPredesinedMessageToMaya(CommandType.CREATE_TEXTURE, new string[] { textureName, texturePath });
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find the Maya that is running or was connected to Unity.", "Ok");
                        return;
                    }

                    break;

                case DCCType.MAX:
                    break;

                case DCCType.BLENDER:
                    break;
            }
        }


        public static void LinkTextureToMaterial(DCCType currentDCCType, string textureName, string textureAttribute, string targetMaterialName, string targetAttributeName)
        {
            if (textureName == string.Empty || textureAttribute == string.Empty || targetMaterialName == string.Empty || targetAttributeName == string.Empty)
                return;

            GetDCCPath();

            if (mayaCommands == null)
                mayaCommands = new MayaCommands("localhost", 6000, 4096);

            if (maxCommands == null)
                maxCommands = new MaxCommands("localhost", 6001, 4096);

            if (blenderCommands == null)
                blenderCommands = new BlenderCommands("localhost", 6002, 4096);


            switch (currentDCCType)
            {
                case DCCType.MAYA:
                    //if (mayaCommands.IsMayaRunning() && mayaCommands.IsConnectedMaya())
                    if (mayaCommands.IsMayaRunning())
                    {
                        mayaCommands.SendPredesinedMessageToMaya(CommandType.LINK_TEXTURETOMATERIAL, new string[] { string.Format("{0}.{1}", textureName, textureAttribute), string.Format("{0}.{1}", targetMaterialName, targetAttributeName) });
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find the Maya that is running or was connected to Unity.", "Ok");
                        return;
                    }

                    break;

                case DCCType.MAX:
                    break;

                case DCCType.BLENDER:
                    break;
            }
        }


        public static void SendCommand(DCCType currentDCCType, string command)
        {
            GetDCCPath();

            if (mayaCommands == null)
                mayaCommands = new MayaCommands("localhost", 6000, 4096);

            if (maxCommands == null)
                maxCommands = new MaxCommands("localhost", 6001, 4096);

            if (blenderCommands == null)
                blenderCommands = new BlenderCommands("localhost", 6002, 4096);


            switch (currentDCCType)
            {
                case DCCType.MAYA:
                    //if (mayaCommands.IsMayaRunning() && mayaCommands.IsConnectedMaya())
                    if (mayaCommands.IsMayaRunning())
                    {
                        mayaCommands.SendMessageToMaya(command);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not find the Maya that is running or was connected to Unity.", "Ok");
                        return;
                    }

                    break;

                case DCCType.MAX:
                    break;

                case DCCType.BLENDER:
                    break;
            }
        }


        static void RunBlenderWithFBX(string FBXPath)
        {
            if (blenderCommands.IsBlenderRunning())
            {
                blenderCommands.KillBlenderProcess();
            }

            string[] guids = AssetDatabase.FindAssets("InitCommandPortOnBlender");

            if (guids.Length == 0)
            {
                Debug.LogError("Could not find the Blender Script : InitCommandPortOnBlender");
                return;
            }

            string cmdPortScriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            cmdPortScriptPath = Path.GetFullPath(cmdPortScriptPath);
            cmdPortScriptPath = cmdPortScriptPath.Replace("\\", "/");


            if (!File.Exists(cmdPortScriptPath))
            {
                Debug.LogError("Could not find the path : " + cmdPortScriptPath);
                return;
            }

            string scriptPath = cmdPortScriptPath.Replace(Path.GetFileName(cmdPortScriptPath), string.Empty);
            scriptPath = scriptPath.Replace("\\", "/");


            string[] blenderScripts = Directory.GetFiles(scriptPath, "*.py");

            string command = string.Empty;

            foreach (string blenderScript in blenderScripts)
            {
                string curMaxPath = blenderScript.Replace("\\", "/");
                command = curMaxPath;
                break;
            }


            System.Diagnostics.Process proc = new System.Diagnostics.Process();

            proc.StartInfo.FileName = blenderPath;
            //proc.StartInfo.Arguments = string.Format("-mxs \"{0}\" \"{1}\"", command, FBXPath);
            proc.StartInfo.Arguments = string.Format("--python \"{0}\"", command);

            proc.Start();
            proc.Close();
        }


        static void RunMaxWithFBX(string FBXPath)
        {
            if (maxCommands.IsMaxRunning())
            {
                maxCommands.KillMayaProcess();
            }

            string[] guids = AssetDatabase.FindAssets("InitCommandPortOnMax");

            if (guids.Length == 0)
            {
                Debug.LogError("Could not find the Max Script : InitCommandPortOnMax");
                return;
            }

            string cmdPortScriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            cmdPortScriptPath = Path.GetFullPath(cmdPortScriptPath);
            cmdPortScriptPath = cmdPortScriptPath.Replace("\\", "/");


            if (!File.Exists(cmdPortScriptPath))
            {
                Debug.LogError("Could not find the path : " + cmdPortScriptPath);
                return;
            }

            string scriptPath = cmdPortScriptPath.Replace(Path.GetFileName(cmdPortScriptPath), string.Empty);
            scriptPath = scriptPath.Replace("\\", "/");


            string[] maxScripts = Directory.GetFiles(scriptPath, "*.ms");

            string command = string.Empty;

            foreach (string maxScript in maxScripts)
            {
                string curMaxPath = maxScript.Replace("\\", "/");
                //command = command + string.Format("fileIn \\\"{0}\\\";", curMaxPath);
                command = curMaxPath;
                break;
            }

            if (!File.Exists(maxPath))
            {
                Debug.LogError("Could not find Max Path. need to check the LDS Project settings.");
                return;
            }

            System.Diagnostics.Process proc = new System.Diagnostics.Process();


            proc.StartInfo.FileName = maxPath;
            //proc.StartInfo.Arguments = string.Format("-mxs \"{0}\" \"{1}\"", command, FBXPath);
            proc.StartInfo.Arguments = string.Format("-U MAXScript \"{0}\" \"{1}\"", command, FBXPath);
            // 3dmax.exe -u MAXScript "?.ms" file.max


            proc.Start();
            proc.Close();
        }


        static void RunMayaWithFBX(string FBXPath)
        {
            
            if (mayaCommands.IsMayaRunning())
            {
                mayaCommands.KillMayaProcess();
            }

            string[] guids = AssetDatabase.FindAssets("InitCommandPortOnMaya");

            if (guids.Length == 0)
            {
                Debug.LogError("Could not find the MEL Script : InitCommandPortOnMaya");
                return;
            }

            string cmdPortScriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            cmdPortScriptPath = Path.GetFullPath(cmdPortScriptPath);
            cmdPortScriptPath = cmdPortScriptPath.Replace("\\", "/");


            if (!File.Exists(cmdPortScriptPath))
            {
                Debug.LogError("Could not find the path : " + cmdPortScriptPath);
                return;
            }

            string scriptPath = cmdPortScriptPath.Replace(Path.GetFileName(cmdPortScriptPath), string.Empty);
            scriptPath = scriptPath.Replace("\\", "/");


            string[] melScripts = Directory.GetFiles(scriptPath, "*.mel");

            string command = string.Empty;

            foreach (string melScript in melScripts)
            {
                string curMelPath = melScript.Replace("\\", "/");
                command = command + string.Format("source \\\"{0}\\\";", curMelPath);
            }

            if (File.Exists(FBXPath))
            {
                string modelImpotCmd = string.Format("ImportTargetFBX(\\\"{0}\\\");", FBXPath);
                command = command + modelImpotCmd;
            }


            if (!File.Exists(mayaPath))
            {
                Debug.LogError("Could not find Maya Path. need to check the LDS Project settings.");
                return;
            }

            System.Diagnostics.Process proc = new System.Diagnostics.Process();


            proc.StartInfo.FileName = mayaPath;
            proc.StartInfo.Arguments = string.Format("-command \"{0}\"", command);

            proc.Start();
            proc.Close();
        }

        /*
        private static List<string> GetDefaultVendorDirectories()
        {

            List<string> existingDirectories = new List<string>();
            List<string> searchDirectories = new List<string>();

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    {
                        DriveInfo[] allDrives = DriveInfo.GetDrives();
                        foreach (DriveInfo drive in allDrives)
                        {
                            searchDirectories.Add(Path.Combine(drive.Name, @"Program Files\Autodesk"));
                            searchDirectories.Add(Path.Combine(drive.Name, @"Program Files\Blender Foundation"));
                        }
                        break;
                    }
                case RuntimePlatform.OSXEditor:
                    {
                        searchDirectories.Add("/Applications/Autodesk");
                        searchDirectories.Add("/Applications");
                        break;
                    }
                case RuntimePlatform.LinuxEditor:
                    {

                        searchDirectories.Add(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
                        searchDirectories.Add("/usr/autodesk");
                        break;
                    }
                default:
                    {
                        throw new System.NotImplementedException();
                    }
            }

            foreach (string path in searchDirectories)
            {
                if (Directory.Exists(path))
                {
                    existingDirectories.Add(path);
                }
            }

            return existingDirectories;
        }
        */


    }
}
#endif

