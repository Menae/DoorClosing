using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GraduationProject.EditorTools
{
    public static class M1NormalRouteBuilder
    {
        public const string ScenePath = "Assets/Scenes/M1NormalRoute.unity";
        private const string AssetFolder = "Assets/M1Prototype";
        private static Material wall, floor, metal, buttonMaterial;
        private static TMP_FontAsset font;

        [MenuItem("Tools/Unity Agent/Create M1 Normal Route Prototype")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Requires idle Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scenes first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Prototype already exists; edit it instead of overwriting.");
            if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "M1Prototype");
            wall = Material("Wall", new Color(0.58f, 0.57f, 0.53f));
            floor = Material("Floor", new Color(0.23f, 0.25f, 0.25f));
            metal = Material("Metal", new Color(0.35f, 0.38f, 0.4f));
            buttonMaterial = Material("Button", new Color(0.12f, 0.15f, 0.15f));
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansJP-Regular SDF.asset");
            if (font == null) throw new InvalidOperationException("Existing Japanese font is required.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.35f);

            var geometry = new GameObject("Building");
            Cube("CabFloor", new Vector3(0, -0.1f, 3.5f), new Vector3(3.4f, 0.2f, 3.4f), floor, geometry.transform);
            Cube("CabBack", new Vector3(0, 1.5f, 5.1f), new Vector3(3.4f, 3, 0.2f), metal, geometry.transform);
            Cube("CabLeft", new Vector3(-1.7f, 1.5f, 3.5f), new Vector3(0.2f, 3, 3.4f), metal, geometry.transform);
            Cube("CabRight", new Vector3(1.7f, 1.5f, 3.5f), new Vector3(0.2f, 3, 3.4f), metal, geometry.transform);
            Cube("CabCeiling", new Vector3(0, 3.1f, 3.5f), new Vector3(3.4f, 0.2f, 3.4f), wall, geometry.transform);
            Cube("FrontLeft", new Vector3(-1.25f, 1.5f, 2), new Vector3(0.9f, 3, 0.2f), wall, geometry.transform);
            Cube("FrontRight", new Vector3(1.25f, 1.5f, 2), new Vector3(0.9f, 3, 0.2f), wall, geometry.transform);
            var leftDoor = Cube("DoorLeft", new Vector3(-0.4f, 1.45f, 2), new Vector3(0.8f, 2.9f, 0.12f), metal, geometry.transform);
            var rightDoor = Cube("DoorRight", new Vector3(0.4f, 1.45f, 2), new Vector3(0.8f, 2.9f, 0.12f), metal, geometry.transform);
            var hall = new GameObject("EntranceHall");
            Room(hall.transform, 7, -1.5f, 7);
            Label("Notice", "おかえりなさい\n自宅は８階です\n\n呼びボタンで\n扉を開けてください", new Vector3(-2.15f, 1.9f, 1.79f), hall.transform, 1.5f, new Vector2(2.2f, 2));
            AddEntranceEnvelope(geometry.transform, hall.transform);
            var corridor = new GameObject("HomeCorridor");
            Room(corridor.transform, 3.4f, -5, 14);
            var home = Button("HomeDoor", "自宅", PlayerAction.TouchHomeDoor, -1, new Vector3(0, 1.35f, -11.85f), corridor.transform);
            home.transform.localScale = new Vector3(4, 7, 1);
            home.transform.rotation = Quaternion.Euler(0, 180, 0);
            FitHomeLabel(home.transform);
            Label("Floor8Notice", "８階", new Vector3(1.2f, 2.2f, -1), corridor.transform, 3, new Vector2(1, 0.6f)).transform.rotation = Quaternion.Euler(0, 180, 0);
            corridor.SetActive(false);
            Lamp(new Vector3(0, 2.8f, 3.5f), geometry.transform);
            Lamp(new Vector3(0, 2.8f, -1), hall.transform);
            Lamp(new Vector3(0, 2.8f, -2), corridor.transform);
            Lamp(new Vector3(0, 2.8f, -7), corridor.transform);

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0.95f, -1);
            var body = player.AddComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.25f; body.skinWidth = 0.03f;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0.65f, 0);
            var camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 100; camera.fieldOfView = 65;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var look = player.AddComponent<PlayerLook>();
            Set(look, "cameraTransform", camera.transform); Set(look, "characterController", body);
            Set(look, "enableSprint", true); Set(look, "applyGravity", true);
            var raycaster = player.AddComponent<InteractionRaycaster>();
            Set(raycaster, "playerCamera", camera); Set(raycaster, "interactableLayers", 1 << 6);

            var system = new GameObject("NormalJourney");
            var elevator = system.AddComponent<ElevatorController>();
            var safety = Zone("DoorSafety", new Vector3(0, 1.5f, 2), new Vector3(1.65f, 3, 0.55f), system.transform);
            var cabin = Zone("CabinBodyVolume", new Vector3(0, 1.5f, 3.6f), new Vector3(3.1f, 3, 2.8f), system.transform);
            Set(elevator, "leftDoor", leftDoor.transform); Set(elevator, "rightDoor", rightDoor.transform);
            Set(elevator, "leftDoorOpenLocalOffset", new Vector3(-0.82f, 0, 0));
            Set(elevator, "rightDoorOpenLocalOffset", new Vector3(0.82f, 0, 0));
            Set(elevator, "doorSafetyZone", safety); Set(elevator, "passengerBody", body);
            var indicatorText = Label("FloorDisplay", "1", new Vector3(0, 2.3f, 4.91f), geometry.transform, 4, new Vector2(1, 0.6f));
            var indicator = system.AddComponent<FloorIndicator>(); Set(indicator, "floorText", indicatorText);
            var journey = system.AddComponent<NormalJourneyController>();
            Set(journey, "elevator", elevator); Set(journey, "indicator", indicator);
            Set(journey, "passenger", body); Set(journey, "cabin", cabin);
            Set(journey, "entranceHall", hall); Set(journey, "homeCorridor", corridor); Set(journey, "playerLook", look);
            Set(raycaster, "normalJourney", journey);
            Button("Call", "呼", PlayerAction.PressOpen, -1, new Vector3(1.25f, 1.5f, 1.82f), geometry.transform);
            Button("Floor8", "8", PlayerAction.PressFloor, 8, new Vector3(0, 1.6f, 4.88f), geometry.transform);
            Button("Floor7", "7", PlayerAction.PressFloor, 7, new Vector3(-0.5f, 1.6f, 4.88f), geometry.transform);
            Button("Open", "開", PlayerAction.PressOpen, -1, new Vector3(-0.5f, 1.1f, 4.88f), geometry.transform);
            Button("Close", "閉", PlayerAction.PressClose, -1, new Vector3(0, 1.1f, 4.88f), geometry.transform);
            Button("Emergency", "非常停止", PlayerAction.PressEmergencyStop, -1, new Vector3(0.65f, 1.35f, 4.88f), geometry.transform);

            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var dot = new GameObject("AimDot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(canvas.transform, false);
            dot.GetComponent<RectTransform>().sizeDelta = new Vector2(4, 4);
            dot.GetComponent<Image>().raycastTarget = false;
            var fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            fadeObject.transform.SetParent(canvas.transform, false);
            var fadeRect = fadeObject.GetComponent<RectTransform>(); fadeRect.anchorMin = Vector2.zero; fadeRect.anchorMax = Vector2.one; fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            var fade = fadeObject.GetComponent<Image>(); fade.color = Color.black; fade.raycastTarget = false;
            var completed = new GameObject("NightComplete", typeof(RectTransform), typeof(TextMeshProUGUI));
            completed.transform.SetParent(canvas.transform, false);
            completed.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 200);
            var completedText = completed.GetComponent<TextMeshProUGUI>(); completedText.font = font; completedText.fontSize = 40; completedText.alignment = TextAlignmentOptions.Center;
            completedText.text = "帰宅しました";
            Set(journey, "fade", fade); Set(journey, "completionText", completedText);
            fadeObject.SetActive(false); completed.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[M1] Created normal-route prototype: " + ScenePath + ". Existing build scenes unchanged.");
        }

        [MenuItem("Tools/Unity Agent/Repair M1 Prototype Envelope")]
        public static void RepairEnvelope()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Requires M1NormalRoute in Edit Mode.");
            wall = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/Wall.mat");
            buttonMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/Button.mat");
            var geometry = GameObject.Find("Building"); var hall = GameObject.Find("EntranceHall");
            AddEntranceEnvelope(geometry.transform, hall.transform);
            var notice = hall.transform.Find("Notice").GetComponent<TextMeshPro>();
            notice.transform.position = new Vector3(-2.15f, 1.9f, 1.79f);
            notice.fontSize = 1.5f; notice.rectTransform.sizeDelta = new Vector2(2.2f, 2);
            notice.text = "おかえりなさい\n自宅は８階です\n\n呼びボタンで\n扉を開けてください";
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "HomeCorridor") FitHomeLabel(root.transform.Find("HomeDoor"));
                foreach (var target in root.GetComponentsInChildren<Interactable>(true)) ConfigureHighlight(target);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static void AddEntranceEnvelope(Transform geometry, Transform hall)
        {
            if (geometry.Find("OuterFrontLeft") == null)
                Cube("OuterFrontLeft", new Vector3(-2.6f, 1.5f, 2), new Vector3(1.8f, 3, 0.2f), wall, geometry);
            if (geometry.Find("OuterFrontRight") == null)
                Cube("OuterFrontRight", new Vector3(2.6f, 1.5f, 2), new Vector3(1.8f, 3, 0.2f), wall, geometry);
            if (hall.Find("NoticeBoard") == null)
                Cube("NoticeBoard", new Vector3(-2.15f, 1.9f, 1.84f), new Vector3(2.3f, 2.05f, 0.05f), buttonMaterial, hall);
        }

        private static void FitHomeLabel(Transform home)
        {
            var label = home.GetComponentInChildren<TextMeshPro>(true);
            label.transform.localScale = new Vector3(1f / home.localScale.x, 1f / home.localScale.y, 1);
            label.rectTransform.sizeDelta = new Vector2(1.2f, 0.5f);
            label.fontSize = 2.5f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void ConfigureHighlight(Interactable target)
        {
            var serialized = new SerializedObject(target);
            var renderers = serialized.FindProperty("targetRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = target.GetComponent<Renderer>();
            serialized.FindProperty("hoverColor").colorValue = new Color(0.25f, 0.3f, 0.3f);
            serialized.FindProperty("hoverEmissionIntensity").floatValue = 0.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Room(Transform parent, float width, float centerZ, float length)
        {
            Cube("Floor", new Vector3(0, -0.1f, centerZ), new Vector3(width, 0.2f, length), floor, parent);
            Cube("LeftWall", new Vector3(-width / 2, 1.5f, centerZ), new Vector3(0.2f, 3, length), wall, parent);
            Cube("RightWall", new Vector3(width / 2, 1.5f, centerZ), new Vector3(0.2f, 3, length), wall, parent);
            Cube("EndWall", new Vector3(0, 1.5f, centerZ - length / 2), new Vector3(width, 3, 0.2f), wall, parent);
            Cube("Ceiling", new Vector3(0, 3.1f, centerZ), new Vector3(width, 0.2f, length), wall, parent);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            var go = mesh.gameObject; go.name = name; go.transform.SetParent(parent, false); go.transform.position = position;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            if (collider == null) { var box = go.AddComponent<BoxCollider>(); box.size = size; }
            return go;
        }

        private static BoxCollider Zone(string name, Vector3 position, Vector3 size, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position;
            var box = go.AddComponent<BoxCollider>(); box.size = size; box.isTrigger = true; return box;
        }

        private static GameObject Button(string name, string text, PlayerAction action, int number, Vector3 position, Transform parent)
        {
            var go = Cube(name, position, new Vector3(0.35f, 0.3f, 0.08f), buttonMaterial, parent); go.layer = 6;
            var target = go.AddComponent<Interactable>(); Set(target, "actionType", (int)action); Set(target, "floorNumber", number);
            ConfigureHighlight(target);
            var label = Label(name + "Label", text, position + Vector3.back * 0.045f, go.transform, text.Length > 2 ? 1.15f : 2.5f, new Vector2(0.34f, 0.28f));
            label.color = Color.white;
            return go;
        }

        private static TextMeshPro Label(string name, string text, Vector3 position, Transform parent, float size, Vector2 bounds)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position;
            var label = go.AddComponent<TextMeshPro>(); label.font = font; label.text = text; label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = bounds; return label;
        }

        private static void Lamp(Vector3 position, Transform parent)
        {
            var go = new GameObject("CeilingLight", typeof(Light)); go.transform.SetParent(parent, false); go.transform.position = position;
            var light = go.GetComponent<Light>(); light.type = LightType.Point; light.color = Color.white; light.intensity = 2; light.range = 8;
        }

        private static Material Material(string name, Color color)
        {
            string path = AssetFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
            AssetDatabase.CreateAsset(material, path); return material;
        }

        private static void Set(UnityEngine.Object target, string name, object value)
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Missing property " + name);
            if (value is UnityEngine.Object reference) property.objectReferenceValue = reference;
            else if (value is bool flag) property.boolValue = flag;
            else if (value is int integer) property.intValue = integer;
            else if (value is Vector3 vector) property.vector3Value = vector;
            else throw new InvalidOperationException("Unsupported property value " + name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
