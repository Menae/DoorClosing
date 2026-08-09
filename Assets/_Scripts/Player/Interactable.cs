using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Action")]
    [SerializeField] private PlayerAction actionType = PlayerAction.None;
    [SerializeField] private int floorNumber = -1;

    [Header("Hover Highlight")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField, Min(0f)] private float hoverEmissionIntensity = 1.5f;

    private Material[] runtimeMaterials;
    private Color[] originalColors;
    private Color[] originalEmissionColors;
    private bool isHovered;

    public PlayerAction ActionType => actionType;
    public int FloorNumber => floorNumber;

    private void Awake()
    {
        CacheMaterials();
    }

    private void Reset()
    {
        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;
        ApplyHoverState();
    }

    private void CacheMaterials()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        int materialCount = 0;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                materialCount += targetRenderers[i].materials.Length;
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
                originalColors[index] = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                originalEmissionColors[index] = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                index++;
            }
        }
    }

    private void ApplyHoverState()
    {
        if (runtimeMaterials == null)
        {
            return;
        }

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material material = runtimeMaterials[i];
            if (material == null)
            {
                continue;
            }

            if (isHovered)
            {
                SetMaterialColor(material, hoverColor);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", hoverColor * hoverEmissionIntensity);
                }
            }
            else
            {
                SetMaterialColor(material, originalColors[i]);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", originalEmissionColors[i]);
                }
            }
        }
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
}
