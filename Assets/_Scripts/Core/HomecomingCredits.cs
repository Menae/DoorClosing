using System;
using UnityEngine;

[AddComponentMenu("Graduation Project/スタッフロール編集")]
public sealed class HomecomingCredits : MonoBehaviour
{
    [Serializable]
    public sealed class Page
    {
        public string heading;
        [TextArea(3, 16)] public string body;
    }
    [Header("結果画面の後に表示。文章・順序・ページ数を編集できます")]
    public Page[] pages =
    {
        new Page { heading = "音声・音素材", body = "VOICEVOX:白上虎太郎\n\n足音：HerbertBoland\nShoeLeatherSteps.wav（CC BY 4.0）\n切り出し・音量調整" },
        new Page { heading = "フォント・制作環境", body = "Noto Sans JP / Liberation Sans\nSIL Open Font License 1.1\n\nUnity / Universal Render Pipeline\n\n素材の出典・利用条件：同梱 licenses フォルダー" }
    };
    [Min(1)] public float secondsPerPage = 7;
    [Min(0)] public float fadeSeconds = .4f;
    public string skipLabel = "タイトルへ";
    public string resultButtonLabel = "スタッフロール";
}
