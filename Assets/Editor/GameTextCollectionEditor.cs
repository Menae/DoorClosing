using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameTextCollection))]
public sealed class GameTextCollectionEditor : Editor
{
    private string search = "";
    public override void OnInspectorGUI()
    {
        var collection = (GameTextCollection)target;
        EditorGUILayout.HelpBox("Playを停止して文章を編集し、Ctrl+Sでシーンを保存してください。起動・結果画面は次回表示時に反映されます。文章を変更してもボタンの機能は変わりません。", MessageType.Info);
        if (Application.isPlaying) EditorGUILayout.HelpBox("Play中の変更は停止すると失われます。", MessageType.Warning);
        foreach (Transform child in collection.transform)
            if (GUILayout.Button(child.name)) Selection.activeGameObject = child.gameObject;
        search = EditorGUILayout.TextField("文章を検索", search);
        serializedObject.Update();
        var entries = serializedObject.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var label = entry.FindPropertyRelative("label").stringValue;
            var value = entry.FindPropertyRelative("value");
            if (!string.IsNullOrEmpty(search) && !label.Contains(search) && !value.stringValue.Contains(search)) continue;
            EditorGUILayout.Space(8);
            var key = entry.FindPropertyRelative("key").stringValue;
            EditorGUILayout.LabelField(key == "cabin.standby" ? "かご内モニター：最初の文言" : label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(value, GUIContent.none);
            if (key == "cabin.standby")
            {
                foreach (var root in collection.gameObject.scene.GetRootGameObjects())
                {
                    var display = root.GetComponentInChildren<CabinInformationDisplay>(true);
                    if (display == null) continue;
                    CabinInformationDisplayEditor.DrawSettings(new SerializedObject(display));
                    if (GUILayout.Button("モニター本体を選択")) Selection.activeGameObject = display.gameObject;
                    break;
                }
            }
            if (key == "floor.format") EditorGUILayout.HelpBox("{0} が現在階に置き換わります。例：{0} 階。階数の判定自体は変わりません。", MessageType.Info);
            var view = entry.FindPropertyRelative("target").objectReferenceValue as TMPro.TMP_Text;
            if (view != null)
            {
                if (GUILayout.Button("表示オブジェクトを確認", EditorStyles.miniButton)) EditorGUIUtility.PingObject(view.gameObject);
                view.ForceMeshUpdate(true);
                if (view.isTextOverflowing) EditorGUILayout.HelpBox("表示枠からはみ出しています。文章・改行を調整してください。", MessageType.Warning);
                if (!view.gameObject.activeInHierarchy) EditorGUILayout.LabelField("現在は非表示（進行中に表示するもの・旧試作用を含む）", EditorStyles.miniLabel);
            }
        }
        if (serializedObject.ApplyModifiedProperties()) collection.Apply();
        foreach (var notice in collection.notices)
        {
            if (notice == null) continue;
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(notice.name, EditorStyles.boldLabel);
            var so = new SerializedObject(notice); so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("heading"), new GUIContent("見出し"));
            EditorGUILayout.PropertyField(so.FindProperty("body"), new GUIContent("本文"));
            EditorGUILayout.PropertyField(so.FindProperty("headingSize"), new GUIContent("見出しの文字サイズ"));
            EditorGUILayout.PropertyField(so.FindProperty("bodySize"), new GUIContent("本文の文字サイズ"));
            if (so.ApplyModifiedProperties()) notice.Apply();
            if (GUILayout.Button("掲示物を選択")) Selection.activeGameObject = notice.gameObject;
        }
    }
}

[CustomEditor(typeof(CabinInformationDisplay))]
public sealed class CabinInformationDisplayEditor : Editor
{
    public static void DrawSettings(SerializedObject settings)
    {
        settings.Update();
        EditorGUILayout.HelpBox("最初の文言 → 追加の文言を上から順に繰り返します。追加0件なら常時表示。文章は枠に収まる長さ・改行で入力してください。怪異の案内が優先され、終了後に最初から再開します。", MessageType.Info);
        EditorGUILayout.PropertyField(settings.FindProperty("additionalMessages"), new GUIContent("追加の文言（＋で追加）"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("displaySeconds"), new GUIContent("1枚の表示時間（秒）"));
        EditorGUILayout.PropertyField(settings.FindProperty("fadeSeconds"), new GUIContent("片道のフェード時間（秒）", "消える時間と現れる時間それぞれ。大きいほどゆっくり。0で瞬時切替。怪異の提示速度には影響しません。"));
        settings.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        var display = (CabinInformationDisplay)target;
        EditorGUILayout.HelpBox("Play停止中に編集してCtrl+Sで保存してください。", MessageType.Info);
        bool found = false;
        foreach (var root in display.gameObject.scene.GetRootGameObjects())
        foreach (var group in root.GetComponentsInChildren<GameTextCollection>(true))
        {
            var copy = new SerializedObject(group);
            var entries = copy.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").stringValue != "cabin.standby") continue;
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("value"), new GUIContent("最初の文言"));
                if (copy.ApplyModifiedProperties()) group.Apply();
                found = true;
            }
        }
        if (!found)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("standbyMessage"), new GUIContent("最初の文言"));
            serializedObject.ApplyModifiedProperties();
        }
        DrawSettings(serializedObject);
    }
}
