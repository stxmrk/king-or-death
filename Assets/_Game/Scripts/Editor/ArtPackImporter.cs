using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEditor.Animations;
namespace KingOrDeath.Editor {
    public class ArtPackPostprocessor:AssetPostprocessor {
        void OnPreprocessTexture(){if(!assetPath.Contains("/_Game/Art/Textures/KOD_"))return;var t=(TextureImporter)assetImporter;t.maxTextureSize=1024;t.mipmapEnabled=true;t.textureCompression=TextureImporterCompression.CompressedHQ;t.anisoLevel=4;if(assetPath.Contains("_Normal"))t.textureType=TextureImporterType.NormalMap;else if(!assetPath.Contains("_BaseColor"))t.sRGBTexture=false;}
        void OnPreprocessModel(){if(!assetPath.Contains("/_Game/Art/Models/KOD_")&&!assetPath.Contains("/_Game/Art/Models/"))return;var m=(ModelImporter)assetImporter;m.globalScale=1;m.useFileScale=true;m.importBlendShapes=false;m.importCameras=false;m.importLights=false;m.importVisibility=false;m.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;m.materialLocation=ModelImporterMaterialLocation.InPrefab;m.isReadable=true;m.importAnimation=true;m.animationType=ModelImporterAnimationType.Generic;m.animationCompression=ModelImporterAnimationCompression.Off;}
    }
    public static class ArtPackImporter {
        const string Art="Assets/_Game/Art";
        public static void Import(){
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var path in Directory.GetFiles(Art+"/Textures","*_BaseColor.png")){
                string name=Path.GetFileName(path).Replace("_BaseColor.png","");string output=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(output);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,output);}m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_BaseColor.png"));m.SetColor("_BaseColor",Color.white);m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_Normal.png"));m.SetFloat("_BumpScale",.35f);m.EnableKeyword("_NORMALMAP");m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_MetallicSmoothness.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);m.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+name+"_AO.png"));m.enableInstancing=true;EditorUtility.SetDirty(m);
            }
            foreach(var file in Directory.GetFiles(Art+"/Models","*.fbx",SearchOption.AllDirectories)){
                string path=file.Replace('\\','/');var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!model)throw new Exception("FBX failed: "+path);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(model);go.name=model.name;
                // FBX stores original material names in the importer even with material creation disabled.
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.SaveAndReimport();UnityEngine.Object.DestroyImmediate(go);
                model=AssetDatabase.LoadAssetAtPath<GameObject>(path);go=(GameObject)PrefabUtility.InstantiatePrefab(model);go.name=model.name;
                foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
                    var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){string n=mats[i]?mats[i].name:"";n=n.Replace(" (Instance)","");var assigned=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+n+".mat");if(!assigned)throw new Exception("Unmapped material "+n+" on "+go.name);mats[i]=assigned;}r.sharedMaterials=mats;
                }
                foreach(var childLod in go.GetComponentsInChildren<LODGroup>(true))if(childLod.gameObject!=go)UnityEngine.Object.DestroyImmediate(childLod);var levels=new LOD[3];for(int l=0;l<3;l++){var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.Contains("_LOD"+l)).ToArray();if(rs.Length==0)throw new Exception("Missing LOD "+l+" "+go.name);foreach(var r in rs){r.gameObject.SetActive(true);r.enabled=true;}levels[l]=new LOD(l==0?.45f:l==1?.18f:.025f,rs);}
                var lod=go.GetComponent<LODGroup>();if(!lod)lod=go.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();lod.fadeMode=LODFadeMode.None;
                var ao=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+go.name+"_AO.png");
                if(ao)foreach(var r in go.GetComponentsInChildren<Renderer>(true)){if(r.name.StartsWith("Wheel_")||r.name.StartsWith("Weapon_")||r.name.StartsWith("CaptureFlag"))continue;var mats=r.sharedMaterials;for(int k=0;k<mats.Length;k++){string output=Art+"/Materials/"+go.name+"_"+mats[k].name+".mat";var instance=AssetDatabase.LoadAssetAtPath<Material>(output);if(!instance){instance=new Material(mats[k]);AssetDatabase.CreateAsset(instance,output);}instance.CopyPropertiesFromMaterial(mats[k]);instance.SetTexture("_OcclusionMap",ao);instance.SetFloat("_OcclusionStrength",.65f);EditorUtility.SetDirty(instance);mats[k]=instance;}r.sharedMaterials=mats;}
                ConfigureAnimations(go,path);
                // Preserve FBX axis conversion under a neutral, metre-scale prefab root.
                UnityEngine.Object.DestroyImmediate(lod);var wrapper=new GameObject(go.name);go.transform.SetParent(wrapper.transform,true);go=wrapper;lod=go.AddComponent<LODGroup>();lod.SetLODs(levels);lod.RecalculateBounds();
                var bounds=new Bounds();bool first=true;foreach(var r in levels[0].renderers){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}var collider=go.AddComponent<BoxCollider>();collider.center=bounds.center-go.transform.position;collider.size=bounds.size;
                PrefabUtility.SaveAsPrefabAsset(go,Art+"/Prefabs/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();Showcase();InstallGameplay();Debug.Log("KOD ART IMPORT COMPLETE");
        }
        static void ConfigureAnimations(GameObject go,string path){
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();if(clips.Length==0)return;
            string controllerPath=Art+"/Animations/"+go.name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            foreach(var clip in clips){string n=AssetShowcase.StateName(clip.name,go.name);var state=machine.AddState(n);state.motion=clip;if(n=="Idle")machine.defaultState=state;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=n!="Die"&&n!="Hit"&&n!="PickUp";AnimationUtility.SetAnimationClipSettings(clip,settings);}
            var animator=go.GetComponent<Animator>();if(!animator)animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;EditorUtility.SetDirty(controller);
        }
        static GameObject Prefab(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/"+name+".prefab");
        static void InstallGameplay(){
            if(!Prefab("KOD_Wolf"))return;string path=Art+"/ArtCatalog.asset";var catalog=AssetDatabase.LoadAssetAtPath<ArtCatalog>(path);if(!catalog){catalog=ScriptableObject.CreateInstance<ArtCatalog>();AssetDatabase.CreateAsset(catalog,path);}catalog.enemyCloth=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/KOD_Burgundy.mat");catalog.house=Prefab("KOD_House");catalog.worker=Prefab("KOD_Human_Worker");catalog.swordsman=Prefab("KOD_Human_Swordsman");catalog.wolf=Prefab("KOD_Wolf");catalog.outpost=Prefab("KOD_Outpost");catalog.wagon=Prefab("KOD_TradingWagon");EditorUtility.SetDirty(catalog);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Prototype_01.unity");var session=UnityEngine.Object.FindAnyObjectByType<GameSession>();var integration=session.GetComponent<ArtIntegration>();if(!integration)integration=session.gameObject.AddComponent<ArtIntegration>();integration.catalog=catalog;EditorSceneManager.SaveScene(session.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void BuildGameplay(){InstallGameplay();PrototypeBuilder.BuildWindows();}
        public static void BuildAll(){Import();BuildShowcase();PrototypeBuilder.BuildWindows();}
        public static void BuildShowcase(){var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/_Game/Scenes/AssetShowcase_01.unity"},locationPathName="Builds/AssetShowcase/KingOrDeath-AssetShowcase.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Showcase build failed");Debug.Log("ASSET SHOWCASE BUILD SUCCEEDED");}
        public static void Showcase(){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var prefabPaths=Directory.GetFiles(Art+"/Prefabs","*.prefab").OrderBy(x=>x).ToArray();
            var subjects=new Transform[prefabPaths.Length];var prefabs=new GameObject[prefabPaths.Length];
            for(int i=0;i<prefabPaths.Length;i++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i].Replace('\\','/'));var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.position=new Vector3(i%3*11,0,i/3*11);go.name=prefab.name;subjects[i]=go.transform;prefabs[i]=prefab;}
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Showcase Ground";ground.transform.localScale=Vector3.one*10;ground.GetComponent<Renderer>().sharedMaterial=ModelFactory.Material(new Color(.22f,.26f,.20f));
            var light=new GameObject("Showcase Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(40,-35,0);var fill=new GameObject("Showcase Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.transform.rotation=Quaternion.Euler(35,160,0);RenderSettings.sun=light;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.57f,.67f);RenderSettings.ambientEquatorColor=new Color(.35f,.39f,.33f);RenderSettings.ambientGroundColor=new Color(.22f,.23f,.17f);RenderSettings.fog=false;
            var camera=new GameObject("Showcase Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(10,8,12);camera.transform.LookAt(new Vector3(0,2.4f,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.21f,.20f);camera.fieldOfView=40;camera.nearClipPlane=.1f;camera.farClipPlane=300;
            var groundMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/ShowcaseGround.mat");if(!groundMat){groundMat=new Material(ground.GetComponent<Renderer>().sharedMaterial);AssetDatabase.CreateAsset(groundMat,Art+"/Materials/ShowcaseGround.mat");}ground.GetComponent<Renderer>().sharedMaterial=groundMat;
            var controls=camera.gameObject.AddComponent<AssetShowcase>();controls.subjects=subjects;controls.prefabs=prefabs;controls.view=camera;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_Game/Scenes/AssetShowcase_01.unity");
            foreach(var subject in subjects){var group=subject.GetComponent<LODGroup>();group.ForceLOD(0);var bounds=new Bounds(subject.position,Vector3.one*.1f);foreach(var r in group.GetLODs()[0].renderers)bounds.Encapsulate(r.bounds);float distance=Mathf.Max(2.2f,bounds.size.magnitude*1.2f);camera.transform.position=bounds.center+new Vector3(.7f,.45f,1).normalized*distance;camera.transform.LookAt(bounds.center);Screenshot(camera,subject.name+"_Unity");group.ForceLOD(-1);}
        }
        public static void Screenshot(Camera camera,string name){
            var rt=new RenderTexture(1400,1200,24);camera.targetTexture=rt;for(int i=0;i<4;i++)camera.Render();RenderTexture.active=rt;var image=new Texture2D(1400,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1200),0,0);image.Apply();Directory.CreateDirectory("Docs/Screenshots/AssetPack001");File.WriteAllBytes("Docs/Screenshots/AssetPack001/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
