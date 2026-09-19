using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(RunManager))]
public sealed class RunManagerEditor : Editor
{
    [MenuItem("Tools/Unity Agent/怪異構成を開く")]
    private static void Open()
    {
        var run=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RunManager>(true)).FirstOrDefault();
        if(run!=null) Selection.activeGameObject=run.gameObject;
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("遭遇の順番は Beat Definitions。各定義を選ぶと判定・猶予・Prefabを確認できます。以下は各系統の最初の遭遇だけを差し替えます。Play中に比較すると停止時に元に戻ります。停止中の変更はシーンに保存されます。",MessageType.Info);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("誘引：柱を試す")) ReplaceVariant("Assets/Data/lure.asset");
            if(GUILayout.Button("誘引：濡れを試す")) ReplaceVariant(GraduationProject.EditorTools.WetLureBuilder.DefinitionPath);
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("挑発：設備放送を試す")) ReplaceVariant("Assets/Data/provocation.asset");
            if(GUILayout.Button("挑発：外からの声を試す")) ReplaceVariant(GraduationProject.EditorTools.VoiceProvocationBuilder.DefinitionPath);
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("乗っ取り：機械暴走を試す")) ReplaceVariant("Assets/Data/hijack.asset");
            if(GUILayout.Button("乗っ取り：空間異常を試す")) ReplaceVariant(GraduationProject.EditorTools.SpatialHijackBuilder.DefinitionPath);
        }
        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector();
    }
    private void ReplaceVariant(string path)
    {
        if (Application.isPlaying && ((RunManager)target).GetComponent<HomecomingCampaign>() is HomecomingCampaign campaign && campaign.FullStory)
        { Debug.LogWarning("全4夜から比較へ切り替えるときはPlayを停止してください。",target); return; }
        var definition=AssetDatabase.LoadAssetAtPath<BeatDefinition>(path);
        if(definition==null) { Debug.LogWarning("先に怪異assetを作成してください: "+path); return; }
        var list=serializedObject.FindProperty("beatDefinitions");
        for(int i=0;i<list.arraySize;i++)
        {
            var item=list.GetArrayElementAtIndex(i);
            if(item.objectReferenceValue is BeatDefinition beat && beat.Category==definition.Category)
            { item.objectReferenceValue=definition; UseComparisonList((RunManager)target); return; }
        }
        Debug.LogWarning("この遭遇リストに対象系統がありません。",target);
    }

    internal static void UseComparisonList(RunManager run)
    {
        var campaign = run.GetComponent<HomecomingCampaign>();
        if (campaign == null) return;
        var so = new SerializedObject(campaign);
        so.FindProperty("playFullStory").boolValue = false;
        so.ApplyModifiedProperties();
    }
}
