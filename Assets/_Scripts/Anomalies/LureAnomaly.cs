using TMPro;
using UnityEngine;

public class LureAnomaly : AnomalyBehaviour
{
    [Header("Lure")]
    [SerializeField] private GameObject hallwayRoot;
    [SerializeField] private TMP_Text plateText;
    [SerializeField] private string fakePlateString = "708";
    [SerializeField] private Light hallwayLight;
    [SerializeField] private Color revealColor = new Color(0.25f, 1f, 0.35f);
    [SerializeField] private AudioSource revealDrone;

    private const float RevealedLightIntensityScale = 0.3f;
    private const string BrokenPlateString = "7#8";

    protected override void Awake()
    {
        base.Awake();

        if (hallwayRoot != null)
        {
            hallwayRoot.SetActive(false);
        }
    }

    public override void OnDiagnosisStart()
    {
        base.OnDiagnosisStart();
        SetActiveIfPresent(hallwayRoot, nameof(hallwayRoot), true);
        SetTextOrLog(plateText, nameof(plateText), fakePlateString);
    }

    public override void OnReveal()
    {
        base.OnReveal();

        if (HasReference(hallwayLight, nameof(hallwayLight)))
        {
            hallwayLight.color = revealColor;
            hallwayLight.intensity *= RevealedLightIntensityScale;
        }

        PlayAudioOrLog(revealDrone, nameof(revealDrone), "Lure reveal drone playback");
        SetTextOrLog(plateText, nameof(plateText), BrokenPlateString);
    }

    public override void OnGraceStart()
    {
        base.OnGraceStart();
    }

    public override void OnGraceEnd(bool recovered)
    {
        base.OnGraceEnd(recovered);
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        SetActiveIfPresent(hallwayRoot, nameof(hallwayRoot), false);
        StopAudioIfPresent(revealDrone, nameof(revealDrone));
    }
}
