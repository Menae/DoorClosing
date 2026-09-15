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
        private Component journey;
        private CursorLockMode savedLock;
        private bool savedVisible;
        private readonly List<string> states = new List<string>();
        private readonly List<string> outcomes = new List<string>();
        private readonly List<(EventInfo info, Delegate callback)> subscriptions = new List<(EventInfo, Delegate)>();
        private string JourneyState => journey.GetType().GetProperty("State").GetValue(journey).ToString();

        public override void Setup()
        {
            base.Setup();
            savedLock = Cursor.lockState;
            savedVisible = Cursor.visible;
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            states.Clear();
            outcomes.Clear();
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

        private Vector3 Control(string name) => fixtureScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>())
            .Single(t => t.name == name).GetComponent<Collider>().bounds.center;

        private IEnumerator WaitForDoors()
        {
            var elevator = journey.GetComponent(GameAccess.Type("ElevatorController"));
            float end = Time.realtimeSinceStartup + 4;
            while ((bool)elevator.GetType().GetProperty("IsDoorMoving").GetValue(elevator)
                && Time.realtimeSinceStartup < end) yield return null;
            Assert.That((bool)elevator.GetType().GetProperty("IsDoorMoving").GetValue(elevator), Is.False);
        }

        private IEnumerator ClickAt(Vector3 point)
        {
            yield return Aim(point);
            Press(mouse.leftButton, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
        }

        private IEnumerator WalkTo(Vector3 point)
        {
            Vector3 horizontalTarget = point;
            horizontalTarget.y = camera.transform.position.y;
            yield return Aim(horizontalTarget);
            Press(keyboard.wKey, queueEventOnly: true);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(point.x, point.z)) > 0.12f
                && Time.realtimeSinceStartup < deadline) yield return null;
            Release(keyboard.wKey, queueEventOnly: true);
            yield return null;
            Assert.That(Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(point.x, point.z)),
                Is.LessThan(0.25f), "WASD route obstructed");
        }

        private static IEnumerator Capture(string path)
        {
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(File.Exists(path), Is.True, "Screenshot was not written: " + path);
        }

        private static IEnumerator Until(Func<bool> predicate, string description, float timeout = 12f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Timed out: " + description);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SprintThroughOpeningDoor_RevealsLureAndLightsHall()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/M2VerticalSlice.unity",new LoadSceneParameters(LoadSceneMode.Single));
            fixtureScene=SceneManager.GetSceneByPath("Assets/Scenes/M2VerticalSlice.unity"); SceneManager.SetActiveScene(fixtureScene);
            var roots=fixtureScene.GetRootGameObjects(); player=roots.Single(r=>r.name=="Player").transform;
            camera=player.GetComponentInChildren<Camera>(); journey=FindGameComponent("NormalJourneyController");
            yield return WalkTo(new Vector3(0,.95f,.5f)); yield return ClickAt(new Vector3(1.25f,1.5f,1.82f));
            yield return Until(()=>JourneyState=="Boarding","boarding"); yield return WaitForDoors();
            yield return WalkTo(new Vector3(0,.95f,3.6f)); yield return ClickAt(Control("Floor8"));
            var elevator=FindGameComponent("ElevatorController");
            Assert.That((bool)elevator.GetType().GetProperty("DestinationSelected").GetValue(elevator),Is.True);
            yield return Aim(new Vector3(0,camera.transform.position.y,1.8f));
            yield return WaitForDoors();
            ScreenCapture.CaptureScreenshot("artifacts/feedback-01/inside-door-selected.png"); yield return null;
            yield return Until(()=>Count("Arrive")>0,"arrival",40);
            float chimeAt=Time.time;
            var door=roots.Single(r=>r.name=="Building").transform.Find("DoorLeft"); var closed=door.position;
            yield return new WaitForSeconds(.8f); Assert.That(door.position,Is.EqualTo(closed),"Chime must precede door movement");
            Press(keyboard.leftShiftKey,queueEventOnly:true); Press(keyboard.wKey,queueEventOnly:true);
            yield return new WaitForSeconds(.3f); Assert.That(Count("Reveal"),Is.Zero,"Pushing against a closed door must not reveal");
            yield return Until(()=>Count("Reveal")>0,"Early exit must reveal",5);
            Release(keyboard.wKey,queueEventOnly:true); Release(keyboard.leftShiftKey,queueEventOnly:true); yield return null;
            Assert.That(Time.time-chimeAt,Is.GreaterThanOrEqualTo(1.4f));
            Assert.That((bool)elevator.GetType().GetProperty("IsDoorMoving").GetValue(elevator),Is.True,"Reproduce exit before opening completes");
            var lure=FindGameComponent("LureAnomaly"); var lamp=lure.GetComponentInChildren<Light>();
            Assert.That(lamp,Is.Not.Null); Assert.That(lamp.isActiveAndEnabled,Is.True); Assert.That(lamp.intensity,Is.GreaterThan(0));
            Assert.That(lamp.color.r,Is.GreaterThan(lamp.color.g));
            Assert.That(player.position.z,Is.LessThan(2.15f),"Body must cross the threshold, not lean against the door");
            yield return WalkTo(new Vector3(0,.95f,.5f));
            yield return Aim(new Vector3(0,1.6f,-6));
            ScreenCapture.CaptureScreenshot("artifacts/feedback-01/sprint-reveal.png"); yield return null;
        }

        [UnityTest]
        public IEnumerator HijackPresentation_PassesThirteenKeepsCadenceAndStopsOnCleanup()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/M2VerticalSlice.unity",new LoadSceneParameters(LoadSceneMode.Single));
            fixtureScene=SceneManager.GetSceneByPath("Assets/Scenes/M2VerticalSlice.unity"); SceneManager.SetActiveScene(fixtureScene);
            yield return null; // Let the journey complete its Start/reset before the presentation fixture.
            var tuning=FindGameComponent("ElevatorTuning"); tuning.GetType().GetField("怪異の階数上昇間隔").SetValue(tuning,.1f);
            var floor=FindGameComponent("FloorIndicator"); floor.GetType().GetMethod("SetFloor").Invoke(floor,new object[]{12});
            var anomaly=new GameObject("Hijack presentation fixture").AddComponent(GameAccess.Type("HijackAnomaly"));
            int Number() => (int)floor.GetType().GetProperty("CurrentDisplayedFloor").GetValue(floor);
            anomaly.GetType().GetMethod("OnDiagnosisStart").Invoke(anomaly,null);
            yield return new WaitForSeconds(.36f); Assert.That(Number(),Is.InRange(15,16));
            int before=Number(); anomaly.GetType().GetMethod("OnReveal").Invoke(anomaly,null);
            yield return new WaitForSeconds(.35f); Assert.That(Number()-before,Is.InRange(3,4));
            anomaly.GetType().GetMethod("OnCleanup").Invoke(anomaly,null); int stopped=Number();
            yield return new WaitForSeconds(.25f); Assert.That(Number(),Is.EqualTo(stopped));
            UnityEngine.Object.Destroy(anomaly.gameObject);
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
            journey = Array.Find(roots, go => go.name == "NormalJourney")
                .GetComponent(GameAccess.Type("NormalJourneyController"));
            Assert.That(journey, Is.Not.Null);
            Assert.That(((Behaviour)journey).enabled, Is.True, "M2 night must begin at the entrance journey");
            var information = FindGameComponent("CabinInformationDisplay");
            Assert.That(information, Is.Not.Null, "The information housing must exist before any encounter");
            var housing=information.transform.Find("Housing").GetComponent<Renderer>();
            var backWall=roots.Single(r=>r.name=="Building").transform.Find("FrontLeft").GetComponent<Renderer>();
            Assert.That(backWall.bounds.max.z-housing.bounds.min.z,Is.InRange(0f,.004f),"Housing rear must touch the wall");
            Assert.That(roots.Single(r=>r.name=="Building").transform.Find("FloorDisplay").gameObject.activeSelf,Is.False);

            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../artifacts/m2-04/scene-input-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(evidence);

            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "entrance.png"));
            yield return WalkTo(new Vector3(0f, 0.95f, 0.5f));
            yield return ClickAt(new Vector3(1.25f, 1.5f, 1.82f));
            yield return Until(() => JourneyState == "Boarding", "elevator call");
            yield return WaitForDoors();
            yield return WalkTo(new Vector3(0f, 0.95f, 3.6f));
            yield return WalkTo(new Vector3(-.85f,.95f,3.8f));
            yield return Aim(information.transform.position);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"information-mounted-side.png"));
            yield return null;
            yield return WalkTo(new Vector3(0f,.95f,3.6f));
            yield return ClickAt(Control("Floor8"));
            yield return Until(() => Count("Diagnosis") >= 1, "lure diagnosis", 40f);
            Assert.That(((Behaviour)journey).enabled, Is.False, "Encounter input must route to BeatStateMachine");
            Component lure = FindGameComponent("LureAnomaly");
            Assert.That(roots.Single(r=>r.name=="EntranceHall").activeSelf,Is.False,"Do not leave the lobby in front of the anomaly");
            Assert.That(roots.Single(r=>r.name=="HomeCorridor").activeSelf,Is.True,"False arrival must share the eighth-floor baseline");
            var pillar=lure.GetComponentInChildren<Collider>();
            Assert.That(pillar,Is.Not.Null);
            var pillarMaterial=pillar.GetComponent<Renderer>().sharedMaterial;
            Assert.That(pillarMaterial.shader.name,Is.EqualTo("GraduationProject/Apartment Surface"));
            yield return Aim(pillar.bounds.center);
            Physics.SyncTransforms();
            var ray=new Ray(camera.transform.position,pillar.bounds.center-camera.transform.position);
            Assert.That(Physics.Raycast(ray,out RaycastHit obstruction,20f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(obstruction.collider,Is.EqualTo(pillar),"The abnormal pillar must be visible from inside the cabin, not hidden by a lobby wall");
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
            // The additional oblique fixture inspection changes our approach. Walk back within real click range.
            yield return WalkTo(new Vector3(.25f,.95f,3.5f));
            Assert.That(Vector3.Distance(camera.transform.position,Control("SideRightClose")),Is.LessThan(1.9f));
            yield return ClickAt(Control("SideRightClose"));
            yield return Until(() => CountOutcome("Correct") >= 1, "lure close resolution");
            yield return null;
            Assert.That(pillarMaterial==null,Is.True,"Completed encounters must release their per-instance materials");

            yield return Until(() => Count("Diagnosis") >= 2, "provocation diagnosis");
            yield return Until(() =>
            {
                Component active = FindGameComponent("ProvocationAnomaly");
                return active != null && ReadBool(active, "IsAnnouncementVisible") && ReadBool(active, "IsAnnouncementPlaying");
            }, "provocation diegetic display and chime", 4.5f);
            yield return Aim(FindGameComponent("ProvocationAnomaly").transform.position);
            Assert.That(FindGameComponent("ProvocationAnomaly").GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled),Is.True,
                "Encounter must not introduce a second floating panel");
            Assert.That(ReadBool(information,"IsShowingAnnouncement"),Is.True);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "provocation.png"));
            yield return Until(() => CountOutcome("Correct") >= 2, "provocation no-input success");
            Assert.That(ReadBool(information,"IsShowingAnnouncement"),Is.False,"Restore the ordinary notice after the encounter");

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
            var floorController=FindGameComponent("FloorIndicator");
            Transform floorDisplay = ((Component)floorController.GetType().GetField("floorText",BindingFlags.Instance|BindingFlags.NonPublic)
                .GetValue(floorController)).transform;
            Assert.That(floorDisplay.gameObject.activeInHierarchy,Is.True,"Hijack must use a visible fixed-panel display");
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
            yield return ClickAt(Control("SideRightEmergency"));
            yield return Until(() => JourneyState == "Arrived", "genuine floor after three encounters");
            Assert.That(outcomes, Does.Not.Contain("RunClear"), "Three encounters alone must not clear the night");
            yield return Aim(new Vector3(0f, 1.6f, -10f));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "home-corridor.png"));
            yield return WalkTo(new Vector3(0f, 0.95f, -10.7f));
            yield return ClickAt(new Vector3(0f, 1.35f, -11.85f));
            yield return Until(() => outcomes.Contains("RunClear"), "home door night clear");
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "complete.png"));
            File.WriteAllText(Path.Combine(evidence, "context.txt"),
                "Scene: Assets/Scenes/M2VerticalSlice.unity\n" +
                "Input: synthetic Keyboard D + Mouse delta + short left clicks; no direct SubmitAction or transform teleport.\n" +
                "Order: entrance call/board/8, Lure close, Provocation no input, Hijack emergency stop, genuine corridor walk, home-door click.\n" +
                "Resolution: " + Screen.width + "x" + Screen.height + "\nUnity: " + Application.unityVersion);
        }

        [UnityTest]
        public IEnumerator LureGraceRecovery_HasVisibleRevealGraceAndResolution()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/M2VerticalSlice.unity", new LoadSceneParameters(LoadSceneMode.Single));
            fixtureScene = SceneManager.GetSceneByPath("Assets/Scenes/M2VerticalSlice.unity");
            SceneManager.SetActiveScene(fixtureScene);
            GameObject[] roots = fixtureScene.GetRootGameObjects();
            player = Array.Find(roots, go => go.name == "Player").transform;
            camera = player.GetComponentInChildren<Camera>();
            journey = Array.Find(roots, go => go.name == "NormalJourney")
                .GetComponent(GameAccess.Type("NormalJourneyController"));
            yield return null;

            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../artifacts/m2-06/lure-recovery-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(evidence);

            yield return WalkTo(new Vector3(0f, 0.95f, 0.5f));
            yield return ClickAt(new Vector3(1.25f, 1.5f, 1.82f));
            yield return Until(() => JourneyState == "Boarding", "elevator call before recovery");
            yield return WaitForDoors();
            yield return WalkTo(new Vector3(0f, 0.95f, 3.6f));
            yield return ClickAt(Control("Floor8"));
            yield return Until(() => Count("Diagnosis") >= 1, "lure diagnosis before recovery", 40f);

            yield return WalkTo(new Vector3(0f, 0.95f, 1.5f));
            yield return Until(() => Count("Reveal") >= 1, "lure reveal before recovery");
            yield return WalkTo(new Vector3(0f, 0.95f, 3.6f));
            yield return Aim(new Vector3(0f, 1.6f, -6f));
            yield return Capture(Path.Combine(evidence, "01-reveal.png"));
            yield return Until(() => Count("Grace") >= 1, "lure grace before recovery");
            yield return Aim(new Vector3(0f, 1.6f, -6f));
            yield return Capture(Path.Combine(evidence, "02-grace.png"));
            yield return ClickAt(Control("SideRightClose"));
            yield return Until(() => Count("Resolve") >= 1, "lure recovery resolution");
            yield return Aim(new Vector3(0f, 1.6f, -6f));
            yield return Capture(Path.Combine(evidence, "03-recovered.png"));
            yield return Until(() => CountOutcome("GraceRecovered") >= 1, "lure grace recovery outcome");

            File.WriteAllText(Path.Combine(evidence, "context.txt"),
                "Scene: Assets/Scenes/M2VerticalSlice.unity\nInput: synthetic Keyboard W + Mouse delta + short clicks.\n" +
                "Sequence: cross threshold, visible reveal, return during grace, close doors, visible recovery/departure.\n" +
                "No direct SubmitAction or transform teleport.\nUnity: " + Application.unityVersion);
        }

        [UnityTest]
        public IEnumerator DeathFade_RestartsAtNightEntranceWithResetView()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/M2VerticalSlice.unity", new LoadSceneParameters(LoadSceneMode.Single));
            fixtureScene = SceneManager.GetSceneByPath("Assets/Scenes/M2VerticalSlice.unity");
            SceneManager.SetActiveScene(fixtureScene);
            GameObject[] roots = fixtureScene.GetRootGameObjects();
            player = Array.Find(roots, go => go.name == "Player").transform;
            camera = player.GetComponentInChildren<Camera>();
            journey = Array.Find(roots, go => go.name == "NormalJourney")
                .GetComponent(GameAccess.Type("NormalJourneyController"));
            yield return null;

            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../artifacts/m2-06/death-retry-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(evidence);

            yield return WalkTo(new Vector3(0f, 0.95f, 0.5f));
            yield return ClickAt(new Vector3(1.25f, 1.5f, 1.82f));
            yield return Until(() => JourneyState == "Boarding", "elevator call before death");
            yield return WaitForDoors();
            yield return WalkTo(new Vector3(0f, 0.95f, 3.6f));
            yield return ClickAt(Control("Floor8"));
            yield return Until(() => Count("Diagnosis") >= 1, "lure diagnosis before death", 40f);

            yield return WalkTo(new Vector3(0f, 0.95f, 1.5f));
            yield return Until(() => Count("Reveal") >= 1, "lure threshold reveal");
            yield return WalkTo(new Vector3(0f, 0.95f, 3.6f));
            yield return Aim(new Vector3(0f, 1.6f, -6f));
            yield return Capture(Path.Combine(evidence, "01-reveal.png"));
            yield return Until(() => Count("Grace") >= 1, "lure grace before second error");
            yield return Aim(new Vector3(0f, 1.6f, -6f));
            yield return Capture(Path.Combine(evidence, "02-grace.png"));
            yield return ClickAt(Control("SideRightEmergency"));
            yield return Until(() => CountOutcome("Death") >= 1, "second error death");
            yield return new WaitForSeconds(0.9f);
            yield return Capture(Path.Combine(evidence, "03-death-blackout.png"));
            yield return Until(() => JourneyState == "WaitingForCall", "night entrance restart");
            yield return new WaitForSeconds(0.9f);

            Assert.That(Vector3.Distance(player.position, new Vector3(0f, 0.95f, -1f)), Is.LessThan(0.15f));
            Assert.That(Quaternion.Angle(player.rotation, Quaternion.identity), Is.LessThan(0.5f));
            Assert.That(Quaternion.Angle(camera.transform.localRotation, Quaternion.identity), Is.LessThan(0.5f));
            Assert.That(Array.Find(roots, go => go.name == "EntranceHall").activeSelf, Is.True);
            Assert.That(Array.Find(roots, go => go.name == "HomeCorridor").activeSelf, Is.False);
            Assert.That(outcomes, Does.Not.Contain("RunClear"));

            yield return Capture(Path.Combine(evidence, "04-entrance-retry.png"));
            File.WriteAllText(Path.Combine(evidence, "context.txt"),
                "Scene: Assets/Scenes/M2VerticalSlice.unity\nInput: synthetic Keyboard W + Mouse delta + short clicks.\n" +
                "Sequence: reveal, grace, second wrong Emergency click, death blackout, entrance retry.\n" +
                "Expected: fade then entrance position/view and night route reset.\nUnity: " + Application.unityVersion);
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
