using System;
using UnityEngine;

// Scene-owned presentation only. BeatStateMachine remains the authority for input, deadlines and outcomes.
public sealed class LureCorridorPresentation : MonoBehaviour
{
    [Serializable]
    private sealed class Lamp
    {
        public Light light;
        public Renderer diffuser;
        [NonSerialized] public float intensity;
        [NonSerialized] public Color surface, emission;
        [NonSerialized] public MaterialPropertyBlock original, working;
    }
    [Header("奥から手前の順。帰路の照明は含めない")]
    [SerializeField] private Lamp[] lamps = Array.Empty<Lamp>();
    [Header("誤降車後の変化")]
    [SerializeField, Min(0)] private float lampInterval = .35f;
    [SerializeField, Min(0)] private float fadeSeconds = .65f;
    [SerializeField, Range(0, 1)] private float remainingLight = .06f;
    [Header("奥から聞こえる低い設備音")]
    [SerializeField] private AudioSource drone;
    [SerializeField, Range(0, 1)] private float droneVolume = .12f;
    [SerializeField, Min(0)] private float soundFadeSeconds = 1.2f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private LureAnomaly owner;
    private bool hasSnapshot, revealing;
    private float elapsed;
    private float previousVolume;
    public bool IsRevealing => revealing;

    internal void Begin(LureAnomaly source)
    {
        RestoreState();
        owner = source; hasSnapshot = true; revealing = false;
        foreach (var lamp in lamps)
        {
            if (lamp == null) continue;
            if (lamp.light != null) lamp.intensity = lamp.light.intensity;
            if (lamp.diffuser == null) continue;
            lamp.original ??= new MaterialPropertyBlock(); lamp.working ??= new MaterialPropertyBlock();
            lamp.diffuser.GetPropertyBlock(lamp.original);
            var material = lamp.diffuser.sharedMaterial;
            lamp.surface = material != null && material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : Color.white;
            lamp.emission = material != null && material.HasProperty(EmissionColor) ? material.GetColor(EmissionColor) : Color.black;
        }
        if (drone != null) { previousVolume = drone.volume; drone.Stop(); }
    }

    internal void Reveal(LureAnomaly source)
    {
        if (!hasSnapshot || source != owner || revealing) return;
        revealing = true; elapsed = 0;
        if (drone != null && drone.clip != null) { drone.volume = 0; drone.Play(); }
    }

    private void Update()
    {
        if (!revealing) return;
        elapsed += Time.deltaTime;
        for (int i = 0; i < lamps.Length; i++)
        {
            var lamp = lamps[i]; if (lamp == null) continue;
            float t = Progress(elapsed - i * lampInterval, fadeSeconds);
            float scale = Mathf.Lerp(1, remainingLight, t);
            if (lamp.light != null) lamp.light.intensity = lamp.intensity * scale;
            if (lamp.diffuser != null)
            {
                lamp.diffuser.GetPropertyBlock(lamp.working);
                lamp.working.SetColor(BaseColor, lamp.surface * Mathf.Lerp(1, .12f, t));
                lamp.working.SetColor(EmissionColor, lamp.emission * scale);
                lamp.diffuser.SetPropertyBlock(lamp.working);
            }
        }
        if (drone != null) drone.volume = droneVolume * Progress(elapsed, soundFadeSeconds);
    }

    private static float Progress(float elapsed, float seconds) => elapsed < 0 ? 0 :
        Mathf.SmoothStep(0, 1, seconds <= 0 ? 1 : Mathf.Clamp01(elapsed / seconds));

    internal void Restore(LureAnomaly source)
    {
        // A destroyed earlier encounter must never restore over a newer encounter's state.
        if (source == owner) RestoreState();
    }

    private void RestoreState()
    {
        revealing = false;
        if (!hasSnapshot) return;
        foreach (var lamp in lamps)
        {
            if (lamp == null) continue;
            if (lamp.light != null) lamp.light.intensity = lamp.intensity;
            if (lamp.diffuser != null) lamp.diffuser.SetPropertyBlock(lamp.original);
        }
        if (drone != null) { drone.Stop(); drone.volume = previousVolume; }
        hasSnapshot = false; owner = null;
    }
    private void OnDisable() => RestoreState();
    private void OnDestroy() => RestoreState();
}
