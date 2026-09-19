using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    public static class HomecomingLurePass
    {
        [MenuItem("Tools/Unity Agent/Add Homecoming Lure Presentation")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != HomecomingSceneBuilder.ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires saved Homecoming in Edit Mode.");
            var roots = scene.GetRootGameObjects();
            if (roots.Any(r => r.GetComponentInChildren<LureCorridorPresentation>(true) != null))
                throw new InvalidOperationException("Already installed; edit the existing Inspector without replacing authored settings.");
            var corridor = roots.Single(r => r.name == "HomeCorridor").transform.Find("InteriorVisuals");
            var owner = new GameObject("演出調整_誘引の廊下");
            var presentation = owner.AddComponent<LureCorridorPresentation>();
            var so = new SerializedObject(presentation); var lamps = so.FindProperty("lamps"); lamps.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                int fixture = 2 - i;
                var entry = lamps.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("light").objectReferenceValue = corridor.Find("FixtureLight" + fixture).GetComponent<Light>();
                entry.FindPropertyRelative("diffuser").objectReferenceValue = corridor.Find("LampDiffuser" + fixture).GetComponent<Renderer>();
            }
            var audioObject = new GameObject("奥の設備音_音源差替え可能"); audioObject.transform.SetParent(owner.transform, false);
            audioObject.transform.position = new Vector3(0, 2.5f, -8);
            var sound = audioObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.loop = true;
            sound.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/Ventilation.wav");
            sound.spatialBlend = 1; sound.minDistance = 4; sound.maxDistance = 18; sound.volume = 0; sound.pitch = .65f;
            so.FindProperty("drone").objectReferenceValue = sound; so.ApplyModifiedPropertiesWithoutUndo();
            var fsm = roots.SelectMany(r => r.GetComponentsInChildren<BeatStateMachine>(true)).Single();
            var machine = new SerializedObject(fsm); machine.FindProperty("lurePresentation").objectReferenceValue = presentation;
            machine.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = owner;
        }
    }
}
