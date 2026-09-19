using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EntranceAccessController))]
public sealed class EntranceAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("805の郵便受けを調べる → 番号を入力 → 自動扉が開く。表示文は「テキスト編集 → 09_入口・ポスト・オートロック」で編集できます。", MessageType.Info);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("roomNumber"), new GUIContent("解錠する部屋番号"));
        EditorGUILayout.HelpBox("番号判定とポストの表記は別です。変更時は表記も揃えてください。", MessageType.None);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("doorSeconds"), new GUIContent("自動扉の開く時間（秒）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("doorTravel"), new GUIContent("片側の移動量（m）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keyVolume"), new GUIContent("キー操作音量"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("doorVolume"), new GUIContent("自動扉の動作音量"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keySound"), new GUIContent("キー操作音（空欄で内蔵音）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("doorSound"), new GUIContent("自動扉の動作音"));
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("操作盤の配置・大きさを選択"))
        {
            var display = serializedObject.FindProperty("display").objectReferenceValue as Component;
            if (display != null) Selection.activeGameObject = display.transform.parent.gameObject;
        }
    }
}

[CustomEditor(typeof(HomecomingPresentation))]
public sealed class HomecomingPresentationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("説明本文を出さず入口から開始し、通常帰宅後は短い暗転で翌夜へ進みます。文章はテキスト編集の各カテゴリで編集してください。", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInSeconds"), new GUIContent("暗転から戻る時間（秒）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nightBlackSeconds"), new GUIContent("翌夜までの暗転（秒）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("followingNightStart"), new GUIContent("翌夜・死亡再開の位置"));
        serializedObject.ApplyModifiedProperties();
    }
}
