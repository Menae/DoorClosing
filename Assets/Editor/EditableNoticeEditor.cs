using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EditableNotice))]
public sealed class EditableNoticeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("見出し・本文が掲示に反映されます。編集後はSceneを保存してください。内装の再適用でも文章を保持します。子のTMPではなく、この欄を編集します。", MessageType.Info);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("heading"), new GUIContent("見出し"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("body"), new GUIContent("本文"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("headingSize"), new GUIContent("見出しの文字サイズ"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bodySize"), new GUIContent("本文の文字サイズ"));
        serializedObject.ApplyModifiedProperties();
        var notice = (EditableNotice)target;
        notice.Apply();
        var views = notice.GetComponentsInChildren<TMPro.TMP_Text>();
        foreach (var view in views)
        {
            view.ForceMeshUpdate();
            if (view.isTextOverflowing)
                EditorGUILayout.HelpBox("文章が紙面を超えています。短くするか改行・文字サイズを調整してください。", MessageType.Warning);
        }
    }
}
