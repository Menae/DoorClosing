using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GraduationProject.EditorTools
{
    public static class SpatialHijackBuilder
    {
        public const string DefinitionPath = "Assets/Data/hijack_space.asset";
        public const string PrefabPath = "Assets/Prefab/Anomalies/Hijack_SpatialCabin.prefab";

        [MenuItem("Tools/Unity Agent/空間異常を編集")]
        private static void SelectAsset() => Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(PrefabPath);

        [MenuItem("Tools/Unity Agent/空間異常だけを試す（遭遇リスト変更）")]
        private static void TryOnlySpatial()
        {
            var run = UnityEngine.Object.FindFirstObjectByType<RunManager>();
            var definition = AssetDatabase.LoadAssetAtPath<BeatDefinition>(DefinitionPath);
            if (run == null || definition == null) throw new InvalidOperationException("Homecomingと空間異常assetが必要です。");
            var so = new SerializedObject(run);
            var list = so.FindProperty("beatDefinitions"); list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = definition;
            so.ApplyModifiedProperties(); Selection.activeGameObject = run.gameObject;
        }

        [MenuItem("Tools/Unity Agent/Create Spatial Hijack Assets")]
        public static void Create()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != HomecomingSceneBuilder.ScenePath)
                throw new InvalidOperationException("Requires saved Homecoming Edit Mode");
            if (AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null || AssetDatabase.LoadMainAssetAtPath(DefinitionPath) != null)
                throw new InvalidOperationException("既存の作者設定を保護します。Prefabを直接編集してください。");
            var building = GameObject.Find("Building").transform;
            var paths = new List<string> { "CabCeiling" };
            for (int i = 0; i < 3; i++)
                foreach (string part in new[] { "CeilingTray", "CeilingDiffuser", "PerforatedCeiling" })
                    paths.Add("InteriorVisuals/" + part + i);
            foreach (string path in paths)
                if (building.Find(path)?.GetComponent<MeshFilter>() == null) throw new InvalidOperationException("Missing ceiling: " + path);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Hijack_SpatialCabin");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
                root.AddComponent<HijackAnomaly>();
                var presentation = root.AddComponent<SpatialCabinPresentation>();
                var visuals = Child(root.transform, "空間異常_表示");
                var roof = Child(visuals, "遠ざかる天井");
                foreach (string path in paths)
                {
                    var source = building.Find(path);
                    var copy = Child(roof, source.name);
                    copy.localPosition = building.InverseTransformPoint(source.position);
                    copy.localRotation = Quaternion.Inverse(building.rotation) * source.rotation;
                    copy.localScale = source.lossyScale;
                    copy.gameObject.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                    copy.gameObject.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                }
                // Preserve the ordinary cabin light for readable controls. This small
                // moving light only illuminates the extended upper walls.
                var light = Child(roof, "上部壁の照明").gameObject.AddComponent<Light>();
                light.transform.localPosition = new Vector3(0, 2.8f, 3.5f);
                light.type = LightType.Point; light.range = 5; light.intensity = 1.2f;
                light.color = new Color(.94f, .96f, 1); light.shadows = LightShadows.None;
                var walls = Child(visuals, "伸びる上部壁");
                walls.localPosition = new Vector3(0, 2.99f, 0);
                var laminate = building.Find("CabBack").GetComponent<Renderer>().sharedMaterial;
                var trim = building.Find("InteriorVisuals/RearJoint0").GetComponent<Renderer>().sharedMaterial;
                Box(walls, "奥の壁", new Vector3(0,.5f,5.1f), new Vector3(3.4f,1,.2f), laminate);
                Box(walls, "扉上の壁", new Vector3(0,.5f,1.9f), new Vector3(3.4f,1,.2f), laminate);
                for (int side=-1; side<=1; side+=2)
                {
                    Box(walls, "側壁"+side, new Vector3(side*1.7f,.5f,3.5f), new Vector3(.2f,1,3.4f), laminate);
                    for(int i=0;i<4;i++) Box(walls,"側壁目地"+side+"_"+i,new Vector3(side*1.592f,.5f,2.35f+i*.8f),new Vector3(.012f,1,.015f),trim);
                }
                for(int i=0;i<5;i++) Box(walls,"奥目地"+i,new Vector3(-1.56f+i*.78f,.5f,4.99f),new Vector3(.014f,1,.013f),trim);
                var so = new SerializedObject(presentation);
                so.FindProperty("movingCeiling").objectReferenceValue = roof;
                so.FindProperty("upperWalls").objectReferenceValue = walls;
                so.FindProperty("visualRoot").objectReferenceValue = visuals.gameObject;
                var sourcePaths = so.FindProperty("sourceCeilingPaths"); sourcePaths.arraySize = paths.Count;
                for(int i=0;i<paths.Count;i++) sourcePaths.GetArrayElementAtIndex(i).stringValue = paths[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                visuals.gameObject.SetActive(false);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                var definition = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<BeatDefinition>("Assets/Data/hijack.asset"));
                definition.name = "hijack_space";
                var ds = new SerializedObject(definition);
                ds.FindProperty("debugLabel").stringValue = "Hijack - Spatial Cabin";
                ds.FindProperty("anomalyPrefab").objectReferenceValue = prefab;
                ds.FindProperty("presentationLocalPosition").vector3Value = Vector3.zero;
                ds.FindProperty("presentationLocalScale").vector3Value = Vector3.one;
                ds.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
                AssetDatabase.SaveAssets(); Selection.activeObject = prefab;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
