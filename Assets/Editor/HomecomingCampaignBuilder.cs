using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GraduationProject.EditorTools
{
    public static class HomecomingCampaignBuilder
    {
        [MenuItem("Tools/Unity Agent/本編の全4夜を設定")]
        private static void SelectCampaign()
        {
            var campaign = UnityEngine.Object.FindFirstObjectByType<HomecomingCampaign>();
            if (campaign != null) Selection.activeObject = campaign;
        }

        [MenuItem("Tools/Unity Agent/Add Four Night Campaign")]
        public static void Add()
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (s.path != HomecomingSceneBuilder.ScenePath || s.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires saved Homecoming Edit Mode.");
            var run = UnityEngine.Object.FindFirstObjectByType<RunManager>();
            if (run.GetComponent<HomecomingCampaign>() != null) throw new InvalidOperationException("Author settings already exist.");
            var campaign = Undo.AddComponent<HomecomingCampaign>(run.gameObject);
            var so = new SerializedObject(campaign);
            Assign(so.FindProperty("firstNight"), new[] { "lure", "provocation", "hijack" });
            Assign(so.FindProperty("secondNight"), new[] { "lure_wet", "provocation_voice", "hijack_space" });
            so.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s); Selection.activeObject = campaign;
        }
        private static void Assign(SerializedProperty list, string[] names)
        {
            list.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<BeatDefinition>("Assets/Data/" + names[i] + ".asset");
                if (definition == null) throw new InvalidOperationException("Missing " + names[i]);
                list.GetArrayElementAtIndex(i).objectReferenceValue = definition;
            }
        }
    }
}
