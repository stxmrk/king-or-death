using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KingOrDeath.Editor
{
    public static class PrototypeBuilder
    {
        [MenuItem("KING OR DEATH/Rebuild prototype scene")]
        public static void BuildScene()
        {
            string root = "Assets/_Game";
            foreach (
                var d in new[]
                {
                    "Art",
                    "Audio",
                    "Materials",
                    "Prefabs",
                    "Scenes",
                    "ScriptableObjects",
                    "Settings",
                    "Tests",
                }
            )
                Directory.CreateDirectory(root + "/" + d);
            Directory.CreateDirectory("Docs/Screenshots");
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(
                root + "/ScriptableObjects/FrontierConfig.asset"
            );
            if (!cfg)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, root + "/ScriptableObjects/FrontierConfig.asset");
            }
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                root + "/Settings/FrontierRenderer.asset"
            );
            if (!renderer)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, root + "/Settings/FrontierRenderer.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                root + "/Settings/FrontierURP.asset"
            );
            if (!pipeline)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, root + "/Settings/FrontierURP.asset");
            }
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 180;
            pipeline.renderScale = 1;
            pipeline.shadowCascadeCount = 4;
            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.shadows = UnityEngine.ShadowQuality.All;
            QualitySettings.shadowDistance = 140;
            QualitySettings.antiAliasing = 4;
            WorldBuilder.Build(cfg);
            // Persist every procedural material / mesh before saving scene and prefabs.
            int meshIndex = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>())
            {
                var mat = r.sharedMaterial;
                if (mat && !AssetDatabase.Contains(mat))
                {
                    string path = root + "/Materials/" + mat.name + ".mat";
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (existing)
                        r.sharedMaterial = existing;
                    else
                        AssetDatabase.CreateAsset(mat, path);
                }
            }
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh && mesh.name == "RoofMesh" && !AssetDatabase.Contains(mesh))
                {
                    string path = root + "/Art/Roof_" + (meshIndex++) + ".asset";
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(mesh, path);
                }
            }
            var saved = new System.Collections.Generic.HashSet<string>();
            foreach (var u in UnityEngine.Object.FindObjectsByType<Unit>())
            {
                string name = u.faction + "_" + u.kind;
                if (saved.Add(name))
                    PrefabUtility.SaveAsPrefabAsset(
                        u.gameObject,
                        root + "/Prefabs/" + name + ".prefab"
                    );
            }
            var house = ModelFactory.House("FrontierHouse", new Vector3(0, -100, 0));
            house.AddComponent<Building>();
            foreach (var r in house.GetComponentsInChildren<Renderer>())
            {
                var path = root + "/Materials/" + r.sharedMaterial.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat)
                    r.sharedMaterial = mat;
                else
                    AssetDatabase.CreateAsset(r.sharedMaterial, path);
            }
            foreach (var mf in house.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh.name == "RoofMesh")
                {
                    string path = root + "/Art/HouseRoof.asset";
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(mf.sharedMesh, path);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(house, root + "/Prefabs/FrontierHouse.prefab");
            UnityEngine.Object.DestroyImmediate(house);
            PlayerSettings.companyName = "KingOrDeath";
            PlayerSettings.productName = "KING OR DEATH";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Standalone,
                ScriptingImplementation.Mono2x
            );
            EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
                root + "/Scenes/Prototype_01.unity"
            );
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(root + "/Scenes/Prototype_01.unity", true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("KING OR DEATH SCENE READY");
        }

        public static void BuildDeliverables()
        {
            BuildScene();
            Screenshot();
            BuildWindows();
        }

        public static void RepairHousePrefab()
        {
            var house = ModelFactory.House("FrontierHouse", Vector3.zero);
            house.AddComponent<Building>();
            foreach (var r in house.GetComponentsInChildren<Renderer>())
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Game/Materials/" + r.sharedMaterial.name + ".mat"
                );
                if (!mat)
                    throw new Exception("Missing palette material");
                r.sharedMaterial = mat;
            }
            foreach (var mf in house.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh.name == "RoofMesh")
                    mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                        "Assets/_Game/Art/HouseRoof.asset"
                    );
            PrefabUtility.SaveAsPrefabAsset(house, "Assets/_Game/Prefabs/FrontierHouse.prefab");
            UnityEngine.Object.DestroyImmediate(house);
            AssetDatabase.SaveAssets();
            Debug.Log("HOUSE PREFAB MATERIALS VERIFIED");
        }

        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/_Game/Scenes/Prototype_01.unity" },
                    locationPathName = "Builds/Windows/KingOrDeath.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                }
            );
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("KING OR DEATH WINDOWS BUILD SUCCEEDED");
        }

        public static void Screenshot()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Prototype_01.unity");
            var camera = UnityEngine.Camera.main;
            var rt = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = rt;
            camera.Render();
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Docs/Screenshots");
            File.WriteAllBytes("Docs/Screenshots/UnityScene.png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("UNITY SCENE SCREENSHOT SAVED");
        }
    }
}
