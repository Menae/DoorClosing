using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GraduationProject.EditorTools
{
    public static class M2VerticalSliceBuilder
    {
        public const string ScenePath = "Assets/Scenes/M2VerticalSlice.unity";
        private const string TemplatePath = M1NormalRouteBuilder.ScenePath;

        [MenuItem("Tools/Unity Agent/Create M2 Vertical Slice Prototype")]
        public static void Create()
        {
            RequireCleanEditMode();
            if (!File.Exists(TemplatePath))
                throw new InvalidOperationException("Create the saved M1 normal-route prototype first.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("M2 prototype already exists; edit it instead of overwriting.");

            Scene scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the M2 prototype scene.");
            Configure(scene);
        }

        [MenuItem("Tools/Unity Agent/Repair M2 Vertical Slice Prototype")]
        public static void Repair()
        {
            RequireCleanEditMode();
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("Create the M2 prototype first.");
            Configure(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        }

        private static void Configure(Scene scene)
        {
            // Load external assets after opening the scene. Opening a new scene can unload
            // otherwise-unused asset objects retained only by local variables.
            BeatDefinition lure = LoadBeat("Assets/Data/lure.asset");
            BeatDefinition provocation = LoadBeat("Assets/Data/provocation.asset");
            BeatDefinition hijack = LoadBeat("Assets/Data/hijack.asset");

            GameObject[] roots = scene.GetRootGameObjects();
            GameObject player = roots.Single(go => go.name == "Player");
            GameObject system = roots.Single(go => go.name == "NormalJourney");
            GameObject entranceHall = roots.Single(go => go.name == "EntranceHall");
            GameObject corridor = roots.Single(go => go.name == "HomeCorridor");
            GameObject hud = roots.Single(go => go.name == "HUD");
            CharacterController passenger = player.GetComponent<CharacterController>();
            InteractionRaycaster raycaster = player.GetComponent<InteractionRaycaster>();
            NormalJourneyController normalJourney = system.GetComponent<NormalJourneyController>();
            ElevatorController elevator = system.GetComponent<ElevatorController>();
            FloorIndicator indicator = system.GetComponent<FloorIndicator>();
            BoxCollider cabin = system.transform.Find("CabinBodyVolume").GetComponent<BoxCollider>();

            normalJourney.enabled = false;
            entranceHall.SetActive(false);
            corridor.SetActive(true);
            passenger.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.95f, 3.6f), Quaternion.identity);
            passenger.enabled = true;
            Set(elevator, "doorSlideSeconds", 0.75f);

            Transform anomalyParent = new GameObject("EncounterPresentation").transform;
            anomalyParent.SetParent(system.transform, false);
            ResponseEvaluator evaluator = system.GetComponent<ResponseEvaluator>() ?? system.AddComponent<ResponseEvaluator>();
            BeatStateMachine machine = system.GetComponent<BeatStateMachine>() ?? system.AddComponent<BeatStateMachine>();
            Set(machine, "responseEvaluator", evaluator);
            Set(machine, "elevatorController", elevator);
            Set(machine, "floorIndicator", indicator);
            Set(machine, "anomalyParent", anomalyParent);
            Set(machine, "passenger", passenger);
            Set(machine, "cabin", cabin);
            Set(machine, "doorOpenSeconds", 0.75f);
            Set(machine, "doorCloseSeconds", 0.75f);
            Set(machine, "resolveSeconds", 0.8f);
            Set(machine, "representedTravelSeconds", 1.5f);

            Set(raycaster, "beatStateMachine", machine);
            Set(raycaster, "normalJourney", null);

            RunManager run = system.GetComponent<RunManager>() ?? system.AddComponent<RunManager>();
            Set(run, "beatStateMachine", machine);
            Set(run, "blackFadeImage", hud.transform.Find("Fade").GetComponent<Image>());
            TMP_Text clearText = hud.transform.Find("NightComplete").GetComponent<TMP_Text>();
            Set(run, "nightClearText", clearText);
            Set(run, "clearMessage", "M2 VERTICAL SLICE CLEAR");
            Set(run, "clearFadeSeconds", 0.8f);
            Set(run, "startRunOnStart", true);
            SetBeatList(run, lure, provocation, hijack);

            GameObject homeDoor = GameObject.Find("HomeDoor");
            if (homeDoor != null) homeDoor.layer = 0;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[M2] Created three-encounter vertical slice: " + ScenePath + ". Existing build scenes unchanged.");
        }

        private static BeatDefinition LoadBeat(string path)
        {
            BeatDefinition beat = AssetDatabase.LoadAssetAtPath<BeatDefinition>(path);
            if (beat == null) throw new InvalidOperationException("Missing BeatDefinition: " + path);
            return beat;
        }

        private static void SetBeatList(RunManager run, params BeatDefinition[] beats)
        {
            var serialized = new SerializedObject(run);
            SerializedProperty list = serialized.FindProperty("beatDefinitions");
            list.arraySize = beats.Length;
            for (int i = 0; i < beats.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = beats[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RequireCleanEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Requires idle Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve unsaved scenes before creating the M2 prototype.");
        }

        private static void Set(UnityEngine.Object target, string name, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Missing property " + name);
            if (value == null || value is UnityEngine.Object) property.objectReferenceValue = value as UnityEngine.Object;
            else if (value is bool flag) property.boolValue = flag;
            else if (value is float number) property.floatValue = number;
            else if (value is string text) property.stringValue = text;
            else throw new InvalidOperationException("Unsupported property value " + name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
