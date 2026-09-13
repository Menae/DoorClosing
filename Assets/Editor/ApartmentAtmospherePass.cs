using System;
using UnityEditor;
using UnityEngine;

namespace GraduationProject.EditorTools
{
    // Applied last: shared material identities/GUIDs and all interaction geometry are retained.
    internal static class ApartmentAtmospherePass
    {
        private const string Folder="Assets/ApartmentVisuals/";
        public static void Apply(Transform cabin,Transform entrance,Transform corridor)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Folder+"ApartmentSurface.shader");
            if(shader==null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Apartment surface shader must compile before applying.");
            Surface("WarmPlaster",shader,1.3f,.75f,.0008f);
            Surface("IvoryLaminate",shader,1f,.7f,.00035f);
            Surface("DoorSteel",shader,.65f,.55f,.00008f,true);
            Surface("GreenStone",shader,1f/1.2f,.6f,.0002f);
            var stone=AssetDatabase.LoadAssetAtPath<Material>(Folder+"GreenStone.mat");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"AgedGreenStone.png");
            if(texture==null)throw new InvalidOperationException("Import the selected mineral reference texture first.");
            stone.SetTexture("_BaseMap",texture);stone.SetColor("_BaseColor",new Color(.68f,.72f,.67f));
            EditorUtility.SetDirty(stone);
            // Painted residential doors are not stone, although the blockout shared their material.
            var paint=AssetDatabase.LoadAssetAtPath<Material>(Folder+"ResidencePaint.mat");
            if(paint==null){paint=new Material(shader);AssetDatabase.CreateAsset(paint,Folder+"ResidencePaint.mat");}
            paint.SetColor("_BaseColor",new Color(.26f,.31f,.28f));paint.SetFloat("_Metallic",.05f);
            paint.SetFloat("_Smoothness",.3f);paint.SetFloat("_Wear",.6f);paint.SetFloat("_Relief",.00016f);
            EditorUtility.SetDirty(paint);
            foreach(var renderer in corridor.GetComponentsInChildren<Renderer>(true))
                if(renderer.name=="HomeDoor" || renderer.name=="Leaf" || renderer.name=="HomeKickPlate") renderer.sharedMaterial=paint;
            // Four tiles span the source image: a repeat is 1.2 metres, including a stable grout width.
            Surface("StoneFloor",shader,1f/1.2f,.2f,.00025f);
            foreach(var lamp in cabin.GetComponentsInChildren<Light>(true))
            {
                // One downward cone instead of six cube shadows from an omnidirectional point.
                lamp.type=LightType.Spot;lamp.transform.rotation=Quaternion.Euler(90,0,0);
                lamp.spotAngle=155;lamp.innerSpotAngle=120;lamp.intensity=3.8f;lamp.range=5;
                lamp.shadows=LightShadows.Soft;lamp.shadowBias=.025f;lamp.shadowNormalBias=.15f;
                lamp.shadowCustomResolution=512;
            }
            LightHall(entrance,false);LightHall(corridor,true);
        }

        private static void Surface(string name,Shader shader,float scale,float wear,float relief,bool moving=false)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");
            if(m==null)throw new InvalidOperationException("Missing existing material "+name);
            m.shader=shader;m.shaderKeywords=Array.Empty<string>();
            m.SetFloat("_WorldScale",scale);m.SetFloat("_Wear",wear);m.SetFloat("_Relief",relief);
            m.SetFloat("_ObjectSpace",moving?1:0);
            EditorUtility.SetDirty(m);
        }

        private static void LightHall(Transform root,bool home)
        {
            var visual=root.Find("InteriorVisuals");
            int count=home?3:2;
            for(int i=0;i<count;i++)
            {
                var lamp=visual.Find("FixtureLight"+i).GetComponent<Light>();
                // The middle fixture sits close to the abnormal pillar; avoid a clipped hotspot.
                lamp.intensity=home?(i==1?.7f:4.2f):4.4f;
                lamp.innerSpotAngle=95;lamp.spotAngle=135;
                lamp.color=i%2==0?new Color(.95f,.97f,1):new Color(.95f,1,.94f);
                lamp.shadows=LightShadows.Soft;lamp.shadowBias=.025f;lamp.shadowNormalBias=.15f;
                lamp.shadowCustomResolution=512;
            }
        }
    }
}
