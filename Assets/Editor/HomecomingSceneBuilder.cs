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
    public static class HomecomingSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Homecoming.unity";
        private static Material steel, dark, plaster, floor, stone, glass, diffuser;
        private static TMP_FontAsset font;
        private static GameTextCollection copy;

        [MenuItem("Tools/Unity Agent/Create Homecoming Opening")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Requires idle Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scenes first.");
            if (System.IO.File.Exists(ScenePath)) throw new InvalidOperationException("Homecoming exists; edit it without overwriting author changes.");
            var source = EditorSceneManager.OpenScene(PlayableDemoBuilder.ScenePath);
            if (!EditorSceneManager.SaveScene(source, ScenePath, true)) throw new InvalidOperationException("Could not copy demo.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var roots = scene.GetRootGameObjects();
            var player = roots.Single(g => g.name == "Player");
            var hall = roots.Single(g => g.name == "EntranceHall").transform;
            var owner = roots.SelectMany(r => r.GetComponentsInChildren<DemoSession>()).Single();
            var journey = roots.SelectMany(r => r.GetComponentsInChildren<NormalJourneyController>()).Single();
            owner.name = "本編_入口と夜の進行";
            var opening = owner.gameObject.AddComponent<HomecomingPresentation>();
            var followingStart = new GameObject("翌夜・死亡再開位置_ホール").transform;
            followingStart.SetParent(owner.transform, false);
            followingStart.SetPositionAndRotation(player.transform.position, player.transform.rotation);
            Set(opening, "followingNightStart", followingStart);
            Set(owner, "opening", opening); Set(journey, "showCompletionText", false);
            player.transform.SetPositionAndRotation(new Vector3(-1.15f, .95f, -9.2f), Quaternion.Euler(0, -12, 0));
            var markers = player.AddComponent<NearbyInteractionMarkers>();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
            Set(markers, "view", player.GetComponentInChildren<Camera>()); Set(markers, "font", font);
            var textRoot = roots.Single(g => g.GetComponent<GameTextCollection>() != null);
            var copyObject = new GameObject("09_入口・ポスト・オートロック", typeof(GameTextCollection));
            copyObject.transform.SetParent(textRoot.transform, false); copy = copyObject.GetComponent<GameTextCollection>();
            // Preserve all authored copy, including the hidden demo introduction, for later editing.
            foreach (var collection in textRoot.GetComponentsInChildren<GameTextCollection>())
                foreach (var entry in collection.entries)
                    if (entry.key == "menu.ShowTitle.1") entry.value = "帰宅";

            steel = Mat("SatinSteel"); dark = Mat("Charcoal"); plaster = Mat("WarmPlaster");
            floor = Mat("StoneFloor"); stone = Mat("GreenStone"); diffuser = Mat("FluorescentDiffuser");
            glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            glass.name = "EntranceGlass";
            glass.SetColor("_BaseColor", new Color(.34f, .43f, .40f, .24f));
            glass.SetFloat("_Surface", 1); glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); glass.SetFloat("_ZWrite", 0);
            glass.SetFloat("_Smoothness", .75f); glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.SetOverrideTag("RenderType", "Transparent"); glass.renderQueue = 3000;
            AssetDatabase.CreateAsset(glass, "Assets/ApartmentVisuals/EntranceGlass.mat");

            hall.Find("EndWall").gameObject.SetActive(false);
            var vestibule = Group(hall, "入口_集合ポストとオートロック");
            Box(vestibule, "床", new(0, -.1f, -7.55f), new(7, .2f, 5.1f), floor, true);
            Box(vestibule, "天井", new(0, 3.1f, -7.55f), new(7, .2f, 5.1f), plaster, true);
            Box(vestibule, "左壁", new(-3.5f, 1.5f, -7.55f), new(.2f, 3, 5.1f), plaster, true);
            Box(vestibule, "右壁", new(3.5f, 1.5f, -7.55f), new(.2f, 3, 5.1f), plaster, true);
            Box(vestibule, "入口背面", new(0, 1.5f, -10.1f), new(7, 3, .2f), stone, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(vestibule, "巾木" + side, new(side * 3.39f, .1f, -7.5f), new(.03f, .2f, 5), stone);
                Box(vestibule, "オートロック袖壁" + side, new(side * 2.29f, 1.5f, -5), new(2.42f, 3, .16f), stone, true);
            }
            Box(vestibule, "自動ドア上枠", new(0, 2.47f, -5), new(2.25f, .18f, .22f), steel, true);
            Box(vestibule, "上部欄間", new(0, 2.78f, -5), new(2.2f, .48f, .14f), glass, true);
            Box(vestibule, "敷居", new(0, .009f, -5), new(2.28f, .018f, .24f), steel);
            var left = Door(vestibule, "自動扉_左", -.55f);
            var right = Door(vestibule, "自動扉_右", .55f);
            var system = Group(vestibule, "入口操作_805を調べて入力").gameObject.AddComponent<EntranceAccessController>();
            Set(system, "leftDoor", left); Set(system, "rightDoor", right);
            Set(system, "display", Keypad(vestibule, system));
            Set(system, "mailboxLid", Mailboxes(vestibule, system));
            Set(system, "doorSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/ElevatorDoor.wav"));
            for (int i = 0; i < 2; i++)
            {
                float z = -6.15f - i * 2.45f;
                Box(vestibule, "照明枠" + i, new(0, 2.95f, z), new(.4f, .08f, 1.1f), steel);
                Box(vestibule, "乳白カバー" + i, new(0, 2.90f, z), new(.29f, .02f, 1), diffuser);
                var lamp = Group(vestibule, "入口照明" + i).gameObject.AddComponent<Light>();
                lamp.transform.SetPositionAndRotation(new Vector3(0, 2.85f, z), Quaternion.Euler(90, 0, 0));
                lamp.type = LightType.Spot; lamp.spotAngle = 135; lamp.innerSpotAngle = 100; lamp.range = 7;
                lamp.intensity = 3.5f; lamp.color = new Color(.96f, .98f, 1); lamp.shadows = LightShadows.Soft;
                lamp.shadowBias = .025f; lamp.shadowNormalBias = .15f; lamp.shadowCustomResolution = 512;
            }
            copy.Apply();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); Selection.activeGameObject = system.gameObject;
        }

        private static Transform Mailboxes(Transform parent, EntranceAccessController system)
        {
            var group = Group(parent, "集合郵便受け_801-809");
            group.SetPositionAndRotation(new Vector3(-3.34f, 0, -7.15f), Quaternion.Euler(0, -90, 0));
            Box(group, "集合ポスト背板", new(0, 1.35f, 0), new(1.55f, 1.23f, .08f), dark, true);
            Transform selected = null;
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
            {
                string number = (801 + row * 3 + col).ToString();
                float x = (col - 1) * .5f, y = 1.75f - row * .40f;
                var hinge = Group(group, number + "_扉ヒンジ"); hinge.localPosition = new Vector3(x - .235f, y, -.051f);
                var leaf = Box(hinge, number + "_郵便受け", new(.235f, 0, -.012f), new(.47f, .365f, .025f), steel, true);
                Box(hinge, "投函口", new(.235f, .10f, -.029f), new(.32f, .021f, .012f), dark);
                Box(hinge, "番号プレート", new(.235f, -.012f, -.032f), new(.15f, .066f, .014f), dark);
                Label(hinge, "部屋番号_" + number, number, new(.235f, -.012f, -.042f), new(.13f, .05f), .32f);
                Box(hinge, "錠", new(.40f, -.105f, -.031f), new(.04f, .04f, .012f), dark);
                if (number == "805")
                {
                    selected = hinge;
                    AddControl(leaf.gameObject, system, EntranceControl.Kind.Mailbox, "");
                    Box(group, "805内部", new(x, y, -.026f), new(.45f, .35f, .014f), dark);
                    Label(group, "805内部番号", "805", new(x, y, -.036f), new(.25f, .10f), .5f);
                }
            }
            return selected;
        }

        private static TMP_Text Keypad(Transform parent, EntranceAccessController system)
        {
            var group = Group(parent, "オートロック操作盤"); group.localPosition = new Vector3(1.48f, 1.36f, -5.14f);
            Box(group, "パネル", Vector3.zero, new(.41f, .88f, .065f), steel, true);
            Box(group, "表示窓", new(0, .27f, -.037f), new(.30f, .12f, .015f), dark);
            var display = Label(group, "入力番号", "---", new(0, .27f, -.049f), new(.27f, .10f), .63f, false);
            for (int n = 1; n <= 9; n++)
                Key(group, system, n.ToString(), (n - 1) % 3 - 1, (n - 1) / 3);
            Key(group, system, "0", 0, 3); Key(group, system, "C", 1, 3);
            for (int i = 0; i < 4; i++)
                Box(group, "スピーカー孔" + i, new(-.10f + i * .066f, .385f, -.035f), new(.035f, .006f, .01f), dark);
            return display;
        }
        private static void Key(Transform parent, EntranceAccessController system, string symbol, int col, int row)
        {
            Vector3 p = new(col * .105f, .10f - row * .11f, -.046f);
            var key = Box(parent, "EntranceKey_" + symbol, p, new(.084f, .080f, .025f), dark, true);
            Label(parent, "キー表記_" + symbol, symbol == "C" ? "取消" : symbol, p + new Vector3(0, 0, -.015f), new(.082f, .07f), symbol == "C" ? .28f : .45f);
            AddControl(key.gameObject, system, EntranceControl.Kind.Key, symbol);
        }
        private static void AddControl(GameObject go, EntranceAccessController system, EntranceControl.Kind kind, string symbol)
        {
            go.layer = 6;
            var target = go.AddComponent<Interactable>();
            var s = new SerializedObject(target);
            s.FindProperty("hoverColor").colorValue = new Color(.36f, .41f, .37f);
            s.FindProperty("hoverEmissionIntensity").floatValue = .05f; s.ApplyModifiedPropertiesWithoutUndo();
            var control = go.AddComponent<EntranceControl>();
            Set(control, "entrance", system); Set(control, "kind", (int)kind); Set(control, "symbol", symbol);
        }
        private static Transform Door(Transform parent, string name, float x)
        {
            var group = Group(parent, name); group.localPosition = new Vector3(x, 0, -5);
            Box(group, "ガラス", new(0, 1.20f, 0), new(1.055f, 2.36f, .035f), glass, true);
            foreach (int side in new[] { -1, 1 }) Box(group, "縦框" + side, new(side * .52f, 1.20f, 0), new(.044f, 2.4f, .09f), steel, true);
            Box(group, "上框", new(0, 2.39f, 0), new(1.08f, .05f, .09f), steel, true);
            Box(group, "下框", new(0, .09f, 0), new(1.08f, .16f, .09f), steel, true);
            Box(group, "衝突防止帯", new(0, 1.11f, -.026f), new(1.02f, .042f, .007f), steel);
            return group;
        }
        private static TMP_Text Label(Transform parent, string name, string value, Vector3 p, Vector2 size, float fontSize, bool authored = true)
        {
            var text = new GameObject(name, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(parent, false); text.transform.localPosition = p;
            text.font = font; text.fontSize = fontSize; text.text = value; text.color = new Color(.9f, .93f, .88f);
            text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (authored) copy.Add("entrance." + name, name, value, text);
            return text;
        }
        private static Transform Group(Transform parent, string name)
        {
            var result = new GameObject(name).transform; result.SetParent(parent, false); return result;
        }
        private static Transform Box(Transform parent, string name, Vector3 p, Vector3 size, Material material, bool collide = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collide) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }
        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/ApartmentVisuals/" + name + ".mat");
        private static void Set(UnityEngine.Object owner, string field, object value)
        {
            var so = new SerializedObject(owner); var p = so.FindProperty(field);
            if (value is bool b) p.boolValue = b;
            else if (value is int n) p.intValue = n;
            else if (value is string text) p.stringValue = text;
            else p.objectReferenceValue = value as UnityEngine.Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
