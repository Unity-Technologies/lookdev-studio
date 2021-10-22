using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


namespace LookDev.Editor
{
    public class CreateLightPresetWindow : EditorWindow
    {
        public static string lightPresetName;
        public static string latestGeneratedPreviewPath;

        static Texture2D lightPresetPreview;
        static CreateLightPresetWindow createLightPresetWindow;

        public static string ShowWindow(string presetPath)
        {
            lightPresetName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(presetPath));

            createLightPresetWindow = ScriptableObject.CreateInstance<CreateLightPresetWindow>();
            createLightPresetWindow.OnGenerateLightPresetPreview();

            createLightPresetWindow.titleContent = new GUIContent("Create Lighting Preset");


            createLightPresetWindow.position = new Rect(new Vector2(SceneView.lastActiveSceneView.position.x, SceneView.lastActiveSceneView.position.y), new Vector2(300, 410));

            createLightPresetWindow.minSize = createLightPresetWindow.position.size;
            createLightPresetWindow.maxSize = createLightPresetWindow.minSize;

            createLightPresetWindow.ShowUtility();

            return lightPresetName;

        }


        IEnumerator MakeLightPresetPreview(string imagePath)
        {
            while(!File.Exists(imagePath))
                yield return new WaitForSeconds(0.5f);

            byte[] imageByte = File.ReadAllBytes(imagePath);

            lightPresetPreview = new Texture2D(290, 290);
            lightPresetPreview.LoadImage(imageByte);
            lightPresetPreview.Apply();

            LookDevHelpers.GetSceneView();
        }


        void OnGenerateLightPresetPreview()
        {
            LookDevHelpers.GetGameview();

            string captureImgPath = Path.Combine(Path.GetTempPath(), "temp.png");
            CreateLightPresetWindow.latestGeneratedPreviewPath = captureImgPath;

            if (File.Exists(captureImgPath))
                File.Delete(captureImgPath);

            ScreenCapture.CaptureScreenshot(captureImgPath);

            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(MakeLightPresetPreview(captureImgPath), createLightPresetWindow);
        }


        private void OnGUI()
        {

            if (lightPresetPreview != null)
                GUI.DrawTexture(new Rect(0, 0, 300, 300), (Texture)lightPresetPreview, ScaleMode.ScaleAndCrop);

            if (GUI.Button(new Rect(150, 270, 130, 20),"Generate Preview"))
            {
                OnGenerateLightPresetPreview();
            }


            GUI.Label(new Rect(10,310,90,30), "Preset Name :");
            lightPresetName = GUI.TextField(new Rect(100,310,180,25), lightPresetName);


            if (GUI.Button(new Rect(10,360,280,30), "Save Lighting Preset"))
            {
                if (string.IsNullOrEmpty(lightPresetName) == false)
                {
                    string originalScenePath = LightingPresetSceneChanger.GetCurrentLightScenePath();
                    string originalSceneFile = Path.GetFileName(originalScenePath);

                    lightPresetName = lightPresetName.Replace(" ", "_");
                    string newScenePath = originalScenePath.Replace(originalSceneFile, $"{lightPresetName}.unity");
                    string newScenePreviewPath = originalScenePath.Replace(originalSceneFile, $"{lightPresetName}.png");

                    LightingPresetSceneChanger.SaveSceneAsLightPreset(newScenePath);

                    if (File.Exists(CreateLightPresetWindow.latestGeneratedPreviewPath))
                    {
                        #region Resize Preiveiw Image
                        float targetWidth = 128f;
                        float targetHeight = 128f;

                        Texture2D result = new Texture2D((int)targetWidth, (int)targetHeight);
                        Color[] rpixels = result.GetPixels(0);
                        float incX = (1.0f / targetWidth);
                        float incY = (1.0f / targetHeight);
                        for (int px = 0; px < rpixels.Length; px++)
                        {
                            rpixels[px] = lightPresetPreview.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
                        }
                        result.SetPixels(rpixels, 0);
                        result.Apply();

                        byte[] outByte = result.EncodeToPNG();

                        File.WriteAllBytes(Path.GetFullPath(newScenePreviewPath), outByte);

                        DestroyImmediate(result);
                        #endregion

                        //File.Copy(CreateLightPresetWindow.latestGeneratedPreviewPath, Path.GetFullPath(newScenePreviewPath));
                        AssetDatabase.ImportAsset(newScenePreviewPath);

                        TextureImporter texImporter = (TextureImporter)AssetImporter.GetAtPath(newScenePreviewPath);

                        texImporter.textureType = TextureImporterType.GUI;
                        texImporter.alphaSource = TextureImporterAlphaSource.None;
                        texImporter.maxTextureSize = 256;

                        texImporter.SaveAndReimport();
                    }


                    LookDevSearchHelpers.SwitchCurrentProvider(4);

                    createLightPresetWindow.Close();
                }
            }

        }
    }
}