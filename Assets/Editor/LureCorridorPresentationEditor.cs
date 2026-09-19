using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LureCorridorPresentation))]
public sealed class LureCorridorPresentationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("本編の誘引演出。奥から照明が弱まります。帰路の照明は配列に入れないでください。正解操作・猶予時間はBeatDefinition側で管理します。", MessageType.Info);
        Field("lamps", "変化する照明（奥→手前）");
        Field("lampInterval", "照明間の遅れ（秒）"); Field("fadeSeconds", "1灯が弱まる時間（秒）");
        Field("remainingLight", "変化後の明るさ（元の割合）");
        Field("drone", "設備音のAudio Source"); Field("droneVolume", "設備音量"); Field("soundFadeSeconds", "音の立ち上がり（秒）");
        serializedObject.ApplyModifiedProperties();
    }
    private void Field(string name, string label) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label), true);
}
