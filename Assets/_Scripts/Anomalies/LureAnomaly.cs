using TMPro;
using UnityEngine;

public class LureAnomaly : AnomalyBehaviour
{
    [Header("Lure")]
    [SerializeField] private GameObject hallwayRoot;
    [SerializeField] private TMP_Text plateText;
    [SerializeField] private string fakePlateString = "708";
    [SerializeField] private Light hallwayLight;
    [SerializeField] private Color hallwayRevealColor = new Color(0.25f, 1f, 0.35f);
    [SerializeField] private AudioSource revealDrone;

    private const float RevealedLightIntensityScale = 0.3f;
    private const string BrokenPlateString = "7#8";
    private const float TrialRevealLightIntensity = 5f;
    private const float TrialGraceLightIntensity = 3.5f;

    private AudioClip generatedRevealClip;
    private bool usesGeneratedRevealLight;
    private LureCorridorPresentation presentation;

    internal void BindPresentation(LureCorridorPresentation value) => presentation = value;

    protected override void Awake()
    {
        base.Awake();
        EnsurePrototypeRevealPresentation();

        if (hallwayRoot != null)
        {
            hallwayRoot.SetActive(false);
        }
    }

    // Appearance is prepared while the doors are still closed. Input/danger stays
    // owned by Diagnosis, so looking through the first gap cannot advance the timer.
    internal void PrepareArrival()
    {
        SetActiveIfPresent(hallwayRoot, nameof(hallwayRoot), true);
        SetTextOrLog(plateText, nameof(plateText), GameTextCollection.Get(this, "lure.plate", fakePlateString));
    }

    public override void OnDiagnosisStart()
    {
        base.OnDiagnosisStart();
        PrepareArrival();
        if (presentation != null) presentation.Begin(this);
    }

    public override void OnReveal()
    {
        if (presentation != null) { presentation.Reveal(this); return; }
        base.OnReveal();

        if (HasReference(hallwayLight, nameof(hallwayLight)))
        {
            hallwayLight.color = hallwayRevealColor;
            hallwayLight.intensity = usesGeneratedRevealLight
                ? TrialRevealLightIntensity
                : hallwayLight.intensity * RevealedLightIntensityScale;
            hallwayLight.gameObject.SetActive(true);
        }

        PlayAudioOrLog(revealDrone, nameof(revealDrone), "Lure reveal drone playback");
        SetTextOrLog(plateText, nameof(plateText), GameTextCollection.Get(this, "lure.broken", BrokenPlateString));
    }

    public override void OnGraceStart()
    {
        if (presentation != null) return;
        base.OnGraceStart();
        if (hallwayLight != null)
        {
            hallwayLight.color = new Color(1f, 0.45f, 0.08f);
            if (usesGeneratedRevealLight)
            {
                hallwayLight.intensity = TrialGraceLightIntensity;
            }
        }
    }

    public override void OnGraceEnd(bool recovered)
    {
        if (presentation != null) return;
        base.OnGraceEnd(recovered);
        if (recovered && hallwayLight != null)
        {
            hallwayLight.color = new Color(0.2f, 0.8f, 0.45f);
        }
    }

    public override void OnCleanup()
    {
        if (presentation != null) presentation.Restore(this);
        base.OnCleanup();
        SetActiveIfPresent(hallwayRoot, nameof(hallwayRoot), false);
        StopAudioIfPresent(revealDrone, nameof(revealDrone));

        if (generatedRevealClip != null)
        {
            Destroy(generatedRevealClip);
            generatedRevealClip = null;
        }
    }

    protected override void OnDestroy()
    {
        if (presentation != null) presentation.Restore(this);
        if (generatedRevealClip != null) Destroy(generatedRevealClip);
        base.OnDestroy();
    }

    private void EnsurePrototypeRevealPresentation()
    {
        if (hallwayLight == null && hallwayRoot != null)
        {
            usesGeneratedRevealLight = true;
            GameObject lightObject = new GameObject("TrialRevealLight");
            lightObject.transform.SetParent(hallwayRoot.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, 1.1f);
            hallwayLight = lightObject.AddComponent<Light>();
            hallwayLight.type = LightType.Point;
            hallwayLight.range = 7f;
            hallwayLight.intensity = TrialRevealLightIntensity;
            hallwayLight.shadows = LightShadows.None;
            lightObject.SetActive(false);
        }

        if (revealDrone == null)
        {
            revealDrone = gameObject.AddComponent<AudioSource>();
            revealDrone.playOnAwake = false;
            revealDrone.loop = true;
            revealDrone.spatialBlend = 0.65f;
            revealDrone.volume = 0.28f;
            revealDrone.pitch = 0.65f;
            generatedRevealClip = GeneratedTone.CreateMechanicalLoop("LureTrialRevealDrone");
            revealDrone.clip = generatedRevealClip;
        }
    }
}
