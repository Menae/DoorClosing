#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GraduationProject.Tests
{
    public class NormalRouteSceneTests : InputTestFixture
    {
        private Scene fixtureScene;
        private Mouse mouse;
        private Keyboard keyboard;
        private Transform player;
        private Camera camera;
        private Component journey;
        private CursorLockMode savedLock;
        private bool savedVisible;
        private string State => journey.GetType().GetProperty("State").GetValue(journey).ToString();

        public override void Setup()
        {
            base.Setup();
            savedLock = Cursor.lockState; savedVisible = Cursor.visible;
            mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTearDown]
        public IEnumerator CloseFixtureScene()
        {
            if (fixtureScene.IsValid() && fixtureScene.isLoaded) yield return SceneManager.UnloadSceneAsync(fixtureScene);
            Cursor.lockState = savedLock; Cursor.visible = savedVisible;
        }

        private IEnumerator Aim(Vector3 point, bool horizontal = false)
        {
            var direction = point - camera.transform.position;
            if (horizontal) direction.y = 0;
            var angles = Quaternion.LookRotation(direction).eulerAngles;
            var actual = camera.transform.eulerAngles;
            Cursor.lockState = CursorLockMode.Locked;
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(
                Mathf.DeltaAngle(actual.y, angles.y) / 0.12f,
                -Mathf.DeltaAngle(actual.x, angles.x) / 0.12f));
            yield return null;
            yield return null;
        }

        private IEnumerator WalkTo(Vector3 point)
        {
            yield return Aim(point, true);
            Press(keyboard.wKey, queueEventOnly: true);
            float end = Time.realtimeSinceStartup + 10;
            while (Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(point.x, point.z)) > 0.12f
                && Time.realtimeSinceStartup < end) yield return null;
            Release(keyboard.wKey, queueEventOnly: true);
            yield return null;
            Assert.That(Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(point.x, point.z)), Is.LessThan(0.25f), "WASD route obstructed");
        }

        private IEnumerator ClickAt(Vector3 point)
        {
            yield return Aim(point);
            Press(mouse.leftButton, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
        }

        private IEnumerator WaitState(string expected)
        {
            float end = Time.realtimeSinceStartup + 8;
            while (State != expected && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(State, Is.EqualTo(expected));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredPrototype_WASD_MouseRaycastClicks_CompleteIntroduction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/M1NormalRoute.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            fixtureScene = SceneManager.GetSceneByPath("Assets/Scenes/M1NormalRoute.unity");
            SceneManager.SetActiveScene(fixtureScene);
            var roots = fixtureScene.GetRootGameObjects();
            player = roots.Single(go => go.name == "Player").transform;
            camera = player.GetComponentInChildren<Camera>();
            journey = roots.Single(go => go.name == "NormalJourney").GetComponent(GameAccess.Type("NormalJourneyController"));
            yield return null;
            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/m1-02/scene-input-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(evidence);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "entrance.png"));
            yield return WalkTo(new Vector3(0, 0.95f, 0.5f));
            yield return ClickAt(new Vector3(1.25f, 1.5f, 1.82f));
            yield return WaitState("Boarding");
            yield return new WaitForSeconds(1.1f);
            yield return WalkTo(new Vector3(0, 0.95f, 3.6f));
            yield return Aim(new Vector3(0, 1.6f, 4.88f));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "panel-hover.png"));
            yield return null;
            yield return ClickAt(new Vector3(0, 1.6f, 4.88f));
            yield return WaitState("Arrived");
            yield return Aim(new Vector3(0, 1.6f, -10));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "corridor.png"));
            yield return WalkTo(new Vector3(0, 0.95f, -10.7f));
            yield return ClickAt(new Vector3(0, 1.35f, -11.85f));
            yield return WaitState("Complete");
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "complete.png"));
            File.WriteAllText(Path.Combine(evidence, "context.txt"), "Scene: Assets/Scenes/M1NormalRoute.unity\nInput: synthetic Keyboard W + Mouse delta + short left clicks; no direct SubmitAction or transform teleport.\nResolution: " + Screen.width + "x" + Screen.height + "\nUnity: " + Application.unityVersion);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredPrototype_LeftShiftSprint_IsFasterThanWalk()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/M1NormalRoute.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            fixtureScene = SceneManager.GetSceneByPath("Assets/Scenes/M1NormalRoute.unity");
            SceneManager.SetActiveScene(fixtureScene);
            player = fixtureScene.GetRootGameObjects().Single(go => go.name == "Player").transform;
            yield return null;

            var controller = player.GetComponent<CharacterController>();
            Assert.That(controller, Is.Not.Null);
            Vector3 start = player.position;

            Press(keyboard.wKey, queueEventOnly: true);
            yield return null;
            Vector3 walkStart = player.position;
            float walkStartedAt = Time.time;
            while (Time.time - walkStartedAt < 0.15f) yield return null;
            float walkDistance = Vector3.ProjectOnPlane(player.position - walkStart, Vector3.up).magnitude;
            float walkSpeed = walkDistance / (Time.time - walkStartedAt);
            Release(keyboard.wKey, queueEventOnly: true);
            yield return null;

            controller.enabled = false;
            player.position = start;
            controller.enabled = true;
            Physics.SyncTransforms();
            Press(keyboard.leftShiftKey, queueEventOnly: true);
            yield return null;
            Press(keyboard.wKey, queueEventOnly: true);
            yield return null;
            Vector3 sprintStart = player.position;
            float sprintStartedAt = Time.time;
            while (Time.time - sprintStartedAt < 0.15f) yield return null;
            float sprintDistance = Vector3.ProjectOnPlane(player.position - sprintStart, Vector3.up).magnitude;
            float sprintSpeed = sprintDistance / (Time.time - sprintStartedAt);
            Release(keyboard.leftShiftKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.wKey, queueEventOnly: true);
            yield return null;

            Assert.That(walkDistance, Is.GreaterThan(0.1f), "Walking must move the authored player");
            Assert.That(sprintSpeed, Is.GreaterThan(walkSpeed * 1.35f),
                $"Left Shift sprint should exceed walking speed. walk={walkSpeed:F3}m/s, sprint={sprintSpeed:F3}m/s");
        }
    }
}
#endif
