using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    // Additive one-time install. Rerunning cannot replace the author's settings or copy.
    public static class HomecomingAtmosphereBuilder
    {
        [MenuItem("Tools/Unity Agent/Add Homecoming Spatial Pass")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != HomecomingSceneBuilder.ScenePath || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires saved Homecoming in Edit Mode.");
            if (scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<HallwayLoop>(true) != null))
                throw new InvalidOperationException("Already installed. Edit the existing scene instead.");
            var roots = scene.GetRootGameObjects();
            var hall = roots.Single(r => r.name == "EntranceHall").transform;
            var vestibule = hall.Find("入口_集合ポストとオートロック");
            var player = roots.Single(r => r.name == "Player").GetComponent<CharacterController>();
            var text = new GameObject("12_退出・階段の案内", typeof(GameTextCollection));
            text.transform.SetParent(roots.Single(r => r.name == "テキスト編集").transform, false);
            var copy = text.GetComponent<GameTextCollection>();

            // Original wall objects remain available for author inspection / reversibility.
            vestibule.Find("入口背面").gameObject.SetActive(false);
            Opening(vestibule, "退出口", new Vector3(0, 0, -10.1f), Quaternion.Euler(0, 180, 0), 7, Mat("GreenStone"));
            Passage(vestibule, "退出通路_同じ入口へ", new Vector3(0, 0, -10.1f), Quaternion.Euler(0, 180, 0), player, "出口", copy, "exit");
            hall.Find("RightWall").gameObject.SetActive(false);
            // Existing side wall spans z=-5..2. A 1.8 m access opening is centred at -2.8.
            Box(hall, "階段側壁_手前", new Vector3(3.5f, 1.5f, .05f), new Vector3(.2f, 3, 3.9f), Mat("WarmPlaster"), true);
            Box(hall, "階段側壁_奥", new Vector3(3.5f, 1.5f, -4.35f), new Vector3(.2f, 3, 1.3f), Mat("WarmPlaster"), true);
            Box(hall, "階段側壁_開口上", new Vector3(3.5f, 2.8f, -2.8f), new Vector3(.2f, .4f, 1.8f), Mat("WarmPlaster"), true);
            Passage(hall, "階段通路_同じホールへ", new Vector3(3.5f, 0, -2.8f), Quaternion.Euler(0, 90, 0), player, "階段", copy, "stairs");
            // The old skirting must not run across the new passage entrance.
            foreach (var renderer in hall.Find("InteriorVisuals").GetComponentsInChildren<Renderer>(true))
                if (renderer.bounds.center.x > 3.3f && renderer.bounds.size.z > 5 && renderer.bounds.center.y < .3f)
                    renderer.gameObject.SetActive(false);
            RoomTone(roots, player.transform);
            copy.Apply(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = text;
        }

        private static void Passage(Transform parent, string name, Vector3 position, Quaternion rotation,
            CharacterController player, string caption, GameTextCollection copy, string key)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.SetPositionAndRotation(position, rotation);
            var plaster = Mat("WarmPlaster"); var floor = Mat("StoneFloor"); var steel = Mat("SatinSteel");
            // S-shaped access passage. Its two halves are exact 180-degree copies about
            // (2,0,3). At the seam the next bend occludes both the hall and the unused end.
            for (int half = 0; half < 2; half++)
            {
                var arm = new GameObject("対称通路_" + half).transform; arm.SetParent(root, false);
                if (half == 1) { arm.localPosition = new Vector3(4, 0, 6); arm.localRotation = Quaternion.Euler(0, 180, 0); }
                Box(arm, "縦床", new Vector3(0, -.1f, 1.95f), new Vector3(1.8f, .2f, 3.9f), floor, true);
                Box(arm, "横床", new Vector3(1.45f, -.1f, 3), new Vector3(1.1f, .2f, 1.8f), floor, true);
                Box(arm, "縦天井", new Vector3(0, 2.9f, 1.95f), new Vector3(1.8f, .2f, 3.9f), plaster, true);
                Box(arm, "横天井", new Vector3(1.45f, 2.9f, 3), new Vector3(1.1f, .2f, 1.8f), plaster, true);
                Box(arm, "外壁", new Vector3(-1, 1.4f, 1.95f), new Vector3(.2f, 2.8f, 3.9f), plaster, true);
                Box(arm, "角の内壁", new Vector3(1, 1.4f, 1), new Vector3(.2f, 2.8f, 2), plaster, true);
                Box(arm, "突き当り", new Vector3(.5f, 1.4f, 4), new Vector3(3, 2.8f, .2f), plaster, true);
                Box(arm, "横内壁", new Vector3(1.5f, 1.4f, 2), new Vector3(1, 2.8f, .2f), plaster, true);
                Box(arm, "壁下見切り", new Vector3(-.89f, .09f, 1.95f), new Vector3(.025f, .18f, 3.9f), Mat("Charcoal"));
                Box(arm, "手すり", new Vector3(-.80f, .85f, 1.9f), new Vector3(.045f, .045f, 3.4f), steel);
                for (int i = 0; i < 3; i++) Box(arm, "手すり支持", new Vector3(-.85f, .82f, .45f + i * 1.35f), new Vector3(.13f, .025f, .035f), steel);
                Box(arm, "照明枠", new Vector3(0, 2.76f, 1.5f), new Vector3(.28f, .06f, .9f), steel);
                Box(arm, "照明カバー", new Vector3(0, 2.72f, 1.5f), new Vector3(.22f, .02f, .8f), Mat("FluorescentDiffuser"));
                var lamp = new GameObject("通路照明").AddComponent<Light>(); lamp.transform.SetParent(arm, false);
                lamp.transform.localPosition = new Vector3(0, 2.65f, 1.5f); lamp.transform.localRotation = Quaternion.Euler(90, 0, 0);
                lamp.type = LightType.Spot; lamp.spotAngle = 140; lamp.innerSpotAngle = 95; lamp.range = 5; lamp.intensity = 2.1f;
                lamp.color = new Color(.95f, .98f, 1); lamp.shadows = LightShadows.None;
            }
            Box(root, "未使用端_保全壁", new Vector3(4, 1.4f, 6.1f), new Vector3(2, 2.8f, .2f), plaster, true);
            var source = new GameObject("接続面_進入").transform; source.SetParent(root, false); source.localPosition = new Vector3(2, 0, 3); source.localRotation = Quaternion.Euler(0, 90, 0);
            var target = new GameObject("接続面_帰還").transform; target.SetParent(root, false); target.localPosition = source.localPosition; target.localRotation = Quaternion.Euler(0, 270, 0);
            var loop = root.gameObject.AddComponent<HallwayLoop>(); Set(loop, "entrancePlane", source); Set(loop, "returnPlane", target); Set(loop, "player", player);
            Box(root, "案内金属枠", new Vector3(0, 2.52f, -.10f), new Vector3(.60f, .22f, .04f), steel);
            Box(root, "案内面", new Vector3(0, 2.52f, -.125f), new Vector3(.56f, .18f, .012f), Mat("Charcoal"));
            var label = new GameObject("案内文_" + caption, typeof(TextMeshPro)).GetComponent<TextMeshPro>(); label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0, 2.52f, -.135f); label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
            label.fontSize = .45f; label.alignment = TextAlignmentOptions.Center; label.color = new Color(.9f, .96f, .9f); label.rectTransform.sizeDelta = new Vector2(.52f, .16f); label.text = caption;
            copy.Add("passage." + key, caption + "の案内", caption, label);
        }

        private static void Opening(Transform parent, string name, Vector3 position, Quaternion rotation, float width, Material material)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.SetPositionAndRotation(position, rotation);
            for (int side = -1; side <= 1; side += 2) Box(root, "袖壁", new Vector3(side * (width + 1.8f) / 4, 1.5f, 0), new Vector3((width - 1.8f) / 2, 3, .2f), material, true);
            Box(root, "上壁", new Vector3(0, 2.8f, 0), new Vector3(1.8f, .4f, .2f), material, true);
        }
        private static void RoomTone(GameObject[] roots, Transform player)
        {
            var root = new GameObject("演出調整_空間ごとの設備音"); var tone = root.AddComponent<ApartmentRoomTone>();
            Set(tone, "listener", player); Set(tone, "tuning", UnityEngine.Object.FindFirstObjectByType<ElevatorTuning>());
            var so = new SerializedObject(tone); var zones = so.FindProperty("zones"); zones.arraySize = 4;
            string[] environments = { "EntranceHall", "EntranceHall", "Building", "HomeCorridor" };
            string[] names = { "入口の換気口", "ホールの換気口", "かごの天井", "廊下の設備室" };
            Vector3[] centers = { new(0, 1.5f, -7.5f), new(0, 1.5f, -1.5f), new(0, 1.5f, 3.5f), new(0, 1.5f, -5f) };
            Vector3[] sizes = { new(7, 3, 5), new(7, 3, 7), new(3.2f, 3, 3), new(3.2f, 3, 14) };
            for (int i = 0; i < 4; i++)
            {
                var emitter = new GameObject(names[i]).AddComponent<AudioSource>(); emitter.transform.SetParent(root.transform, false);
                emitter.transform.position = centers[i] + Vector3.up * 1.2f;
                emitter.playOnAwake = false; emitter.loop = true; emitter.volume = 0; emitter.spatialBlend = .65f;
                emitter.minDistance = 1.5f; emitter.maxDistance = 10; emitter.rolloffMode = AudioRolloffMode.Linear; emitter.dopplerLevel = 0;
                emitter.pitch = i == 2 ? 1 : .8f; emitter.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ApartmentVisuals/Audio/Ventilation.wav");
                var zone = zones.GetArrayElementAtIndex(i); zone.FindPropertyRelative("label").stringValue = names[i];
                zone.FindPropertyRelative("environment").objectReferenceValue = roots.Single(r => r.name == environments[i]).transform;
                zone.FindPropertyRelative("source").objectReferenceValue = emitter; zone.FindPropertyRelative("listeningArea").boundsValue = new Bounds(centers[i], sizes[i]);
                zone.FindPropertyRelative("level").floatValue = i == 2 ? .8f : .4f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/ApartmentVisuals/" + name + ".mat");
        private static void Set(UnityEngine.Object obj, string field, UnityEngine.Object value) { var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static GameObject Box(Transform parent, string name, Vector3 p, Vector3 scale, Material material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = p; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
    }
}
