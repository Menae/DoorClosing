using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    public static class PlayableDemoBuilder
    {
        public const string ScenePath = "Assets/Scenes/PlayableDemo.unity";

        [MenuItem("Tools/Unity Agent/Create Playable Demo")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Requires idle Editor.");
            for (int i=0; i<SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scenes first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Demo exists. Edit it; do not overwrite authored notices.");
            var scene = EditorSceneManager.OpenScene(M2VerticalSliceBuilder.ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not create demo scene.");
            var roots = scene.GetRootGameObjects();
            var journey = roots.Single(x=>x.name=="NormalJourney").GetComponent<NormalJourneyController>();
            var player = roots.Single(x=>x.name=="Player").GetComponent<PlayerLook>();
            var owner = new GameObject("Demo_導入から怪異夜へ").AddComponent<DemoSession>();
            var so = new SerializedObject(owner);
            so.FindProperty("journey").objectReferenceValue = journey;
            so.FindProperty("run").objectReferenceValue = journey.GetComponent<RunManager>();
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("view").objectReferenceValue = player.GetComponentInChildren<Camera>();
            so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            ConfigureSound(player.gameObject);
            // Keep historical scenes in the list, but make the intended demo the explicit entry point.
            EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene(ScenePath,true)}
                .Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Select(s=>new EditorBuildSettingsScene(s.path,false))).ToArray();
            EditorSceneManager.SaveScene(scene);
        }

        public static void ConfigureSound(GameObject player)
        {
            var sound=player.GetComponent<ApartmentFootsteps>();
            if(sound==null)sound=player.AddComponent<ApartmentFootsteps>();
            var so=new SerializedObject(sound);
            var steps=so.FindProperty("steps");steps.arraySize=4;
            for(int i=0;i<4;i++)steps.GetArrayElementAtIndex(i).objectReferenceValue=
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/Footstep"+(i+1)+".wav");
            so.FindProperty("ventilation").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/Ventilation.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
