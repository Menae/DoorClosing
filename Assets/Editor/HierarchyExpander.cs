// Assets/Editor/HierarchyExpander.cs
using UnityEditor;
using UnityEngine;

public static class HierarchyExpander
{
    // 右クリックメニューに追加
    [MenuItem("GameObject/Expand Children (All)", false, 0)]
    private static void ExpandAll()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;
        SetExpandedRecursive(selected, true);
    }

    [MenuItem("GameObject/Collapse Children (All)", false, 0)]
    private static void CollapseAll()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;
        SetExpandedRecursive(selected, false);
    }

    private static void SetExpandedRecursive(GameObject go, bool expand)
    {
        // UnityEditorの内部APIを使って展開状態を制御する
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        var window = EditorWindow.GetWindow(type);
        var method = type.GetMethod(
            "SetExpandedRecursive",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
        );
        method?.Invoke(window, new object[] { go.GetInstanceID(), expand });
    }
}