using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoiceProvocationAnomaly))]
public sealed class VoiceProvocationAnomalyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("扉の外からの声だけを担当します。正解・猶予はprovocation_voice.assetで調整。台本欄は制作メモです。文章を書き換えたらVOICEVOX等で音声を再生成し、クリップも差し替えてください。", MessageType.Info);
        Field("speaker", "扉の外の音源");
        Field("pleaClip", "最初の呼びかけ");
        Field("revealedClip", "誤操作後の声");
        Field("voiceVolume", "音量");
        Field("firstDelay", "最初の声までの秒数");
        Field("silenceBetweenCalls", "再生終了から次の声までの秒数");
        Field("pleaScript", "最初の台本（制作メモ）");
        Field("revealedScript", "誤操作後の台本（制作メモ）");
        EditorGUILayout.HelpBox("仮音声：VOICEVOX:白上虎太郎。生成条件・利用条件はdocs/licenses/OutsideVoice.txt。声の自然さと怖さは本人評価待ち。", MessageType.None);
        serializedObject.ApplyModifiedProperties();
    }

    private void Field(string name, string label) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label), true);
}
