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
        EditorGUILayout.HelpBox("遭遇の順番は Beat Definitions。各定義を選ぶと判定・猶予・Prefabを確認できます。以下は最初の誘引だけを差し替えます。Play中に比較すると停止時に元に戻ります。停止中の変更はシーンに保存されます。",MessageType.Info);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("誘引：柱を試す")) ReplaceLure("Assets/Data/lure.asset");
            if(GUILayout.Button("誘引：濡れを試す")) ReplaceLure(GraduationProject.EditorTools.WetLureBuilder.DefinitionPath);
        }
        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector();
    }
    private void ReplaceLure(string path)
    {
        var definition=AssetDatabase.LoadAssetAtPath<BeatDefinition>(path);
        if(definition==null) { Debug.LogWarning("先に怪異assetを作成してください: "+path); return; }
        var list=serializedObject.FindProperty("beatDefinitions");
        for(int i=0;i<list.arraySize;i++)
        {
            var item=list.GetArrayElementAtIndex(i);
            if(item.objectReferenceValue is BeatDefinition beat && beat.Category==AnomalyCategory.Lure)
            { item.objectReferenceValue=definition; return; }
        }
        Debug.LogWarning("この遭遇リストに誘引がありません。",target);
    }
}
