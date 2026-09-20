using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    public static class CampaignSaveAuthoring
    {
        [MenuItem("Tools/Unity Agent/本編の保存・結果の文章を追加")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != HomecomingSceneBuilder.ScenePath || EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
                throw new InvalidOperationException("Requires saved Homecoming Edit Mode.");
            var root = scene.GetRootGameObjects().Single(g => g.GetComponent<GameTextCollection>() != null);
            const string name = "11_保存・続き・結果";
            var child = root.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(GameTextCollection));
                Undo.RegisterCreatedObjectUndo(go, "保存メニュー文章を追加"); go.transform.SetParent(root.transform, false); child = go.transform;
            }
            var collection = child.GetComponent<GameTextCollection>();
            Undo.RecordObject(collection, "保存メニュー文章を追加");
            // Keep runtime catalog internal; this installer never overwrites an author's values.
            var catalog = typeof(DemoSession).Assembly.GetType("CampaignMenuCopy");
            var entries = (string[][])catalog.GetField("Entries", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var entry in entries) collection.Add("save." + entry[0], entry[1].Split('\n')[0], entry[1]);
            EditorUtility.SetDirty(collection); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeObject = collection;
        }
    }
}
