using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpatialCabinPresentation))]
public sealed class SpatialCabinPresentationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("天井と上部壁の見た目だけを伸ばします。床・操作盤・Colliderは動かしません。非常停止の判定と期限はhijack_space.asset。", MessageType.Info);
        Field("diagnosisRise", "通常の異常時に伸びる高さ（m）");
        Field("riseSeconds", "伸びるまでの秒数");
        Field("revealedRise", "誤操作・放置後の高さ（m）");
        Field("revealSeconds", "悪化時に伸びる秒数");
        EditorGUILayout.Space();
        Field("movingCeiling", "移動する天井");
        Field("upperWalls", "伸びる上部壁");
        Field("visualRoot", "表示一式");
        Field("sourceRootName", "元天井があるシーンのルート名");
        Field("sourceCeilingPaths", "一時非表示にする天井（ルートからのパス）");
        serializedObject.ApplyModifiedProperties();
    }

    private void Field(string name, string label) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label), true);
}
