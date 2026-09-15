using UnityEngine;

[AddComponentMenu("Graduation Project/音と到着の調整")]
public sealed class ElevatorTuning : MonoBehaviour
{
    [Header("音量（Play中に試聴可能。保存はPlay停止後）")]
    [Range(0,1)] public float 走行音 = .16f;
    [Range(0,1)] public float 扉の動作音 = .12f;
    [Range(0,1)] public float 到着チャイム = .30f;
    [Range(0,1)] public float 怪異の走行音 = .24f;
    [Range(0,1)] public float 足音 = .45f;
    [Range(0,1)] public float 換気音 = .20f;
    [Header("タイミング（秒）")]
    [Min(0)] public float チャイムから開扉まで = 1.4f;
    [Min(.05f)] public float 怪異の階数上昇間隔 = 1f;
}
