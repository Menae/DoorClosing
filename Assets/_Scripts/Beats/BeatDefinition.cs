using UnityEngine;

[CreateAssetMenu(fileName = "BeatDefinition", menuName = "Horror Elevator/Beat Definition")]
public class BeatDefinition : ScriptableObject
{
    [Header("Debug")]
    [SerializeField] private string debugLabel = "Beat";

    [Header("Response")]
    [SerializeField] private AnomalyCategory category = AnomalyCategory.Normal;
    [SerializeField] private PlayerAction correctAction = PlayerAction.PressClose;
    [SerializeField] private int correctFloorNumber = -1;

    [Header("Beat Flow")]
    [SerializeField, Min(0f)] private float travelSeconds = 4f;
    [SerializeField, Min(0f)] private float revealSeconds = 2f;
    [SerializeField, Min(0f)] private float graceSeconds = 3f;
    [SerializeField, Min(0f)] private float passiveSuccessSeconds;
    [SerializeField] private bool doorsStayClosed;

    [Header("Floor Display")]
    [SerializeField] private int displayFloor = 1;

    [Header("Anomaly")]
    [SerializeField, Min(0f)] private float hijackDeadlineSeconds;
    [SerializeField] private GameObject anomalyPrefab;

    public string DebugLabel => debugLabel;
    public AnomalyCategory Category => category;
    public PlayerAction CorrectAction => correctAction;
    public int CorrectFloorNumber => correctFloorNumber;
    public float TravelSeconds => travelSeconds;
    public float RevealSeconds => revealSeconds;
    public float GraceSeconds => graceSeconds;
    public float PassiveSuccessSeconds => passiveSuccessSeconds;
    public bool DoorsStayClosed => doorsStayClosed;
    public int DisplayFloor => displayFloor;
    public float HijackDeadlineSeconds => hijackDeadlineSeconds;
    public GameObject AnomalyPrefab => anomalyPrefab;
    public bool HasHijackDeadline => category == AnomalyCategory.Hijack && hijackDeadlineSeconds > 0f;
    public bool HasPassiveSuccess => category == AnomalyCategory.Provocation && passiveSuccessSeconds > 0f;
}
