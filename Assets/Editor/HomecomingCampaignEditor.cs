using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HomecomingCampaign))]
public sealed class HomecomingCampaignEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("全4夜：通常帰宅→基本3種→別表現3種→各系統1種と順序を抽選。死亡しても現在夜から再開します。OFFならRun Managerの比較リストを使用します。", MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playFullStory"), new GUIContent("全4夜を通して遊ぶ"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("firstNight"), new GUIContent("怪異夜1（各系統1件・固定順）"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("secondNight"), new GUIContent("怪異夜2（各系統1件・固定順）"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("laterGraceScale"), new GUIContent("後半夜の修正猶予倍率"));
        if (Application.isPlaying)
        {
            var campaign = (HomecomingCampaign)target;
            EditorGUILayout.LabelField("現在の夜 / 試行", campaign.CurrentNight + " / " + campaign.Attempt);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
