using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    public static class HomecomingCreditsAuthoring
    {
        [MenuItem("Tools/Unity Agent/本編のスタッフロールを追加")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != HomecomingSceneBuilder.ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires saved Homecoming in Edit Mode.");
            var root = scene.GetRootGameObjects().Single(r => r.name == "テキスト編集").transform;
            var existing = root.GetComponentInChildren<HomecomingCredits>(true);
            if (existing != null) { Selection.activeObject = existing; return; }
            var go = new GameObject("13_スタッフロール_文章と表示時間", typeof(HomecomingCredits)); go.transform.SetParent(root, false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Selection.activeGameObject = go;
        }
    }

    [CustomEditor(typeof(HomecomingCredits))]
    public sealed class HomecomingCreditsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Play停止中に編集してシーンを保存。各ページの見出し・本文・順序を変更できます。素材の必須クレジットは同梱licensesにも保持しています。", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pages"), new GUIContent("表示ページ（見出し・本文）"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("secondsPerPage"), new GUIContent("1ページの表示秒数"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeSeconds"), new GUIContent("フェード秒数"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skipLabel"), new GUIContent("スキップボタンの文章"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resultButtonLabel"), new GUIContent("結果画面のボタン文章"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
