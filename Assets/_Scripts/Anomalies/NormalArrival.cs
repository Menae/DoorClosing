using TMPro;
using UnityEngine;

public class NormalArrival : AnomalyBehaviour
{
    [Header("Normal Arrival")]
    [SerializeField] private GameObject hallwayRoot;
    [SerializeField] private TextMeshProUGUI plateText;
    [SerializeField] private string realPlateString = "703";

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
        SetTextOrLog(plateText, nameof(plateText), realPlateString);
    }

    public override void OnReveal()
    {
        Debug.LogError("NormalArrival\u306bReveal\u304c\u547c\u3070\u308c\u305f\u2014\u2014\u5224\u5b9a\u5074\u306e\u8a2d\u8a08\u9055\u53cd", this);
    }

    public override void OnCleanup()
    {
        base.OnCleanup();
        SetActiveIfPresent(hallwayRoot, nameof(hallwayRoot), false);
    }
}
