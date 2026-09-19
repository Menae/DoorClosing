using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraduationProject.EditorTools
{
    public static class WetLureBuilder
    {
        public const string DefinitionPath = "Assets/Data/lure_wet.asset";
        private const string PrefabPath = "Assets/Prefab/Anomalies/Lure_WetCorridor.prefab";
        private const string ReflectionPath = "Assets/ApartmentVisuals/WetCorridorReflection.exr";

        [MenuItem("Tools/Unity Agent/濡れた廊下の反射を更新")]
        public static void BakeReflection()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/Scenes/Homecoming.unity")
                throw new InvalidOperationException("保存済みのHomecomingをEdit Modeで開いてください。");

            var corridor = scene.GetRootGameObjects().Single(g => g.name == "HomeCorridor");
            var hall = scene.GetRootGameObjects().Single(g => g.name == "EntranceHall");
            bool corridorActive = corridor.activeSelf, hallActive = hall.activeSelf;
            var originalLightData = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true)).ToArray();
            GameObject capture = null;
            try
            {
                corridor.SetActive(true);
                hall.SetActive(false);
                capture = new GameObject("Wet reflection capture (temporary)");
                capture.transform.position = new Vector3(0, 1.45f, -5);
                var probe = capture.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Custom;
                probe.renderDynamicObjects = true;
                probe.resolution = 128;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = Color.black;
                if (!Lightmapping.BakeReflectionProbe(probe, ReflectionPath))
                    throw new InvalidOperationException("廊下の反射ベイクに失敗しました。");
            }
            finally
            {
                if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
                corridor.SetActive(corridorActive);
                hall.SetActive(hallActive);
                // URP can attach default light metadata while capturing previously inactive lights.
                foreach (var data in scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true)).ToArray())
                    if (!originalLightData.Contains(data)) UnityEngine.Object.DestroyImmediate(data);
                // Only temporary activation flags changed; preserve all authored values.
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.ImportAsset(ReflectionPath, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath);
            if (texture == null) throw new InvalidOperationException("反射Cubemapのimportに失敗しました。");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var probe = root.GetComponentInChildren<ReflectionProbe>();
                probe.name = "廊下の反射_配置変更時はメニューで更新";
                probe.mode = ReflectionProbeMode.Custom;
                probe.customBakedTexture = texture;
                // The floor is close to the bottom of the volume; a 1m blend leaks the sky.
                probe.blendDistance = .05f;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        [MenuItem("Tools/Unity Agent/Create Wet Lure Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit Mode.");
            foreach (var path in new[]{DefinitionPath,PrefabPath,"Assets/ApartmentVisuals/WetFloor.mat","Assets/ApartmentVisuals/WetFloorMesh.asset"})
                if (AssetDatabase.LoadMainAssetAtPath(path)!=null) throw new InvalidOperationException("Preserve authored asset: "+path);
            var shader=Shader.Find("GraduationProject/Wet Floor");
            var dry=AssetDatabase.LoadAssetAtPath<Material>("Assets/ApartmentVisuals/StoneFloor.mat");
            var source=AssetDatabase.LoadAssetAtPath<BeatDefinition>("Assets/Data/lure.asset");
            if(shader==null || dry==null || source==null) throw new InvalidOperationException("Missing source assets.");
            var material=new Material(shader) { name="WetFloor" };
            material.SetTexture("_BaseMap",dry.GetTexture("_BaseMap")); material.SetColor("_BaseColor",dry.GetColor("_BaseColor"));
            material.SetFloat("_WorldScale",dry.GetFloat("_WorldScale"));
            AssetDatabase.CreateAsset(material,"Assets/ApartmentVisuals/WetFloor.mat");
            var mesh=new Mesh { name="WetFloorMesh", vertices=new[]{new Vector3(-1.55f,0,1.15f),new Vector3(1.55f,0,1.15f),new Vector3(-1.55f,0,-11.5f),new Vector3(1.55f,0,-11.5f)}, triangles=new[]{0,1,2,2,1,3} };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,"Assets/ApartmentVisuals/WetFloorMesh.asset");
            // Build in a preview scene; neither active scene nor author edits become part of this prefab.
            var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Lure_WetCorridor"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
                var anomaly=root.AddComponent<LureAnomaly>();
                var surface=new GameObject("濡れた床_材質で調整"); surface.transform.SetParent(root.transform,false); surface.transform.localPosition=Vector3.up*.008f;
                surface.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=surface.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.shadowCastingMode=ShadowCastingMode.Off;
                var probeObject=new GameObject("廊下の反射_遭遇開始時のみ"); probeObject.transform.SetParent(root.transform,false);
                probeObject.transform.localPosition=new Vector3(0,1.45f,-5);
                var probe=probeObject.AddComponent<ReflectionProbe>(); probe.mode=ReflectionProbeMode.Custom;
                probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath); probe.blendDistance=.05f;
                probe.refreshMode=ReflectionProbeRefreshMode.OnAwake; probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.resolution=128; probe.size=new Vector3(3.5f,3.2f,14); probe.boxProjection=true; probe.intensity=1;
                probe.clearFlags=ReflectionProbeClearFlags.SolidColor; probe.backgroundColor=Color.black;
                var so=new SerializedObject(anomaly); so.FindProperty("hallwayRoot").objectReferenceValue=surface;
                so.FindProperty("preserveDiagnosisSurface").boolValue=true;
                var renderers=so.FindProperty("targetRenderers"); renderers.arraySize=1; renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
                so.ApplyModifiedPropertiesWithoutUndo();
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                var definition=UnityEngine.Object.Instantiate(source); definition.name="lure_wet";
                var ds=new SerializedObject(definition); ds.FindProperty("debugLabel").stringValue="Lure - Wet Corridor";
                ds.FindProperty("anomalyPrefab").objectReferenceValue=prefab; ds.FindProperty("presentationLocalPosition").vector3Value=Vector3.zero;
                ds.FindProperty("presentationLocalScale").vector3Value=Vector3.one; ds.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition,DefinitionPath); AssetDatabase.SaveAssets(); Selection.activeObject=definition;
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
