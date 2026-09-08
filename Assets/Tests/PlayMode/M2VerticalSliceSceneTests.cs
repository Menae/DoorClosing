#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GraduationProject.Tests
{
    public class M2VerticalSliceSceneTests : InputTestFixture
    {
        private Scene fixtureScene;
        private Mouse mouse;
        private Keyboard keyboard;
        private Transform player;
        private Camera camera;
        private CursorLockMode savedLock;
        private bool savedVisible;
        private readonly List<string> states = new List<string>();
        private readonly List<string> outcomes = new List<string>();
        private readonly List<(EventInfo info, Delegate callback)> subscriptions = new List<(EventInfo, Delegate)>();

        public override void Setup()
        {
            base.Setup();
            savedLock = Cursor.lockState;
            savedVisible = Cursor.visible;
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            Subscribe("OnBeatStateChanged", "CaptureState");
            Subscribe("OnBeatResolved", "CaptureOutcome");
        }

        [UnityTearDown]
        public IEnumerator CloseFixtureScene()
        {
            foreach (var entry in subscriptions) entry.info.RemoveEventHandler(null, entry.callback);
            subscriptions.Clear();
            if (fixtureScene.IsValid() && fixtureScene.isLoaded)
            {
                Scene fallback = SceneManager.CreateScene("M2VerticalSliceTestCleanup");
                SceneManager.SetActiveScene(fallback);
                yield return SceneManager.UnloadSceneAsync(fixtureScene);
            }
            Cursor.lockState = savedLock;
            Cursor.visible = savedVisible;
        }

        private void Subscribe(string eventName, string methodName)
        {
            EventInfo info = GameAccess.Type("GameEvents").GetEvent(eventName);
            Type argumentType = info.EventHandlerType.GetGenericArguments()[0];
            MethodInfo method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(argumentType);
            Delegate callback = Delegate.CreateDelegate(info.EventHandlerType, this, method);
            info.AddEventHandler(null, callback);
            subscriptions.Add((info, callback));
        }

        private void CaptureState<T>(T value) => states.Add(value.ToString());
        private void CaptureOutcome<T>(T value) => outcomes.Add(value.ToString());

        private IEnumerator Aim(Vector3 point)
        {
            Vector3 angles = Quaternion.LookRotation(point - camera.transform.position).eulerAngles;
            Vector3 actual = camera.transform.eulerAngles;
            Cursor.lockState = CursorLockMode.Locked;
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(
                Mathf.DeltaAngle(actual.y, angles.y) / 0.12f,
                -Mathf.DeltaAngle(actual.x, angles.x) / 0.12f));
            yield return null;
            yield return null;
        }

        private IEnumerator ClickAt(Vector3 point)
        {
            yield return Aim(point);
            Press(mouse.leftButton, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
        }

        private static IEnumerator Until(Func<bool> predicate, string description, float timeout = 12f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Timed out: " + description);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredPrototype_WASDMouseAndClicks_ClearThreeEncounters()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/M2VerticalSlice.unity", new LoadSceneParameters(LoadSceneMode.Single));
            fixtureScene = SceneManager.GetSceneByPath("Assets/Scenes/M2VerticalSlice.unity");
            SceneManager.SetActiveScene(fixtureScene);
            GameObject[] roots = fixtureScene.GetRootGameObjects();
            player = Array.Find(roots, go => go.name == "Player").transform;
            camera = player.GetComponentInChildren<Camera>();
            Component normalJourney = Array.Find(roots, go => go.name == "NormalJourney")
                .GetComponent(GameAccess.Type("NormalJourneyController"));
            Assert.That(normalJourney, Is.Not.Null);
            Assert.That(((Behaviour)normalJourney).enabled, Is.False, "M2 scene must route clicks to BeatStateMachine");

            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../artifacts/m2-04/scene-input-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(evidence);

            yield return Until(() => Count("Diagnosis") >= 1, "lure diagnosis");
            Component lure = FindGameComponent("LureAnomaly");
            Assert.That(lure, Is.Not.Null);
            Assert.That(lure.transform.lossyScale.y, Is.GreaterThan(2.5f), "Lure trial cue must read as a tall corridor obstruction");
            yield return Aim(new Vector3(0f, 1.6f, -6f));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "lure-corridor.png"));
            Vector3 beforeStep = player.position;
            Press(keyboard.dKey, queueEventOnly: true);
            yield return new WaitForSeconds(0.1f);
            Release(keyboard.dKey, queueEventOnly: true);
            yield return null;
            Assert.That(Vector3.Distance(beforeStep, player.position), Is.GreaterThan(0.1f), "Authored player must accept WASD");
            yield return ClickAt(new Vector3(0f, 1.1f, 4.88f));
            yield return Until(() => CountOutcome("Correct") >= 1, "lure close resolution");

            yield return Until(() => Count("Diagnosis") >= 2, "provocation diagnosis");
            yield return Until(() =>
            {
                Component active = FindGameComponent("ProvocationAnomaly");
                return active != null && ReadBool(active, "IsAnnouncementVisible") && ReadBool(active, "IsAnnouncementPlaying");
            }, "provocation diegetic display and chime", 4.5f);
            yield return Aim(FindGameComponent("ProvocationAnomaly").transform.position);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "provocation.png"));
            yield return Until(() => CountOutcome("Correct") >= 2, "provocation no-input success");

            yield return Until(() => Count("Diagnosis") >= 3, "hijack diagnosis");
            yield return Until(() =>
            {
                Component active = FindGameComponent("HijackAnomaly");
                Component indicator = FindGameComponent("FloorIndicator");
                return active != null && ReadBool(active, "IsMotorPlaying") && indicator != null && ReadInt(indicator, "CurrentDisplayedFloor") > 8;
            }, "hijack motor and floor drift", 3f);
            // Arrival deliberately flickers the display; capture after it has returned
            // so the evidence shows the impossible floor instead of a blank frame.
            yield return new WaitForSeconds(0.6f);
            Transform floorDisplay = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(item => item.name == "FloorDisplay");
            RectTransform floorRect = floorDisplay as RectTransform;
            Assert.That(floorRect, Is.Not.Null);
            Vector3[] corners = new Vector3[4];
            floorRect.GetWorldCorners(corners);
            Vector3 displayCenter = (corners[0] + corners[2]) * 0.5f;
            yield return Aim(displayCenter);
            Vector3 displayViewport = camera.WorldToViewportPoint(displayCenter);
            Assert.That(displayViewport.z, Is.GreaterThan(0f));
            Assert.That(displayViewport.x, Is.InRange(0.2f, 0.8f));
            Assert.That(displayViewport.y, Is.InRange(0.2f, 0.8f));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "hijack-panel.png"));
            yield return ClickAt(new Vector3(0.65f, 1.35f, 4.88f));
            yield return Until(() => outcomes.Contains("RunClear"), "three-encounter run clear");
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "complete.png"));
            File.WriteAllText(Path.Combine(evidence, "context.txt"),
                "Scene: Assets/Scenes/M2VerticalSlice.unity\n" +
                "Input: synthetic Keyboard D + Mouse delta + short left clicks; no direct SubmitAction or transform teleport.\n" +
                "Order: Lure close, Provocation no input, Hijack emergency stop.\n" +
                "Resolution: " + Screen.width + "x" + Screen.height + "\nUnity: " + Application.unityVersion);
        }

        private int Count(string state) => states.FindAll(value => value == state).Count;
        private int CountOutcome(string outcome) => outcomes.FindAll(value => value == outcome).Count;
        private static Component FindGameComponent(string typeName) =>
            UnityEngine.Object.FindFirstObjectByType(GameAccess.Type(typeName)) as Component;
        private static bool ReadBool(Component target, string property) =>
            (bool)target.GetType().GetProperty(property).GetValue(target);
        private static int ReadInt(Component target, string property) =>
            (int)target.GetType().GetProperty(property).GetValue(target);
    }
}
#endif
