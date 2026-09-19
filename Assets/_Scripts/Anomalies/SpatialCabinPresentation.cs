using System.Collections;
using UnityEngine;

/// <summary>Visual extension above the cabin. Never moves the player, controls or colliders.</summary>
public sealed class SpatialCabinPresentation : MonoBehaviour
{
    [SerializeField] private Transform movingCeiling;
    [SerializeField] private Transform upperWalls;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private string sourceRootName = "Building";
    [SerializeField] private string[] sourceCeilingPaths;
    [SerializeField, Min(0)] private float diagnosisRise = 4f;
    [SerializeField, Min(0)] private float revealedRise = 6f;
    [SerializeField, Min(.1f)] private float riseSeconds = 3f;
    [SerializeField, Min(.1f)] private float revealSeconds = 1.4f;

    private Renderer[] originals;
    private bool[] originalEnabled;
    private Coroutine motion;
    private bool ownsCeiling;
    public float CurrentRise { get; private set; }
    public bool IsActive => ownsCeiling;

    private void Awake() => visualRoot.SetActive(false);

    public void Begin()
    {
        Restore();
        Transform building = null;
        foreach (var root in gameObject.scene.GetRootGameObjects())
            if (root.name == sourceRootName) building = root.transform;
        if (building == null || sourceCeilingPaths == null || sourceCeilingPaths.Length == 0)
        {
            Debug.LogError("空間異常の元天井が見つかりません。Prefabの接続設定を確認してください。", this);
            return;
        }
        originals = new Renderer[sourceCeilingPaths.Length];
        originalEnabled = new bool[originals.Length];
        // Validate every binding before changing any source renderer.
        for (int i = 0; i < originals.Length; i++)
        {
            var source = building.Find(sourceCeilingPaths[i]);
            originals[i] = source != null ? source.GetComponent<Renderer>() : null;
            if (originals[i] == null)
            {
                Debug.LogError("空間異常の接続切れ: " + sourceCeilingPaths[i], this);
                return;
            }
            originalEnabled[i] = originals[i].enabled;
        }
        ownsCeiling = true;
        foreach (var renderer in originals) renderer.enabled = false;
        SetRise(0);
        visualRoot.SetActive(true);
        AnimateTo(diagnosisRise, riseSeconds);
    }

    public void Reveal()
    {
        if (ownsCeiling) AnimateTo(Mathf.Max(diagnosisRise, revealedRise), revealSeconds);
    }

    private void AnimateTo(float target, float seconds)
    {
        if (motion != null) StopCoroutine(motion);
        motion = StartCoroutine(Rise(target, seconds));
    }

    private IEnumerator Rise(float target, float seconds)
    {
        float start = CurrentRise;
        for (float elapsed = 0; elapsed < seconds; elapsed += Time.deltaTime)
        {
            SetRise(Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed / seconds)));
            yield return null;
        }
        SetRise(target);
        motion = null;
    }

    private void SetRise(float height)
    {
        CurrentRise = height;
        movingCeiling.localPosition = Vector3.up * height;
        upperWalls.localScale = new Vector3(1, Mathf.Max(.001f, height), 1);
    }

    public void Restore()
    {
        if (motion != null) StopCoroutine(motion);
        motion = null;
        if (ownsCeiling)
            for (int i = 0; i < originals.Length; i++)
                if (originals[i] != null) originals[i].enabled = originalEnabled[i];
        ownsCeiling = false;
        if (visualRoot != null) visualRoot.SetActive(false);
        if (movingCeiling != null && upperWalls != null) SetRise(0);
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
