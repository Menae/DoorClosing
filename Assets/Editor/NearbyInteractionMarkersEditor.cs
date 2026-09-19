using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NearbyInteractionMarkers))]
public sealed class NearbyInteractionMarkersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("近くの操作対象を▲で示します。遮蔽物・ポーズ中は非表示。テンキーは盤全体で1個。浮遊幅を0にすると揺れを止められます。", MessageType.Info);
        Field("size", "▲の大きさ（1080p基準）");
        Field("fadeSeconds", "出現・消失／注視の秒数");
        Field("floatPixels", "浮遊の幅（0で停止）");
        Field("floatPeriod", "浮遊の周期（秒）");
        Field("focusScale", "狙ったときの拡大率");
        Field("view", "プレイヤーカメラ"); Field("font", "表示フォント");
        serializedObject.ApplyModifiedProperties();
    }
    private void Field(string key, string label) => EditorGUILayout.PropertyField(serializedObject.FindProperty(key), new GUIContent(label));
}
