using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GraduationProject.EditorTools
{
    // Dimensions are in metres. References and deliberate gameplay departures are in VISUAL_REFERENCES.md.
    internal static class ElevatorRealismPass
    {
        private static Material steel, dark, paper, red;
        private static Transform building;
        private static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/ApartmentVisuals/" + name + ".mat");

        internal static void Apply(Transform b, Transform hall, GameObject[] roots)
        {
            building = b;
            steel = Material("SatinSteel"); dark = Material("Charcoal");
            paper = Material("NoticePaper"); red = Material("WarningRed");
            var old = b.Find("InteriorVisuals");
            foreach (Transform child in old)
                if (child.name.StartsWith("Panel") || child.name.EndsWith("Bezel") || child.name == "DisplayBacking"
                    || child.name == "CapacityPlate" || child.name == "CabinCapacity" || child.name.StartsWith("CabinNotice"))
                    child.gameObject.SetActive(false);
            var v = ApartmentVisualPass.Group(b, "RealismDetails");
            var call=b.Find("Call");
            call.localScale=new Vector3(.08f/.35f,.08f/.3f,.018f/.08f);
            call.position=new Vector3(1.25f,1.5f,1.82f);
            call.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
            ApartmentVisualPass.Box(v,"CallPlate",new Vector3(1.25f,1.55f,1.844f),new Vector3(.15f,.34f,.022f),steel);
            ApartmentVisualPass.Box(v,"CallRim",new Vector3(1.25f,1.5f,1.833f),new Vector3(.092f,.092f,.014f),steel);
            Text(v,"CallLegend","△",1.25f,1.5f,1.809f,.075f,.075f,.47f,new Color(.96f,.96f,.9f));
            Text(v,"CallCaption","呼 出",1.25f,1.655f,1.831f,.13f,.038f,.24f,Color.black);
            var callSettings=new SerializedObject(call.GetComponent<Interactable>());
            callSettings.FindProperty("hoverColor").colorValue=new Color(.12f,.24f,.17f);
            callSettings.FindProperty("hoverEmissionIntensity").floatValue=.25f;
            callSettings.ApplyModifiedPropertiesWithoutUndo();
            var main = Panel(v, "EntrancePanel", new Vector3(1.23f, 0, 2.135f), 180);
            Box(main, "SteelFace", 0, 1.35f, 0, .29f, 1.95f, .028f, steel);
            Box(main, "IndicatorGlass", 0, 2.16f, -.020f, .235f, .17f, .012f, dark);
            var mainDisplay = Text(main, "Display", "1", 0, 2.16f, -.028f, .21f, .15f, 1.05f, new Color(1,.43f,.16f));
            Text(main, "Capacity", "定員９名　６００kg", 0, 2.015f, -.019f, .26f, .07f, .23f, Color.black);
            for (int row=0; row<4; row++) for(int col=0; col<8; col++)
                Box(main, "Speaker"+row+"_"+col, -.07f+col*.020f, 1.90f+row*.014f, -.020f, .008f,.005f,.003f,dark);
            Button(main, "Emergency", "非常\n停止", 0, 1.77f, .09f, .065f, PlayerAction.PressEmergencyStop);
            for(int floor=1;floor<=10;floor++)
                Button(main,"Floor"+floor,floor.ToString(),floor%2==1?-.061f:.061f,1.11f+(floor-1)/2*.108f,.073f,.070f,PlayerAction.PressFloor,floor);
            Button(main,"Open","開",-.061f,.91f,.079f,.073f,PlayerAction.PressOpen);
            Button(main,"Close","閉",.061f,.91f,.079f,.073f,PlayerAction.PressClose);
            Text(main,"OpenCaption","ひらく",-.061f,.835f,-.02f,.11f,.035f,.23f,Color.black);
            Text(main,"CloseCaption","とじる",.061f,.835f,-.02f,.11f,.035f,.23f,Color.black);
            Box(main,"ServiceSeam",0,.62f,-.018f,.25f,.25f,.006f,dark);
            Box(main,"ServiceHatch",0,.62f,-.023f,.241f,.241f,.006f,steel);
            Box(main,"Keyhole",0,.70f,-.029f,.009f,.018f,.003f,dark);
            Text(main,"Service","保守点検用",0,.56f,-.030f,.20f,.045f,.24f,Color.black);
            Finish(main,new Vector3(1.23f,0,2.135f),180);

            var displays = new System.Collections.Generic.List<TMP_Text> {mainDisplay};
            for(int side=-1;side<=1;side+=2)
            {
                string prefix=side<0?"SideLeft":"SideRight";
                var p=Panel(v,prefix+"Panel",Vector3.zero,0);
                Box(p,"SteelFace",0,1.15f,0,1.08f,.34f,.034f,steel);
                for(int floor=1;floor<=10;floor++)
                    Button(p,prefix+"Floor"+floor,floor.ToString(),-.20f+((floor-1)%5)*.09f,1.10f+(floor>5?.11f:0),.070f,.067f,PlayerAction.PressFloor,floor);
                Button(p,prefix+"Open","開",.30f,1.10f,.075f,.075f,PlayerAction.PressOpen);
                Button(p,prefix+"Close","閉",.43f,1.10f,.075f,.075f,PlayerAction.PressClose);
                Button(p,prefix+"Emergency","非常\n停止",.37f,1.23f,.095f,.062f,PlayerAction.PressEmergencyStop);
                Box(p,"IndicatorGlass",-.405f,1.18f,-.021f,.16f,.12f,.010f,dark);
                displays.Add(Text(p,"Display","1",-.405f,1.18f,-.029f,.14f,.105f,.80f,new Color(1,.43f,.16f)));
                Text(p,"Caption","行先階",-.40f,1.066f,-.02f,.20f,.045f,.26f,Color.black);
                Finish(p,new Vector3(side*1.558f,0,3.35f),side*90);
            }
            // No housing can be supported by the moving door leaf. Use the fixed operation-panel displays.
            b.Find("FloorDisplay").gameObject.SetActive(false);
            var oldHeader=v.Find("HeaderDisplay");
            if(oldHeader!=null)oldHeader.gameObject.SetActive(false);
            var system=roots.Single(x=>x.name=="NormalJourney");
            var indicator=new SerializedObject(system.GetComponent<FloorIndicator>());
            indicator.FindProperty("floorText").objectReferenceValue=mainDisplay;
            var additional=indicator.FindProperty("additionalDisplays");additional.arraySize=displays.Count-1;
            for(int i=1;i<displays.Count;i++)additional.GetArrayElementAtIndex(i-1).objectReferenceValue=displays[i];
            indicator.ApplyModifiedPropertiesWithoutUndo();
            var information=MountedInformation(v,b.Find("FrontLeft").GetComponent<Renderer>().bounds.max.z);
            var machine=system.GetComponent<BeatStateMachine>();
            if(machine!=null)
            {
                var settings=new SerializedObject(machine);
                settings.FindProperty("informationDisplay").objectReferenceValue=information;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var elevator=system.GetComponent<ElevatorController>();
            if(!roots.Any(r=>r.GetComponent<ElevatorTuning>()!=null)) new GameObject("音・動作調整",typeof(ElevatorTuning));
            foreach(var button in b.GetComponentsInChildren<Interactable>(true).Where(i=>i.ActionType==PlayerAction.PressFloor && i.FloorNumber==8))
            {
                var lamp=button.GetComponent<DestinationButtonLamp>();
                if(lamp==null) lamp=button.gameObject.AddComponent<DestinationButtonLamp>();
                lamp.rim=button.transform.parent.Find(button.name+"Rim").GetComponent<Renderer>(); lamp.elevator=elevator;
            }
            SetFloat(elevator,"doorSlideSeconds",2.4f);
            SetFloat(elevator,"cameraShakeAmplitude",.003f);
            SetFloat(system.GetComponent<NormalJourneyController>(),"travelSeconds",24f);
            var sounds=new SerializedObject(elevator);
            sounds.FindProperty("travelLoopAudioSource").objectReferenceValue=Audio(v,"Drive","ElevatorDrive",.55f,true);
            sounds.FindProperty("doorAudioSource").objectReferenceValue=Audio(v,"DoorDrive","ElevatorDoor",.40f,true);
            sounds.FindProperty("arrivalAudioSource").objectReferenceValue=Audio(v,"Arrival","ElevatorArrival",.35f,false);
            sounds.ApplyModifiedPropertiesWithoutUndo();
            DressNotices(hall,v);
        }

        private static CabinInformationDisplay MountedInformation(Transform parent,float wallSurface)
        {
            var p=Panel(parent,"CabinInformation",Vector3.zero,0);
            Box(p,"Housing",0,0,0,.66f,.40f,.040f,steel);
            Box(p,"Gasket",0,-.014f,-.025f,.61f,.32f,.012f,dark);
            var screen=Material("InformationScreen");
            if(screen==null)
            {
                screen=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(screen,"Assets/ApartmentVisuals/InformationScreen.mat");
            }
            // The LCD has its own dark backlight; avoid screen-space occlusion streaks on its face.
            screen.SetColor("_BaseColor",new Color(.025f,.031f,.032f));
            EditorUtility.SetDirty(screen);
            Box(p,"Screen",0,-.014f,-.032f,.565f,.275f,.004f,screen);
            // Millimetre-thick front layers must not self-shadow into stripes. Housing still casts.
            foreach(var part in new[]{"Screen","Gasket"})
            {
                var renderer=p.Find(part).GetComponent<Renderer>();
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            Text(p,"Caption","ご案内",0,.166f,-.022f,.30f,.032f,.22f,Color.black);
            var content=Text(p,"Content","扉の開閉に\nご注意ください",0,-.014f,-.035f,.53f,.23f,.48f,new Color(.78f,.86f,.8f));
            content.enableAutoSizing=true;content.fontSizeMin=.30f;content.fontSizeMax=.48f;
            for(int side=-1;side<=1;side+=2)
            for(int row=-1;row<=1;row+=2)
            {
                Box(p,"Fastener"+side+row,side*.313f,row*.180f,-.022f,.010f,.010f,.004f,dark);
                Box(p,"Slot"+side+row,side*.313f,row*.180f,-.025f,.007f,.002f,.001f,steel);
            }
            // The rear face penetrates the actual wall by 2 mm, not a free-floating visual offset.
            Finish(p,new Vector3(-1.22f,2.12f,wallSurface+.018f),180);
            var display=p.GetComponent<CabinInformationDisplay>();
            if(display==null)display=p.gameObject.AddComponent<CabinInformationDisplay>();
            var so=new SerializedObject(display);so.FindProperty("content").objectReferenceValue=content;
            so.FindProperty("standbyMessage").stringValue="扉の開閉に\nご注意ください";
            so.ApplyModifiedPropertiesWithoutUndo();return display;
        }

        private static void DressNotices(Transform hall, Transform cabin)
        {
            hall.Find("Notice").gameObject.SetActive(false);
            hall.Find("NoticeBoard").gameObject.SetActive(false);
            var old=hall.Find("InteriorVisuals");
            foreach(Transform child in old)
                if(child.name.StartsWith("MaintenanceNotice") || child.name.StartsWith("Tape"))child.gameObject.SetActive(false);
            var v=hall.Find("掲示物_文章編集はこちら");
            if(v==null) v=ApartmentVisualPass.Group(hall,"RealismNotices");
            v.name="掲示物_文章編集はこちら";
            ApartmentVisualPass.Box(v,"BoardFrame",new Vector3(-2.48f,1.57f,1.84f),new Vector3(.80f,.68f,.035f),steel);
            ApartmentVisualPass.Box(v,"BoardFelt",new Vector3(-2.48f,1.57f,1.817f),new Vector3(.762f,.642f,.008f),dark);
            Sheet(v,"UseNotice","01_帰宅の手順_文章編集",new Vector3(-2.66f,1.56f,1.802f),"８階へお帰りの方へ",
                "呼びボタンで扉を開け、\n中に入って「８」を押してください。\n\n到着したら、降りる前に\n廊下の様子をお確かめください。\n\nいつもの廊下を通り、\n突き当たりのご自宅へ。\n\n見慣れない廊下の場合は\nかご内に留まり、「閉」を押して\n次の到着をお待ちください。\n\nマンション管理室");
            Sheet(v,"Inspection","02_困ったときの対処_文章編集",new Vector3(-2.30f,1.56f,1.802f),"慌てずお戻りください",
                "廊下へ出て違和感に気づいたら、\nすぐに身体をかご内へ戻し、\n「閉」を押してください。\n外から扉を閉めないでください。\n\n放送に誘われて押してしまっても、\n続けてボタンを押さず、\n静かになるまでお待ちください。\n\n階数と走行音が異常なときは\n「非常停止」で止めてください。\n\n扉の前に立ち止まらず、\n落ち着いて対応を。");
            Sheet(v,"EmergencyContacts","03_乗車前の三原則_文章編集",new Vector3(2.12f,1.68f,1.878f),"乗車前の注意事項",
                "一　降りる前に廊下を確認\n８階の表示だけで降りず、\n普段と違えば車内で「閉」。\n\n二　放送だけで停止しない\n停止を求める放送や呼び声は\n操作せず、静かに待つ。\n\n三　階数と走行音を確認\n８を越えて上がり続け、\n音も高く速くなったら\n「非常停止」で止める。\n\nマンション管理室");
            foreach (float x in new[]{-2.48f,2.12f})
            {
                var fixture=ApartmentVisualPass.Group(v,x<0?"掲示板照明":"注意書き照明");
                fixture.position=new Vector3(x,2.18f,1.79f);
                Box(fixture,"Housing",x,2.18f,1.79f,.38f,.045f,.18f,steel);
                Box(fixture,"Diffuser",x,2.155f,1.76f,.32f,.012f,.11f,Material("FluorescentDiffuser"));
                var lamp=fixture.GetComponent<Light>(); if(lamp==null)lamp=fixture.gameObject.AddComponent<Light>();
                lamp.type=LightType.Point;lamp.range=1.5f;lamp.intensity=.65f;
                lamp.color=new Color(1f,.96f,.85f);lamp.shadows=LightShadows.None;
            }
            var sticker=Panel(cabin,"DoorCaution",Vector3.zero,0);
            Box(sticker,"Paper",0,1.56f,0,.15f,.18f,.003f,paper);
            Text(sticker,"Heading","扉にご注意",0,1.613f,-.004f,.14f,.035f,.24f,new Color(.6f,.02f,.015f));
            Text(sticker,"Body","指を挟まない\nように",0,1.53f,-.004f,.14f,.08f,.20f,Color.black);
            Finish(sticker,new Vector3(-1.23f,0,2.14f),180);
        }

        private static void Sheet(Transform parent,string id,string name,Vector3 position,string heading,string body)
        {
            var authored=parent.GetComponentsInChildren<EditableNotice>(true).FirstOrDefault(n=>n.ContentId==id);
            var p=authored!=null?authored.transform:Panel(parent,id,Vector3.zero,0);
            p.SetPositionAndRotation(Vector3.zero,Quaternion.identity); p.localScale=Vector3.one;p.name=name;
            Box(p,"Paper",0,0,0,.297f,.420f,.002f,paper);
            var title=Text(p,"Heading","",0,.169f,-.0025f,.270f,.041f,.26f,Color.black);
            var text=Text(p,"Body","",0,-.027f,-.0025f,.263f,.329f,.16f,Color.black);
            text.alignment=TextAlignmentOptions.TopLeft;text.lineSpacing=0;
            for(int side=-1;side<=1;side+=2)
                Box(p,"Tape"+side,side*.127f,.204f,-.003f,.027f,.035f,.001f,paper);
            if(authored==null)authored=p.gameObject.AddComponent<EditableNotice>();
            authored.Bind(id,title,text,heading,body);
            EditorUtility.SetDirty(authored);
            Finish(p,position,0);
        }

        private static Transform Panel(Transform p,string name,Vector3 pos,float yaw)
        {
            var t=ApartmentVisualPass.Group(p,name);t.SetPositionAndRotation(Vector3.zero,Quaternion.identity);t.localScale=Vector3.one;return t;
        }
        private static void Finish(Transform p,Vector3 position,float yaw) => p.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        private static void Box(Transform p,string n,float x,float y,float z,float w,float h,float d,Material m)
            => ApartmentVisualPass.Box(p,n,new Vector3(x,y,z),new Vector3(w,h,d),m);
        private static TMP_Text Text(Transform p,string n,string value,float x,float y,float z,float w,float h,float size,Color color)
        {
            var t=ApartmentVisualPass.Label(p,n,value,new Vector3(x,y,z),new Vector2(w,h),size,color);
            t.transform.rotation=Quaternion.identity;t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.NoWrap;
            t.ForceMeshUpdate(true);
            // Fit authored copy inside its physical surface, including Japanese font ascender/descender metrics.
            for(int i=0;i<60 && (t.preferredWidth>w || t.preferredHeight>h);i++)
            {
                t.fontSize*=.97f;t.ForceMeshUpdate(true);
            }
            return t;
        }
        private static void Button(Transform p,string name,string caption,float x,float y,float width,float height,PlayerAction action,int floor=-1)
        {
            // Preserve existing object identities; the baked blockout mesh is scaled to measured button dimensions.
            var t=p.Find(name) ?? building.Find(name);
            if(t==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;t=go.transform;
                go.AddComponent<Interactable>();
            }
            t.SetParent(p,false);t.SetPositionAndRotation(new Vector3(x,y,-.036f),Quaternion.identity);
            var mesh=t.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            t.localScale=new Vector3(width/mesh.x,height/mesh.y,.018f/mesh.z);
            t.gameObject.layer=LayerMask.NameToLayer("Interactable");
            var renderer=t.GetComponent<Renderer>();renderer.sharedMaterial=action==PlayerAction.PressEmergencyStop?red:dark;
            var interaction=new SerializedObject(t.GetComponent<Interactable>());
            interaction.FindProperty("actionType").enumValueIndex=(int)action;
            interaction.FindProperty("floorNumber").intValue=floor;
            interaction.FindProperty("hoverColor").colorValue=new Color(.12f,.24f,.17f);
            interaction.FindProperty("hoverEmissionIntensity").floatValue=.25f;
            var renderers=interaction.FindProperty("targetRenderers");renderers.arraySize=1;renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            interaction.ApplyModifiedPropertiesWithoutUndo();
            foreach(var label in t.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
            Box(p,name+"Rim",x,y,-.025f,width+.010f,height+.010f,.013f,steel);
            Text(p,name+"Legend",caption,x,y,-.046f,width*.96f,height*.96f,action==PlayerAction.PressEmergencyStop?.255f:.48f,new Color(.96f,.96f,.90f));
        }
        private static AudioSource Audio(Transform p,string name,string clip,float volume,bool loop)
        {
            var t=ApartmentVisualPass.Group(p,name);t.position=new Vector3(0,2.7f,3.4f);
            var source=t.GetComponent<AudioSource>();
            if(source==null)source=t.gameObject.AddComponent<AudioSource>();
            source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/"+clip+".wav");
            if(source.clip==null)throw new System.InvalidOperationException("Missing audio: "+clip);
            source.playOnAwake=false;source.loop=loop;source.volume=volume;source.spatialBlend=.6f;source.minDistance=2;source.maxDistance=10;
            return source;
        }
        private static void SetFloat(Object target,string name,float value)
        {
            var so=new SerializedObject(target);so.FindProperty(name).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
