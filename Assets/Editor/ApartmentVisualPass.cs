using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    // Reversible interior dressing, with the real-world control layout applied by ElevatorRealismPass.
    public static class ApartmentVisualPass
    {
        private const string Folder = "Assets/ApartmentVisuals";
        private static Material plaster, laminate, steel, dark, tile, paper, ink, red, light;
        private static TMP_FontAsset font;

        [MenuItem("Tools/Unity Agent/Apply Apartment Visual Pass")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty)
                throw new InvalidOperationException("Requires a saved M1/M2 scene in idle Edit Mode.");
            if (scene.path != M1NormalRouteBuilder.ScenePath && scene.path != M2VerticalSliceBuilder.ScenePath)
                throw new InvalidOperationException("Only M1NormalRoute and M2VerticalSlice are supported.");
            PrepareMaterials();
            var roots = scene.GetRootGameObjects();
            var building = roots.Single(x => x.name == "Building").transform;
            var hall = roots.Single(x => x.name == "EntranceHall").transform;
            var corridor = roots.Single(x => x.name == "HomeCorridor").transform;
            DressCabin(building);
            DressHall(hall, false);
            DressHall(corridor, true);
            ElevatorRealismPass.Apply(building, hall, roots);
            foreach (var root in roots)
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                text.richText = true;
            }
            var run = roots.SelectMany(x => x.GetComponentsInChildren<RunManager>(true)).FirstOrDefault();
            if (run != null)
            {
                var so = new SerializedObject(run);
                so.FindProperty("clearMessage").stringValue = "帰宅しました";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.48f, .49f, .50f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .35f, .35f);
            RenderSettings.ambientGroundColor = new Color(.19f, .185f, .17f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[VIS-01] Applied reference-based apartment dressing: " + scene.path);
        }

        private static void PrepareMaterials()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "ApartmentVisuals");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
            if (font == null) throw new InvalidOperationException("Japanese font missing.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Laminate.png");
            if (texture == null) throw new InvalidOperationException("Import Laminate.png before applying.");
            plaster = Mat("WarmPlaster", new Color(.8f,.8f,.75f), 0, .15f, texture, 3);
            laminate = Mat("IvoryLaminate", new Color(.82f,.75f,.61f), 0, .28f, texture, 1);
            var steelTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BrushedSteel.png");
            if (steelTexture == null) throw new InvalidOperationException("Import BrushedSteel.png before applying.");
            steel = Mat("SatinSteel", new Color(.85f,.86f,.85f), .72f, .34f, steelTexture);
            dark = Mat("Charcoal", new Color(.065f,.077f,.074f), .2f, .3f);
            var floorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/FloorTiles.png");
            if (floorTexture == null) throw new InvalidOperationException("Import FloorTiles.png before applying.");
            tile = Mat("StoneFloor", new Color(.72f,.73f,.72f), 0, .22f, floorTexture, 1f/1.2f);
            paper = Mat("NoticePaper", new Color(.89f,.88f,.81f), 0, .1f);
            ink = Mat("GreenStone", new Color(.14f,.19f,.17f), .15f, .42f, texture, 2);
            red = Mat("WarningRed", new Color(.48f,.025f,.018f), 0, .3f);
            light = Mat("FluorescentDiffuser", new Color(.9f,.95f,1), 0, .3f);
            light.EnableKeyword("_EMISSION");
            light.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            light.SetColor("_EmissionColor", new Color(1.4f,1.48f,1.5f));
            EditorUtility.SetDirty(light);
        }

        private static void DressCabin(Transform b)
        {
            foreach (string n in new[]{"CabBack","CabLeft","CabRight","DoorLeft","DoorRight"}) Surface(b,n,laminate);
            foreach (string n in new[]{"FrontLeft","FrontRight","OuterFrontLeft","OuterFrontRight"}) Surface(b,n,ink);
            Surface(b,"CabFloor",tile); Surface(b,"CabCeiling",dark);
            var v = Group(b,"InteriorVisuals");
            Box(v,"PanelBacking",new Vector3(.05f,1.375f,4.967f),new Vector3(1.5f,1.04f,.045f),steel);
            Box(v,"DisplayBacking",new Vector3(0,2.3f,4.955f),new Vector3(.52f,.32f,.035f),dark);
            var indicator=b.Find("FloorDisplay").GetComponent<TMP_Text>();
            indicator.fontSize=2.5f;indicator.rectTransform.sizeDelta=new Vector2(.48f,.28f);
            foreach (string n in new[]{"Call"})
            {
                var target = b.GetComponentsInChildren<Transform>(true).Single(t => t.name == n);
                // Trial 140 x 120 mm faces: smaller than the blockout, still legible at interaction distance.
                // Scale the original collider together with its visible face; never leave invisible oversized targets.
                target.localScale = new Vector3(.4f,.4f,1);
                target.GetComponent<Renderer>().sharedMaterial = n == "Emergency" ? red : dark;
                var text = target.GetComponentInChildren<TMP_Text>(true);
                text.font = font; text.enableAutoSizing = false;
                text.fontSize = n == "Emergency" ? 1.15f : 2.2f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.color = new Color(.96f,.96f,.9f);
                if (n == "Call") text.text = "呼";
                if (n == "Emergency") text.text = "非常\n停止";
                // Non-interactive trim sits behind the original face and its input feedback.
                Box(v,n+"Bezel",target.position+Vector3.forward*.016f,new Vector3(.165f,.145f,.055f),steel);
            }
            for(int x=-1;x<=1;x+=2)
            for(int y=-1;y<=1;y+=2)
                Screw(v,"PanelScrew"+x+"_"+y,new Vector3(.05f+x*.69f,1.375f+y*.46f,4.936f),Quaternion.identity);
            // Keep trim clear of button faces and the doorway safety volume.
            for (int side = -1; side <= 1; side += 2)
            {
                Box(v,"Jamb"+side,new Vector3(side*.85f,1.5f,1.875f),new Vector3(.09f,3,.15f),steel);
                Box(v,"SideSkirting"+side,new Vector3(side*1.586f,.12f,3.55f),new Vector3(.028f,.24f,2.9f),steel);
                Box(v,"SideRail"+side,new Vector3(side*1.53f,.9f,3.65f),new Vector3(.06f,.055f,2.35f),steel);
                for (int j=0;j<4;j++)
                    Box(v,"SideJoint"+side+"_"+j,new Vector3(side*1.592f,1.53f,2.35f+j*.8f),new Vector3(.012f,2.8f,.015f),dark);
            }
            Box(v,"Lintel",new Vector3(0,2.93f,1.88f),new Vector3(1.8f,.14f,.15f),steel);
            Box(b.Find("DoorLeft"),"VisualMeetingEdge",new Vector3(-.006f,1.45f,1.932f),new Vector3(.008f,2.88f,.006f),dark);
            Box(b.Find("DoorRight"),"VisualMeetingEdge",new Vector3(.006f,1.45f,1.932f),new Vector3(.008f,2.88f,.006f),dark);
            Box(v,"Threshold",new Vector3(0,.012f,2),new Vector3(1.63f,.018f,.25f),steel);
            for(int j=0;j<4;j++) Box(v,"ThresholdGroove"+j,new Vector3(0,.023f,1.91f+j*.06f),new Vector3(1.6f,.003f,.007f),dark);
            Box(v,"RearSkirting",new Vector3(0,.12f,4.987f),new Vector3(3.17f,.24f,.024f),steel);
            for(int j=0;j<5;j++) Box(v,"RearJoint"+j,new Vector3(-1.56f+j*.78f,1.5f,4.99f),new Vector3(.014f,2.8f,.013f),dark);
            for(int j=0;j<3;j++)
            {
                Box(v,"CeilingTray"+j,new Vector3(0,2.975f,2.55f+j*.8f),new Vector3(2.85f,.07f,.66f),steel);
                Box(v,"CeilingDiffuser"+j,new Vector3(0,2.93f,2.55f+j*.8f),new Vector3(2.65f,.018f,.49f),light);
                for(int k=0;k<13;k++)
                {
                    var old=v.Find("CeilingLouvre"+j+"_"+k);
                    if(old!=null)old.gameObject.SetActive(false);
                }
                var perforated=Group(v,"PerforatedCeiling"+j);
                perforated.position=new Vector3(0,2.913f,2.55f+j*.8f);
                var filter=perforated.GetComponent<MeshFilter>();
                if(filter==null)filter=perforated.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh=PerforatedSheet();
                var renderer=perforated.GetComponent<MeshRenderer>();
                if(renderer==null)renderer=perforated.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=dark;
            }
            Box(v,"CapacityPlate",new Vector3(0,2.65f,4.963f),new Vector3(1.20f,.16f,.012f),steel);
            Label(v,"CabinCapacity","定員９名　積載６００kg",new Vector3(0,2.65f,4.951f),new Vector2(1.16f,.14f),.65f,Color.black);
            Label(v,"PanelCaption","行先階",new Vector3(-.25f,1.78f,4.935f),new Vector2(.45f,.11f),.65f,Color.black);
            Notice(v,"CabinNotice","扉に注意\n<color=#292923><size=70%>指を挟まない\nように</size></color>",new Vector3(1.27f,1.75f,4.968f),new Vector2(.49f,.46f),.86f,new Color(.55f,.035f,.025f));
            foreach(var lamp in b.GetComponentsInChildren<Light>(true)) { lamp.intensity=2.2f; lamp.color=new Color(.94f,.96f,1); lamp.range=5; lamp.shadows=LightShadows.Soft; }
            var probeObject = Group(v,"CabinReflection").gameObject;
            var probe = probeObject.GetComponent<ReflectionProbe>();
            if (probe == null) probe = probeObject.AddComponent<ReflectionProbe>();
            probe.transform.position=new Vector3(0,1.6f,3.5f); probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.OnAwake; probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution=128; probe.size=new Vector3(3.5f,3.2f,3.4f); probe.boxProjection=true; probe.clearFlags=ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor=new Color(.22f,.23f,.23f);
        }

        private static void DressHall(Transform h, bool home)
        {
            foreach(string n in new[]{"LeftWall","RightWall","EndWall","Ceiling"}) Surface(h,n,plaster);
            Surface(h,"Floor",tile);
            var v=Group(h,"InteriorVisuals");
            float half=home?1.7f:3.5f, length=home?14:7, center=home?-5:-1.5f;
            for(int side=-1;side<=1;side+=2)
            {
                Box(v,"Skirting"+side,new Vector3(side*(half-.105f),.11f,center),new Vector3(.018f,.22f,length-.1f),ink);
                Box(v,"CeilingEdge"+side,new Vector3(side*(half-.11f),2.92f,center),new Vector3(.04f,.05f,length-.1f),paper);
            }
            // Mipmapped grout avoids the thin-geometry aliasing observed in the first pass.
            foreach(Transform child in v)
                if(child.name.StartsWith("FloorJoint", StringComparison.Ordinal)) child.gameObject.SetActive(false);
            int lamps=home?3:2;
            for(int j=0;j<lamps;j++)
            {
                float z=home?-1-j*4.5f:.3f-j*3.6f;
                Box(v,"LampHousing"+j,new Vector3(0,2.97f,z),new Vector3(.4f,.06f,1.28f),steel);
                Box(v,"LampDiffuser"+j,new Vector3(0,2.926f,z),new Vector3(.28f,.02f,1.15f),light);
                var lampObject=Group(v,"FixtureLight"+j).gameObject;
                var lamp=lampObject.GetComponent<Light>();
                if(lamp==null)lamp=lampObject.AddComponent<Light>();
                lamp.transform.SetPositionAndRotation(new Vector3(0,2.89f,z),Quaternion.Euler(90,0,0));
                lamp.type=LightType.Spot;lamp.spotAngle=140;lamp.innerSpotAngle=100;
                lamp.intensity=4;lamp.range=8;lamp.color=new Color(.96f,.97f,1);lamp.shadows=LightShadows.None;
            }
            foreach(Transform child in h)
            {
                var lamp=child.GetComponent<Light>();
                if(lamp!=null)lamp.enabled=false;
            }
            if(home)
            {
                Surface(h,"HomeDoor",ink);
                var door=h.Find("HomeDoor");
                var doorLabel=door.GetComponentInChildren<TMP_Text>(); doorLabel.text="自宅";doorLabel.fontSize=1.5f;
                for(int side=-1;side<=1;side+=2) Box(v,"HomeFrame"+side,new Vector3(side*.74f,1.215f,-11.77f),new Vector3(.065f,2.43f,.13f),steel);
                Box(v,"HomeKickPlate",new Vector3(0,.155f,-11.808f),new Vector3(1.4f,.31f,.02f),ink);
                Box(v,"HomeLintel",new Vector3(0,2.43f,-11.77f),new Vector3(1.55f,.065f,.13f),steel);
                Box(v,"HomeHandle",new Vector3(-.49f,1.08f,-11.70f),new Vector3(.035f,.19f,.075f),steel);
                Box(v,"HomeLetterSlot",new Vector3(0,.73f,-11.695f),new Vector3(.35f,.065f,.025f),steel);
                Box(v,"HomeLetterOpening",new Vector3(0,.733f,-11.678f),new Vector3(.29f,.018f,.012f),dark);
                Box(v,"HomeCloser",new Vector3(.40f,2.29f,-11.67f),new Vector3(.28f,.085f,.085f),steel);
                Screw(v,"HomePeephole",new Vector3(0,1.74f,-11.69f),Quaternion.Euler(0,180,0));
                for(int side=-1;side<=1;side+=2)
                for(int i=0;i<2;i++) CorridorDoor(v,side,-4.0f-i*4.2f);
                var floorText=h.Find("Floor8Notice").GetComponent<TMP_Text>();floorText.fontSize=2.5f;floorText.text="８階";
                floorText.transform.SetPositionAndRotation(new Vector3(1.565f,2.2f,-1),Quaternion.Euler(0,90,0));
                floorText.rectTransform.anchoredPosition3D=new Vector3(1.565f,2.2f,-1);
                Box(v,"FloorPlate",new Vector3(1.59f,2.2f,-1),new Vector3(.025f,.44f,.85f),ink);
            }
            else
            {
                Surface(h,"NoticeBoard",ink);
                var board=h.Find("NoticeBoard");
                board.position=new Vector3(-2.55f,1.9f,1.84f);
                board.localScale=new Vector3(1.5f/2.3f,1,1);
                var notice=h.Find("Notice").GetComponent<TMP_Text>();
                notice.transform.position=new Vector3(-2.55f,1.9f,1.79f);
                notice.rectTransform.sizeDelta=new Vector2(1.42f,1.8f);
                notice.text="<size=120%>おかえりなさい</size>\n\n自宅は８階です\n呼びボタンで\n扉を開けてください";
                notice.fontSize=1.1f;notice.color=new Color(.92f,.92f,.85f);
                Notice(v,"MaintenanceNotice","お知らせ\n<size=62%>共用部は静かに\nご利用ください\n設備の不具合は管理室まで</size>",new Vector3(2.43f,1.78f,1.875f),new Vector2(1.12f,.8f),1.2f,Color.black);
                Box(v,"TapeLeft",new Vector3(2.0f,2.17f,1.85f),new Vector3(.15f,.10f,.008f),paper);
                Box(v,"TapeRight",new Vector3(2.85f,2.17f,1.85f),new Vector3(.15f,.10f,.008f),paper);
            }
        }

        private static void CorridorDoor(Transform parent,int side,float z)
        {
            // Pure wall dressing, with no collider, script, or new route through the wall.
            var root=Group(parent,"Residence"+side+"_"+z.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
            root.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            Box(root,"Leaf",new Vector3(0,1.10f,0),new Vector3(.92f,2.16f,.032f),ink);
            for(int edge=-1;edge<=1;edge+=2)
                Box(root,"Frame"+edge,new Vector3(edge*.49f,1.10f,-.02f),new Vector3(.045f,2.20f,.055f),steel);
            Box(root,"Lintel",new Vector3(0,2.20f,-.02f),new Vector3(1.02f,.045f,.055f),steel);
            Box(root,"Threshold",new Vector3(0,.015f,-.025f),new Vector3(.95f,.025f,.065f),steel);
            Box(root,"Handle",new Vector3(.32f,1.06f,-.045f),new Vector3(.035f,.17f,.055f),steel);
            Box(root,"MailSlot",new Vector3(0,.65f,-.026f),new Vector3(.30f,.045f,.02f),steel);
            Box(root,"Closer",new Vector3(-.25f,2.10f,-.04f),new Vector3(.25f,.075f,.04f),steel);
            Screw(root,"Peephole",new Vector3(0,1.70f,-.026f),Quaternion.identity);
            root.SetPositionAndRotation(new Vector3(side*1.578f,0,z),Quaternion.Euler(0,side*90,0));
        }

        private static void Screw(Transform parent,string name,Vector3 position,Quaternion rotation)
        {
            var t=parent.Find(name);
            if(t==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                t=go.transform;t.SetParent(parent,false);
            }
            t.SetPositionAndRotation(position,rotation*Quaternion.Euler(90,0,0));
            t.localScale=new Vector3(.022f,.004f,.022f);
            t.GetComponent<Renderer>().sharedMaterial=steel;
        }

        private static Mesh PerforatedSheet()
        {
            const string path=Folder+"/PerforatedCeiling.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            // One shared mesh: square cells surrounding real round openings, facing downward.
            var vertices=new System.Collections.Generic.List<Vector3>();
            var triangles=new System.Collections.Generic.List<int>();
            const int columns=15,rows=3,segments=16;
            const float width=2.65f,depth=.49f,radius=.043f;
            for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
            {
                var center=new Vector3((x+.5f)*width/columns-width/2,0,(z+.5f)*depth/rows-depth/2);
                int start=vertices.Count;
                for(int k=0;k<segments;k++)
                {
                    float a=k*Mathf.PI*2/segments,dx=Mathf.Cos(a),dz=Mathf.Sin(a);
                    float scale=1/Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dz));
                    vertices.Add(center+new Vector3(dx*radius,0,dz*radius));
                    vertices.Add(center+new Vector3(dx*scale*width/columns/2,0,dz*scale*depth/rows/2));
                }
                for(int k=0;k<segments;k++)
                {
                    int a=start+k*2,b=start+((k+1)%segments)*2;
                    triangles.Add(a);triangles.Add(a+1);triangles.Add(b+1);
                    triangles.Add(a);triangles.Add(b+1);triangles.Add(b);
                }
            }
            bool create=mesh==null;
            if(create)mesh=new Mesh {name="Perforated ceiling sheet"};
            else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(create)AssetDatabase.CreateAsset(mesh,path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        internal static Transform Group(Transform parent,string name)
        {
            var t=parent.Find(name); if(t!=null)return t;
            t=new GameObject(name).transform;t.SetParent(parent,false);return t;
        }
        internal static GameObject Box(Transform p,string n,Vector3 pos,Vector3 size,Material m)
        {
            var t=p.Find(n);GameObject go;
            if(t==null){go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(p,false);UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}
            else go=t.gameObject;
            go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;return go;
        }
        private static void Surface(Transform p,string name,Material m)
        {
            var t=p.Find(name);if(t!=null)t.GetComponent<Renderer>().sharedMaterial=m;
        }
        internal static TMP_Text Label(Transform p,string n,string content,Vector3 position,Vector2 bounds,float size,Color color)
        {
            var t=Group(p,n);var text=t.GetComponent<TextMeshPro>();
            if(text==null) text=t.gameObject.AddComponent<TextMeshPro>();
            text.transform.position=position;text.font=font;text.text=content;text.fontSize=size;text.color=color;
            text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=bounds;text.textWrappingMode=TextWrappingModes.Normal;
            return text;
        }
        private static void Notice(Transform p,string n,string content,Vector3 pos,Vector2 bounds,float size,Color color)
        {
            Box(p,n+"Paper",pos,new Vector3(bounds.x,bounds.y,.012f),paper);
            Label(p,n,content,pos+Vector3.back*.008f,bounds*.91f,size,color);
        }
        private static Material Mat(string name,Color color,float metallic,float smooth,Texture texture=null,float repeat=1)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
            m.SetTexture("_BaseMap",texture);m.SetTextureScale("_BaseMap",Vector2.one*repeat);
            EditorUtility.SetDirty(m);return m;
        }
    }
}
