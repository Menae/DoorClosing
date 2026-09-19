using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GraduationProject.EditorTools
{
    public static class VoiceProvocationBuilder
    {
        public const string DefinitionPath = "Assets/Data/provocation_voice.asset";
        public const string PrefabPath = "Assets/Prefab/Anomalies/Provocation_OutsideVoice.prefab";

        [MenuItem("Tools/Unity Agent/声の怪異を編集")]
        private static void SelectVoice() => Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(PrefabPath);

        [MenuItem("Tools/Unity Agent/Create Outside Voice Assets")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit Mode");
            if (AssetDatabase.LoadMainAssetAtPath(DefinitionPath) != null || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
                throw new InvalidOperationException("既存の作者設定を保護します。Prefabを直接調整してください。");
            var plea = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/OutsidePlea.wav");
            var insistence = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/OutsideInsistence.wav");
            var source = AssetDatabase.LoadAssetAtPath<BeatDefinition>("Assets/Data/provocation.asset");
            if (plea == null || insistence == null || source == null) throw new InvalidOperationException("Missing source assets");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Provocation_OutsideVoice");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
                var voice = root.AddComponent<VoiceProvocationAnomaly>();
                var sound = new GameObject("扉の外_音源とこもり");
                sound.transform.SetParent(root.transform, false);
                sound.transform.localPosition = new Vector3(0, 1.55f, 1.15f);
                var speaker = sound.AddComponent<AudioSource>();
                speaker.playOnAwake = false;
                speaker.loop = false;
                speaker.spatialBlend = .85f;
                speaker.rolloffMode = AudioRolloffMode.Linear;
                speaker.minDistance = 2;
                speaker.maxDistance = 9;
                speaker.dopplerLevel = 0;
                speaker.volume = .6f;
                sound.AddComponent<AudioLowPassFilter>().cutoffFrequency = 1800;
                var so = new SerializedObject(voice);
                so.FindProperty("speaker").objectReferenceValue = speaker;
                so.FindProperty("pleaClip").objectReferenceValue = plea;
                so.FindProperty("revealedClip").objectReferenceValue = insistence;
                so.FindProperty("pleaScript").stringValue = "助けて。非常停止を押してください。";
                so.FindProperty("revealedScript").stringValue = "押して。早く、非常停止を押して。";
                so.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                var definition = UnityEngine.Object.Instantiate(source);
                definition.name = "provocation_voice";
                var ds = new SerializedObject(definition);
                ds.FindProperty("debugLabel").stringValue = "Provocation - Outside Voice";
                ds.FindProperty("anomalyPrefab").objectReferenceValue = prefab;
                ds.FindProperty("presentationLocalPosition").vector3Value = Vector3.zero;
                ds.FindProperty("presentationLocalScale").vector3Value = Vector3.one;
                // Complete one plea, then allow a quiet interval before passive success.
                ds.FindProperty("passiveSuccessSeconds").floatValue = Mathf.Max(7, .6f + plea.length + 2);
                ds.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = prefab;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
