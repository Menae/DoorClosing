using UnityEngine;

[AddComponentMenu("Graduation Project/暴走時のかご内設備")]
public sealed class CabinPowerPresentation : MonoBehaviour
{
    [SerializeField] private Light cabinLight;
    [SerializeField, Range(.35f, 1)] private float diagnosisBrightness = .85f;
    [SerializeField, Range(.35f, 1)] private float revealedBrightness = .65f;
    [SerializeField, Min(.1f)] private float fadeSeconds = 1.1f;
    private HijackAnomaly owner;
    private float originalIntensity, scale = 1, target = 1;
    internal void Begin(HijackAnomaly source)
    {
        Restore(owner); owner = source; scale = 1; target = diagnosisBrightness;
        if (cabinLight != null) originalIntensity = cabinLight.intensity;
    }
    internal void Reveal(HijackAnomaly source) { if (source == owner) target = revealedBrightness; }
    internal void Restore(HijackAnomaly source)
    {
        if (owner == null || source != owner) return;
        if (cabinLight != null) cabinLight.intensity = originalIntensity;
        owner = null; scale = target = 1;
    }
    private void Update()
    {
        if (owner == null || cabinLight == null) return;
        scale = Mathf.MoveTowards(scale, target, Time.deltaTime / Mathf.Max(.1f, fadeSeconds));
        cabinLight.intensity = originalIntensity * scale;
    }
    private void OnDisable() => Restore(owner);
}
