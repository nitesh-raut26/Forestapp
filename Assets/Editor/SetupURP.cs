#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForestFriendsQuest.Editor
{
    public static class SetupURP
    {
        [MenuItem("Forest Friends Quest/Setup URP and Linear Color Space")]
        public static void Setup()
        {
            // 1. Create Settings directory if it doesn't exist
            if (!Directory.Exists("Assets/Settings"))
            {
                Directory.CreateDirectory("Assets/Settings");
                AssetDatabase.Refresh();
            }

            // 2. Set Color Space to Linear
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Debug.Log("[SetupURP] Color space set to Linear.");
            }

            // 3. Create URP 2D Renderer Data
            string rendererPath = "Assets/Settings/UniversalRP_Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                // Load default post process data from URP package
                string ppDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(ppDataPath);
                AssetDatabase.CreateAsset(rendererData, rendererPath);
                Debug.Log("[SetupURP] Created 2D Renderer Data at: " + rendererPath);
            }

            // 4. Create URP Pipeline Asset for Desktop (High Quality, 4x MSAA)
            string desktopAssetPath = "Assets/Settings/UniversalRP_Desktop.asset";
            UniversalRenderPipelineAsset desktopAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(desktopAssetPath);
            if (desktopAsset == null)
            {
                desktopAsset = UniversalRenderPipelineAsset.Create(rendererData);
                desktopAsset.msaaSampleCount = 4;
                desktopAsset.supportsHDR = true;
                desktopAsset.colorGradingMode = ColorGradingMode.HighDynamicRange;
                AssetDatabase.CreateAsset(desktopAsset, desktopAssetPath);
                Debug.Log("[SetupURP] Created Desktop URP Asset at: " + desktopAssetPath);
            }

            // 5. Create URP Pipeline Asset for Mobile (Low/Medium Quality, 2x MSAA)
            string mobileAssetPath = "Assets/Settings/UniversalRP_Mobile.asset";
            UniversalRenderPipelineAsset mobileAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(mobileAssetPath);
            if (mobileAsset == null)
            {
                mobileAsset = UniversalRenderPipelineAsset.Create(rendererData);
                mobileAsset.msaaSampleCount = 2;
                mobileAsset.supportsHDR = true;
                mobileAsset.colorGradingMode = ColorGradingMode.HighDynamicRange;
                AssetDatabase.CreateAsset(mobileAsset, mobileAssetPath);
                Debug.Log("[SetupURP] Created Mobile URP Asset at: " + mobileAssetPath);
            }

            // 6. Assign custom render pipeline to quality levels
            var qualitySettingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualitySettingsAssets != null && qualitySettingsAssets.Length > 0)
            {
                SerializedObject qualitySettings = new SerializedObject(qualitySettingsAssets[0]);
                SerializedProperty qualityLevels = qualitySettings.FindProperty("m_QualitySettings");
                if (qualityLevels != null)
                {
                    for (int i = 0; i < qualityLevels.arraySize; i++)
                    {
                        SerializedProperty level = qualityLevels.GetArrayElementAtIndex(i);
                        SerializedProperty pipelineProp = level.FindPropertyRelative("customRenderPipeline");
                        if (pipelineProp != null)
                        {
                            pipelineProp.objectReferenceValue = (i >= 3) ? desktopAsset : mobileAsset;
                        }
                    }
                    qualitySettings.ApplyModifiedProperties();
                    Debug.Log("[SetupURP] URP Assets assigned to QualitySettings successfully.");
                }
            }

            // 7. Assign default render pipeline to GraphicsSettings
            GraphicsSettings.defaultRenderPipeline = desktopAsset;
            Debug.Log("[SetupURP] Default render pipeline assigned to GraphicsSettings.");

            // 8. Save assets and save project settings
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SetupURP] Configuration completed successfully!");
        }
    }
}
#endif
