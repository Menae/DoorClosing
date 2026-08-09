using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class AnomalyBehaviour : MonoBehaviour
{
    [Header("Debug Visuals")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color diagnosisColor = Color.white;
    [SerializeField] private Color revealColor = Color.red;
    [SerializeField] private Color graceColor = Color.yellow;
    [SerializeField] private Color recoveredColor = Color.green;
    [SerializeField] private Color failedColor = Color.black;
    [SerializeField, Min(0f)] private float emissionIntensity = 1.5f;

    private Material[] runtimeMaterials;
    private Color[] originalColors;
    private Color[] originalEmissionColors;
    private readonly HashSet<string> warnedMissingReferences = new HashSet<string>();

    protected virtual void Awake()
    {
        CacheMaterials();
    }

    protected virtual void Reset()
    {
        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    public virtual void OnDiagnosisStart()
    {
        Debug.Log($"[Anomaly] {name} OnDiagnosisStart", this);
        ApplyColor(diagnosisColor);
    }

    public virtual void OnReveal()
    {
        Debug.Log($"[Anomaly] {name} OnReveal", this);
        ApplyColor(revealColor);
    }

    public virtual void OnGraceStart()
    {
        Debug.Log($"[Anomaly] {name} OnGraceStart", this);
        ApplyColor(graceColor);
    }

    public virtual void OnGraceEnd(bool recovered)
    {
        Debug.Log($"[Anomaly] {name} OnGraceEnd recovered={recovered}", this);
        ApplyColor(recovered ? recoveredColor : failedColor);
    }

    public virtual void OnCleanup()
    {
        Debug.Log($"[Anomaly] {name} OnCleanup", this);
        RestoreOriginalColors();
    }

    private void CacheMaterials()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            WarnMissingReferenceOnce(nameof(targetRenderers));
            runtimeMaterials = new Material[0];
            originalColors = new Color[0];
            originalEmissionColors = new Color[0];
            return;
        }

        int materialCount = 0;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                materialCount += targetRenderers[i].materials.Length;
            }
            else
            {
                WarnMissingReferenceOnce($"{nameof(targetRenderers)}[{i}]");
            }
        }

        runtimeMaterials = new Material[materialCount];
        originalColors = new Color[materialCount];
        originalEmissionColors = new Color[materialCount];

        int index = 0;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
            {
                continue;
            }

            Material[] materials = targetRenderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                runtimeMaterials[index] = material;
                originalColors[index] = GetMaterialColor(material);
                originalEmissionColors[index] = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                index++;
            }
        }
    }

    private void ApplyColor(Color color)
    {
        if (runtimeMaterials == null || runtimeMaterials.Length == 0)
        {
            WarnMissingReferenceOnce(nameof(targetRenderers));
            return;
        }

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material material = runtimeMaterials[i];
            if (material == null)
            {
                continue;
            }

            SetMaterialColor(material, color);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emissionIntensity);
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (runtimeMaterials == null || runtimeMaterials.Length == 0)
        {
            WarnMissingReferenceOnce(nameof(targetRenderers));
            return;
        }

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material material = runtimeMaterials[i];
            if (material == null)
            {
                continue;
            }

            SetMaterialColor(material, originalColors[i]);

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", originalEmissionColors[i]);
            }
        }
    }

    private static Color GetMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    protected bool HasReference(Object reference, string fieldName)
    {
        if (reference != null)
        {
            return true;
        }

        WarnMissingReferenceOnce(fieldName);
        return false;
    }

    protected void PlayAudioOrLog(AudioSource audioSource, string fieldName, string fallbackMessage)
    {
        if (audioSource != null)
        {
            audioSource.Play();
            return;
        }

        WarnMissingReferenceOnce(fieldName);
        LogFallback(fallbackMessage);
    }

    protected void StopAudioIfPresent(AudioSource audioSource, string fieldName)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            return;
        }

        WarnMissingReferenceOnce(fieldName);
    }

    protected void SetTextOrLog(TMP_Text text, string fieldName, string message)
    {
        if (text != null)
        {
            text.text = message;
            return;
        }

        WarnMissingReferenceOnce(fieldName);
        LogFallback(message);
    }

    protected void SetActiveIfPresent(GameObject target, string fieldName, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
            return;
        }

        WarnMissingReferenceOnce(fieldName);
    }

    protected void LogFallback(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Debug.Log($"[Anomaly][Fallback] {name}: {message}", this);
        }
    }

    protected void WarnMissingReferenceOnce(string fieldName)
    {
        if (!warnedMissingReferences.Add(fieldName))
        {
            return;
        }

        Debug.LogWarning($"[Anomaly] {name} missing reference '{fieldName}'. Skipping that effect.", this);
    }
}
