using UnityEngine;

public sealed class DestinationButtonLamp : MonoBehaviour
{
    public Renderer rim;
    public ElevatorController elevator;
    private Material original, lightMaterial;
    private bool selected;
    private void Awake()
    {
        if(rim==null || elevator==null) return;
        original=rim.sharedMaterial;
        lightMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lightMaterial.SetColor("_BaseColor",new Color(1f,.46f,.075f));
    }
    private void LateUpdate()
    {
        if(rim==null || elevator==null || lightMaterial==null) return;
        if(selected==elevator.DestinationSelected) return;
        selected=elevator.DestinationSelected;
        rim.sharedMaterial=selected?lightMaterial:original;
    }
    private void OnDestroy() { if(rim!=null && original!=null) rim.sharedMaterial=original; if(lightMaterial!=null) Destroy(lightMaterial); }
}
