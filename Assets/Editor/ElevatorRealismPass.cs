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
            // The original high display remains the single controller reference, now over the entrance.
            var floorText=b.Find("FloorDisplay").GetComponent<TMP_Text>();
            floorText.transform.SetPositionAndRotation(new Vector3(0,2.64f,2.15f),Quaternion.Euler(0,180,0));
            floorText.rectTransform.anchoredPosition3D=floorText.transform.localPosition;
            floorText.rectTransform.sizeDelta=new Vector2(.40f,.21f); floorText.fontSize=1.35f;
            floorText.color=new Color(1,.43f,.16f);
            ApartmentVisualPass.Box(v,"HeaderDisplay",new Vector3(0,2.64f,2.125f),new Vector3(.46f,.25f,.026f),dark);
            var system=roots.Single(x=>x.name=="NormalJourney");
            var indicator=new SerializedObject(system.GetComponent<FloorIndicator>());
            var additional=indicator.FindProperty("additionalDisplays");additional.arraySize=displays.Count;
            for(int i=0;i<displays.Count;i++)additional.GetArrayElementAtIndex(i).objectReferenceValue=displays[i];
            indicator.ApplyModifiedPropertiesWithoutUndo();
            var elevator=system.GetComponent<ElevatorController>();
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

        private static void DressNotices(Transform hall, Transform cabin)
        {
            hall.Find("Notice").gameObject.SetActive(false);
            hall.Find("NoticeBoard").gameObject.SetActive(false);
            var old=hall.Find("InteriorVisuals");
            foreach(Transform child in old)
                if(child.name.StartsWith("MaintenanceNotice") || child.name.StartsWith("Tape"))child.gameObject.SetActive(false);
            var v=ApartmentVisualPass.Group(hall,"RealismNotices");
            ApartmentVisualPass.Box(v,"BoardFrame",new Vector3(-2.48f,1.57f,1.84f),new Vector3(.80f,.68f,.035f),steel);
            ApartmentVisualPass.Box(v,"BoardFelt",new Vector3(-2.48f,1.57f,1.817f),new Vector3(.762f,.642f,.008f),dark);
            Sheet(v,"UseNotice",new Vector3(-2.66f,1.56f,1.802f),"エレベーターご利用案内",
                "ご入居の皆さまへ\n\n扉の開閉時は、手やお荷物を\n挟まれないようご注意ください。\n\nかご内では静かにご利用ください。\nお子さまには大人の方が\n付き添ってください。\n\n設備の異常にお気づきの際は\n管理室までお知らせください。\n\nマンション管理室");
            Sheet(v,"Inspection",new Vector3(-2.30f,1.56f,1.802f),"定期点検のお知らせ",
                "入居者各位\n\n共用設備の安全点検を行います。\n作業中は係員の案内に従って\nご通行をお願いいたします。\n\n【点検対象】\n照明・防災設備・エレベーター\n\nご不便をおかけいたしますが\nご理解とご協力をお願いいたします。\n\nマンション管理室");
            Sheet(v,"EmergencyContacts",new Vector3(2.30f,1.56f,1.878f),"緊急時の連絡先",
                "【設備の故障・トラブル発生時】\n\n給排水設備・共用照明・エレベーターの\n不具合は管理室へお知らせください。\n\n管理窓口　マンション管理室\n受付時間　９：００〜１７：００\n\n【エレベーター内の異常】\n\n扉を無理に開けず、落ち着いて\n非常停止ボタンを押してください。\n\n【ガス・電気のトラブル】\n\n入居時にお渡ししたご案内の\n各事業者窓口へご連絡ください。");
            var sticker=Panel(cabin,"DoorCaution",Vector3.zero,0);
            Box(sticker,"Paper",0,1.56f,0,.15f,.18f,.003f,paper);
            Text(sticker,"Heading","扉にご注意",0,1.613f,-.004f,.14f,.035f,.24f,new Color(.6f,.02f,.015f));
            Text(sticker,"Body","指を挟まない\nように",0,1.53f,-.004f,.14f,.08f,.20f,Color.black);
            Finish(sticker,new Vector3(-1.23f,0,2.14f),180);
        }

        private static void Sheet(Transform parent,string name,Vector3 position,string heading,string body)
        {
            var p=Panel(parent,name,Vector3.zero,0);
            Box(p,"Paper",0,0,0,.210f,.297f,.002f,paper);
            Text(p,"Heading","<u><b>"+heading+"</b></u>",0,.105f,-.002f,.185f,.035f,.20f,Color.black);
            var text=Text(p,"Body",body,0,-.023f,-.002f,.180f,.212f,.078f,Color.black);
            text.alignment=TextAlignmentOptions.TopLeft;text.lineSpacing=0;
            for(int side=-1;side<=1;side+=2)
                Box(p,"Tape"+side,side*.087f,.144f,-.003f,.027f,.035f,.001f,paper);
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
