using System;
using UnityEngine;

[AddComponentMenu("Graduation Project/空間ごとの設備音")]
public sealed class ApartmentRoomTone : MonoBehaviour
{
    [Serializable]
    private sealed class Zone
    {
        public string label;
        public Transform environment;
        public AudioSource source;
        public Bounds listeningArea;
        [Range(0, 1)] public float level = .4f;
    }
    [Header("既存の換気音量を共通マスターとして使用")]
    [SerializeField] private ElevatorTuning tuning;
    [SerializeField] private Transform listener;
    [SerializeField] private Zone[] zones = Array.Empty<Zone>();
    [SerializeField, Min(.05f)] private float blendSeconds = .8f;
    [SerializeField, Min(.1f)] private float boundaryBlend = 1f;

    private void Update()
    {
        foreach (var zone in zones)
        {
            if (zone?.source == null) continue;
            if (zone.source.clip == null) { zone.source.Stop(); zone.source.volume = 0; continue; }
            bool available = listener != null && zone.environment != null && zone.environment.gameObject.activeInHierarchy;
            float proximity = available ? 1 - Mathf.Clamp01(Mathf.Sqrt(zone.listeningArea.SqrDistance(listener.position)) / boundaryBlend) : 0;
            float target = proximity * zone.level * (tuning != null ? tuning.換気音 : .2f);
            // Muted title / transitions remain deterministic. AudioListener.pause handles pause.
            if (DemoSession.BlocksGameplay) target = 0;
            zone.source.volume = Mathf.MoveTowards(zone.source.volume, target, Time.unscaledDeltaTime / blendSeconds);
            if (zone.source.volume > .0001f && !zone.source.isPlaying && !AudioListener.pause) zone.source.Play();
            if (zone.source.volume <= .0001f && zone.source.isPlaying) zone.source.Stop();
        }
    }
    private void OnDisable()
    {
        foreach (var zone in zones) if (zone?.source != null) { zone.source.Stop(); zone.source.volume = 0; }
    }
}
