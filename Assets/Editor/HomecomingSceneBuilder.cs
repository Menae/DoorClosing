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
            var group = Group(parent, "集合郵便受け_54戸_201-1006");
            group.SetPositionAndRotation(new Vector3(-3.37f, 0, -7.4f), Quaternion.Euler(0, -90, 0));
            // Six 360 mm bays, nine 120 mm rows; body depth follows a real front-access postbox.
            Box(group, "箱体_幅2160高さ1080奥行274mm", new(0, 1.30f, -.137f), new(2.18f, 1.10f, .274f), dark, true);
            Box(group, "上端見切り", new(0, 1.857f, -.14f), new(2.20f, .024f, .29f), steel);
            Box(group, "下端見切り", new(0, .743f, -.14f), new(2.20f, .024f, .29f), steel);
            foreach (int side in new[] { -1, 1 })
                Box(group, "縦見切り" + side, new(side * 1.099f, 1.30f, -.14f), new(.018f, 1.09f, .29f), steel);
            Transform selected = null;
            for (int row = 0; row < 9; row++) for (int col = 0; col < 6; col++)
            {
                string number = ((10 - row) * 100 + col + 1).ToString();
                float x = (col - 2.5f) * .36f, y = 1.78f - row * .12f;
                var hinge = Group(group, number + "_扉ヒンジ"); hinge.localPosition = new Vector3(x - .176f, y, -.279f);
                var leaf = Box(hinge, number + "_郵便受け", new(.176f, 0, 0), new(.352f, .112f, .012f), steel, true);
                Box(hinge, "投函口", new(.167f, .030f, -.008f), new(.279f, .013f, .006f), dark);
                Box(hinge, "投入口の返し", new(.167f, .022f, -.011f), new(.279f, .004f, .012f), steel);
                Box(hinge, "番号プレート", new(.071f, -.021f, -.008f), new(.078f, .033f, .004f), dark);
                Label(hinge, "部屋番号_" + number, number, new(.071f, -.021f, -.012f), new(.072f, .029f), .20f);
                var dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dial.name = "ダイヤル錠"; dial.transform.SetParent(hinge, false);
                dial.transform.localPosition = new Vector3(.321f, -.018f, -.012f);
                dial.transform.localRotation = Quaternion.Euler(90, 0, 0); dial.transform.localScale = new Vector3(.027f, .006f, .027f);
                dial.GetComponent<Renderer>().sharedMaterial = dark; UnityEngine.Object.DestroyImmediate(dial.GetComponent<Collider>());
                Box(hinge, "錠の指標", new(.321f, -.008f, -.019f), new(.002f, .005f, .002f), steel);
                if (number == "805")
                {
                    selected = hinge;
                    AddControl(leaf.gameObject, system, EntranceControl.Kind.Mailbox, "");
                    Box(group, "805内部", new(x, y, -.276f), new(.348f, .108f, .004f), dark);
                    Label(group, "805内部番号", "805", new(x, y, -.280f), new(.12f, .05f), .27f);
                }
            }
            FinishMailboxSurface(group);
            return selected;
        }

        private static void FinishMailboxSurface(Transform group)
        {
            const string path = "Assets/ApartmentVisuals/MailboxSteel.mat";
            var finish = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (finish == null)
            {
                finish = new Material(Mat("DoorSteel")) { name = "MailboxSteel" };
                finish.SetColor("_BaseColor", new Color(.84f, .85f, .82f));
                finish.SetFloat("_Metallic", .42f); finish.SetFloat("_Smoothness", .4f);
                finish.SetFloat("_Wear", .35f); finish.SetFloat("_WorldScale", 1.1f);
                AssetDatabase.CreateAsset(finish, path);
            }
            foreach (var renderer in group.GetComponentsInChildren<Renderer>())
                if (renderer.sharedMaterial == Mat("SatinSteel")) renderer.sharedMaterial = finish;
            if (group.Find("壁面固定レール-1") == null)
                foreach (int side in new[] { -1, 1 })
                    Box(group, "壁面固定レール" + side, new(side * .80f, 1.30f, .01f), new(.06f, 1.08f, .06f), finish);
            if (group.Find("ポスト照明") == null)
            {
                var fixture = Group(group, "ポスト照明"); fixture.localPosition = new Vector3(0, 2.08f, -.19f);
                Box(fixture, "壁付け照明枠", Vector3.zero, new(1.80f, .065f, .30f), Mat("SatinSteel"));
                Box(fixture, "乳白カバー", new(0, -.035f, -.018f), new(1.65f, .01f, .22f), Mat("FluorescentDiffuser"));
                for (int i = 0; i < 2; i++)
                {
                    var light = Group(fixture, "下向き照明" + i).gameObject.AddComponent<Light>();
                    light.transform.localPosition = new Vector3(i == 0 ? -.55f : .55f, -.075f, -.18f);
                    light.transform.rotation = Quaternion.LookRotation(group.TransformDirection(new Vector3(0, -1, .10f)));
                    light.type = LightType.Spot; light.spotAngle = 120; light.innerSpotAngle = 80;
                    light.range = 2.3f; light.intensity = 2.0f; light.color = new Color(.95f, .96f, .88f);
                    light.shadows = LightShadows.None;
                }
            }
            var mountedFixture = group.Find("ポスト照明");
            if (mountedFixture.Find("壁面ブラケット-1") == null)
                foreach (int side in new[] { -1, 1 })
                    Box(mountedFixture, "壁面ブラケット" + side, new(side * .76f, 0, .19f), new(.04f, .055f, .10f), finish);
        }

        [MenuItem("Tools/Unity Agent/Polish Homecoming Mailbox Surface")]
        public static void PolishMailboxSurface()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires saved Homecoming in Edit Mode.");
            var bank = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>())
                .Single(t => t.name == "集合郵便受け_54戸_201-1006");
            FinishMailboxSurface(bank);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Unity Agent/Upgrade Homecoming Mailboxes to 54 Units")]
        public static void UpgradeMailboxes()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Requires saved Homecoming in idle Edit Mode.");
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var old = all.SingleOrDefault(t => t.name == "集合郵便受け_801-809");
            if (old == null) throw new InvalidOperationException("Original bank not found; do not overwrite an already upgraded bank.");
            var parent = old.parent;
            copy = all.Single(t => t.name == "09_入口・ポスト・オートロック").GetComponent<GameTextCollection>();
            // Keep user-authored values by key; retarget surviving room labels to their new objects.
            var retiredEntries = copy.entries.Where(e => e.target != null && e.target.transform.IsChildOf(old)).ToArray();
            var system = parent.GetComponentInChildren<EntranceAccessController>();
            steel = Mat("SatinSteel"); dark = Mat("Charcoal");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
            Undo.RegisterFullObjectHierarchyUndo(parent.gameObject, "Upgrade mailbox bank");
            old.gameObject.SetActive(false); old.name = "旧ポスト_更新前_非表示";
            // Preserve the original geometry and labels in-scene as an inactive author backup.
            Set(system, "mailboxLid", Mailboxes(parent, system));
            var markers = all.Select(t => t.GetComponent<NearbyInteractionMarkers>()).First(m => m != null);
            var markerSettings = new SerializedObject(markers);
            markerSettings.FindProperty("size").floatValue = 18;
            markerSettings.ApplyModifiedProperties();
            foreach (var entry in retiredEntries)
                if (entry.target != null && entry.target.transform.IsChildOf(old)) entry.label += "（旧配置・非表示）";
            copy.Apply(); EditorUtility.SetDirty(copy);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = parent.gameObject;
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
