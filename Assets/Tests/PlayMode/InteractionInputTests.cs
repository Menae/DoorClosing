using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GraduationProject.Tests
{
    // The game remains in Assembly-CSharp to preserve existing serialized UnityEvent targets.
    // Reflection is confined to this adapter; missing/renamed members fail explicitly.
    internal static class GameAccess
    {
        internal static Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Single(a => a.GetName().Name == "Assembly-CSharp").GetType(name, true);
        internal static object Enum(string type, string value) => System.Enum.Parse(Type(type), value);
        internal static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing game field: {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }
        internal static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(target, args);
    }

    public class InteractionInputTests : InputTestFixture
    {
        private GameObject root;
        private Component machine;
        private Component elevator;
        private Component target;
        private ScriptableObject beat;
        private Image gauge;
        private CharacterController passenger;
        private BoxCollider cabin;
        private BoxCollider safety;
        private Mouse mouse;
        private float savedTimeScale;
        private readonly List<string> states = new List<string>();
        private readonly List<string> commits = new List<string>();
        private readonly List<string> outcomes = new List<string>();
        private readonly List<(EventInfo info, Delegate callback)> subscriptions = new List<(EventInfo, Delegate)>();
        private static readonly Vector3 OccupancyOrigin = new Vector3(520, 500, 500);
        private bool DoorMoving => (bool)elevator.GetType().GetProperty("IsDoorMoving").GetValue(elevator);
        private bool DoorOpen => (bool)elevator.GetType().GetProperty("IsDoorOpen").GetValue(elevator);
        private bool Travelling => (bool)elevator.GetType().GetProperty("IsTravelling").GetValue(elevator);

        public override void Setup()
        {
            base.Setup();
            savedTimeScale = Time.timeScale;
            Time.timeScale = 1;
            states.Clear(); commits.Clear(); outcomes.Clear();
            mouse = InputSystem.AddDevice<Mouse>();
            Subscribe("OnBeatStateChanged", "CaptureState");
            Subscribe("OnPlayerCommitted", "CaptureCommit");
            Subscribe("OnBeatResolved", "CaptureOutcome");

            root = new GameObject("UnityAgent_InputFixture");
            root.SetActive(false);
            root.AddComponent(GameAccess.Type("ResponseEvaluator"));
            elevator = root.AddComponent(GameAccess.Type("ElevatorController"));
            GameAccess.Set(elevator, "doorSlideSeconds", 0.05f);

            var passengerObject = new GameObject("Passenger");
            passengerObject.transform.SetParent(root.transform);
            passengerObject.transform.position = OccupancyOrigin;
            passenger = passengerObject.AddComponent<CharacterController>();
            passenger.height = 1.8f;
            passenger.radius = 0.25f;
            var cabinObject = new GameObject("Cabin", typeof(BoxCollider));
            cabinObject.transform.SetParent(root.transform);
            cabinObject.transform.position = OccupancyOrigin;
            cabin = cabinObject.GetComponent<BoxCollider>();
            cabin.size = new Vector3(4f, 3f, 4f);
            cabin.isTrigger = true;
            var safetyObject = new GameObject("DoorSafety", typeof(BoxCollider));
            safetyObject.transform.SetParent(root.transform);
            safetyObject.transform.position = OccupancyOrigin + Vector3.forward * 8f;
            safety = safetyObject.GetComponent<BoxCollider>();
            safety.size = new Vector3(2f, 3f, 0.5f);
            safety.isTrigger = true;
            GameAccess.Set(elevator, "doorSafetyZone", safety);
            GameAccess.Set(elevator, "passengerBody", passenger);

            machine = root.AddComponent(GameAccess.Type("BeatStateMachine"));
            GameAccess.Set(machine, "elevatorController", elevator);
            GameAccess.Set(machine, "passenger", passenger);
            GameAccess.Set(machine, "cabin", cabin);
            GameAccess.Set(machine, "doorOpenSeconds", 0f);
            GameAccess.Set(machine, "doorCloseSeconds", 0f);
            GameAccess.Set(machine, "resolveSeconds", 0f);
            GameAccess.Set(machine, "representedTravelSeconds", 0f);

            var cameraObject = new GameObject("FixtureCamera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = new Vector3(500, 500, 500);
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; // Tests exercise rays, not a competing rendered camera.

            var targetObject = new GameObject("FixtureButton", typeof(BoxCollider));
            targetObject.transform.SetParent(root.transform);
            targetObject.transform.position = cameraObject.transform.position + Vector3.forward;
            targetObject.transform.localScale = Vector3.one * 0.3f;
            target = targetObject.AddComponent(GameAccess.Type("Interactable"));
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", "PressClose"));

            var gaugeObject = new GameObject("FixtureGauge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gaugeObject.transform.SetParent(root.transform);
            gauge = gaugeObject.GetComponent<Image>();
            var raycaster = root.AddComponent(GameAccess.Type("InteractionRaycaster"));
            GameAccess.Set(raycaster, "playerCamera", camera);
            GameAccess.Set(raycaster, "beatStateMachine", machine);
            GameAccess.Set(raycaster, "holdGaugeImage", gauge);

            GameAccess.Set(raycaster, "interactableLayers", (LayerMask)1);

            beat = ScriptableObject.CreateInstance(GameAccess.Type("BeatDefinition"));
            GameAccess.Set(beat, "category", GameAccess.Enum("AnomalyCategory", "Lure"));
            GameAccess.Set(beat, "correctAction", GameAccess.Enum("PlayerAction", "PressClose"));
            GameAccess.Set(beat, "travelSeconds", 0f);
            GameAccess.Set(beat, "revealSeconds", 0f);
            GameAccess.Set(beat, "graceSeconds", 3f);
            root.SetActive(true);
            Physics.SyncTransforms();
        }

        public override void TearDown()
        {
            try
            {
                foreach (var entry in subscriptions) entry.info.RemoveEventHandler(null, entry.callback);
                subscriptions.Clear();
                if (root != null) Object.DestroyImmediate(root);
                if (beat != null) Object.DestroyImmediate(beat);
                Time.timeScale = savedTimeScale;
            }
            finally { base.TearDown(); }
        }

        private void Subscribe(string eventName, string methodName)
        {
            var info = GameAccess.Type("GameEvents").GetEvent(eventName);
            var argumentType = info.EventHandlerType.GetGenericArguments()[0];
            var method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(argumentType);
            var callback = Delegate.CreateDelegate(info.EventHandlerType, this, method);
            info.AddEventHandler(null, callback);
            subscriptions.Add((info, callback));
        }
        private void CaptureState<T>(T value) => states.Add(value.ToString());
        private void CaptureCommit<T>(T value) => commits.Add(value.ToString());
        private void CaptureOutcome<T>(T value) => outcomes.Add(value.ToString());
        private void Begin() => GameAccess.Call(machine, "BeginBeat", beat);
        private static IEnumerator Until(Func<bool> predicate, string description, float timeout = 5)
        {
            var deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, "Timed out: " + description);
        }
        private IEnumerator Diagnose()
        {
            Begin();
            yield return Until(() => states.Contains("Diagnosis"), "Diagnosis state");
            yield return null; // Allow the raycaster to acquire the target before the press edge.
        }

        [UnityTest]
        public IEnumerator Click_CommitsOnFirstInputFrame_WithoutGauge()
        {
            yield return Diagnose();
            Press(mouse.leftButton, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            // A tap shorter than a rendered frame must still be accepted without hold time.
            yield return Until(() => outcomes.Contains("Correct"), "immediate click resolution", 0.25f);
            Assert.That(commits, Is.EqualTo(new[] { "PressClose" }));
            Assert.That(gauge.gameObject.activeSelf, Is.False);
            Assert.That(gauge.fillAmount, Is.Zero);
            Assert.That(states, Does.Contain("Depart"));
        }

        [UnityTest]
        public IEnumerator ContinuousHold_DoesNotCommitAgainAfterRepresentation()
        {
            GameAccess.Set(beat, "category", GameAccess.Enum("AnomalyCategory", "Normal"));
            yield return Diagnose();
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => states.Count(s => s == "Diagnosis") == 2, "same beat represented");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(commits, Is.EqualTo(new[] { "PressClose" }));
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => commits.Count == 2, "new click commits again");
        }

        [UnityTest]
        public IEnumerator PressOutsideTarget_DoesNotCommitWhenHeldOverTarget()
        {
            yield return Diagnose();
            var position = target.transform.position;
            target.transform.position += Vector3.right * 5;
            Physics.SyncTransforms();
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            target.transform.position = position;
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(commits, Is.Empty);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => outcomes.Contains("Correct"), "fresh on-target click");
        }

        [UnityTest]
        public IEnumerator WrongClick_ThenCorrectClick_RecoversDuringGrace()
        {
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", "PressEmergencyStop"));
            yield return Diagnose();
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => states.Contains("Grace"), "grace after wrong action");
            Assert.That(outcomes, Does.Contain("WrongRevealed"));
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", "PressClose"));
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => outcomes.Contains("GraceRecovered"), "recovery through click path");
            Assert.That(commits, Is.EqualTo(new[] { "PressEmergencyStop", "PressClose" }));
            Assert.That(outcomes, Does.Not.Contain("Death"));
        }

        [UnityTest]
        public IEnumerator RevealClick_IsDiscarded_AndNeedsFreshGraceClick()
        {
            GameAccess.Set(beat, "revealSeconds", 0.35f);
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", "PressEmergencyStop"));
            yield return Diagnose();
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => states.Contains("Reveal"), "reveal");
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => states.Contains("Grace"), "grace");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(commits.Count, Is.EqualTo(1));
            Assert.That(outcomes, Does.Not.Contain("Death"));
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", "PressClose"));
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => outcomes.Contains("GraceRecovered"), "fresh grace click");
        }

        [UnityTest]
        public IEnumerator TravelClick_IsNotCarriedIntoDiagnosis()
        {
            GameAccess.Set(beat, "travelSeconds", 0.25f);
            Begin();
            yield return null;
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => states.Contains("Diagnosis"), "diagnosis after travel");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(commits, Is.Empty);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Press(mouse.leftButton, queueEventOnly: true);
            yield return Until(() => outcomes.Contains("Correct"), "fresh diagnosis click");
        }

        private IEnumerator ClickAction(string action)
        {
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", action));
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
        }

        private void MovePassenger(Vector3 position)
        {
            passenger.enabled = false;
            passenger.transform.position = position;
            passenger.enabled = true;
            Physics.SyncTransforms();
        }

        private void ConfigureHijack(float diagnosisDeadline, float graceDeadline)
        {
            GameAccess.Set(beat, "category", GameAccess.Enum("AnomalyCategory", "Hijack"));
            GameAccess.Set(beat, "correctAction", GameAccess.Enum("PlayerAction", "PressEmergencyStop"));
            GameAccess.Set(beat, "doorsStayClosed", true);
            GameAccess.Set(beat, "hijackDeadlineSeconds", diagnosisDeadline);
            GameAccess.Set(beat, "graceSeconds", graceDeadline);
        }

        [UnityTest]
        public IEnumerator InvalidButtons_DoNotCommitOrCountAsSecondError()
        {
            yield return Diagnose();
            yield return ClickAction("PressOpen");
            yield return ClickAction("PressFloor");
            yield return ClickAction("None");
            Assert.That(commits, Is.Empty);
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => states.Contains("Grace"), "grace");
            yield return null;
            yield return ClickAction("PressOpen");
            yield return ClickAction("PressFloor");
            Assert.That(commits.Count, Is.EqualTo(1));
            Assert.That(outcomes, Does.Not.Contain("Death"));
            yield return ClickAction("PressClose");
            yield return Until(() => outcomes.Contains("GraceRecovered"), "valid rescue");
        }

        [UnityTest]
        public IEnumerator SecondWrongClick_DuringGraceCausesDeath()
        {
            yield return Diagnose();
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => states.Contains("Grace"), "grace");
            yield return null;
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => outcomes.Contains("Death"), "second error death");
            Assert.That(commits.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Provocation_NoInput_CompletesDiagnosisSuccessfully()
        {
            GameAccess.Set(beat, "category", GameAccess.Enum("AnomalyCategory", "Provocation"));
            GameAccess.Set(beat, "correctAction", GameAccess.Enum("PlayerAction", "None"));
            GameAccess.Set(beat, "passiveSuccessSeconds", 0.1f);
            Begin();
            yield return Until(() => outcomes.Contains("Correct"), "provocation passive diagnosis success");
            Assert.That(commits, Is.Empty);
            Assert.That(outcomes, Does.Not.Contain("WrongRevealed"));
            Assert.That(states, Does.Contain("Resolve"));
        }

        [UnityTest]
        public IEnumerator Provocation_FirstWrongThenNoInput_RecoversDuringGrace()
        {
            GameAccess.Set(beat, "category", GameAccess.Enum("AnomalyCategory", "Provocation"));
            GameAccess.Set(beat, "correctAction", GameAccess.Enum("PlayerAction", "None"));
            GameAccess.Set(beat, "passiveSuccessSeconds", 0.2f);
            GameAccess.Set(beat, "graceSeconds", 0.15f);
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", "PressEmergencyStop"));
            yield return Diagnose();
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => states.Contains("Grace"), "provocation grace");
            yield return Until(() => outcomes.Contains("GraceRecovered"), "provocation no-input recovery");
            Assert.That(commits, Is.EqualTo(new[] { "PressEmergencyStop" }));
            Assert.That(outcomes, Does.Not.Contain("Death"));
        }

        [UnityTest]
        public IEnumerator Provocation_SecondWrongDuringGrace_CausesDeath()
        {
            GameAccess.Set(beat, "category", GameAccess.Enum("AnomalyCategory", "Provocation"));
            GameAccess.Set(beat, "correctAction", GameAccess.Enum("PlayerAction", "None"));
            GameAccess.Set(beat, "passiveSuccessSeconds", 0.3f);
            GameAccess.Set(beat, "graceSeconds", 0.3f);
            yield return Diagnose();
            yield return ClickAction("PressClose");
            yield return Until(() => states.Contains("Grace"), "provocation grace");
            yield return null;
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => outcomes.Contains("Death"), "provocation second error death");
            Assert.That(commits, Is.EqualTo(new[] { "PressClose", "PressEmergencyStop" }));
        }

        [UnityTest]
        public IEnumerator Lure_NoInputInsideCabin_RemainsSafeWithoutDeadline()
        {
            yield return Diagnose();
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(states.Last(), Is.EqualTo("Diagnosis"));
            Assert.That(commits, Is.Empty);
            Assert.That(outcomes, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Lure_CrossingThreshold_RevealsWithoutButtonInput()
        {
            yield return Diagnose();
            MovePassenger(OccupancyOrigin + Vector3.forward * 4f);
            yield return Until(() => states.Contains("Grace"), "lure threshold reveal");
            Assert.That(commits, Is.EqualTo(new[] { "ExitCab" }));
            Assert.That(outcomes, Does.Contain("WrongRevealed"));
        }

        [UnityTest]
        public IEnumerator Lure_CloseOutsideCabin_ClosesThenDiesImmediately()
        {
            MovePassenger(OccupancyOrigin + Vector3.forward * 4f);
            yield return Diagnose();
            yield return ClickAction("PressClose");
            yield return Until(() => outcomes.Contains("Death"), "outside close death");
            Assert.That(outcomes, Does.Not.Contain("WrongRevealed"));
            Assert.That(DoorMoving, Is.False);
            Assert.That(DoorOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Lure_ReturnAndCloseWithinGrace_SurvivesAfterOldDeadlineWhileDoorFinishes()
        {
            GameAccess.Set(beat, "graceSeconds", 0.1f);
            GameAccess.Set(elevator, "doorSlideSeconds", 0.25f);
            yield return Diagnose();
            MovePassenger(OccupancyOrigin + Vector3.forward * 4f);
            yield return Until(() => states.Contains("Grace"), "lure grace");
            MovePassenger(OccupancyOrigin);
            yield return null;
            yield return ClickAction("PressClose");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(outcomes, Does.Not.Contain("Death"), "Accepted close freezes the old grace deadline");
            Assert.That(outcomes, Does.Not.Contain("GraceRecovered"), "Recovery waits for door completion");
            yield return Until(() => outcomes.Contains("GraceRecovered"), "recovery after closed doors");
            Assert.That(DoorOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Lure_ObstructedRecoveryClose_ReopensAndResumesRemainingGrace()
        {
            GameAccess.Set(beat, "graceSeconds", 0.12f);
            GameAccess.Set(elevator, "doorSlideSeconds", 0.08f);
            yield return Diagnose();
            MovePassenger(OccupancyOrigin + Vector3.forward * 4f);
            yield return Until(() => states.Contains("Grace"), "lure grace");
            MovePassenger(OccupancyOrigin);
            safety.transform.position = OccupancyOrigin;
            Physics.SyncTransforms();
            yield return ClickAction("PressClose");
            yield return Until(() => !DoorMoving, "obstructed close reopens");
            Assert.That(states.Last(), Is.EqualTo("Grace"));
            Assert.That(DoorOpen, Is.True);
            Assert.That(outcomes, Does.Not.Contain("Death"), "Door processing must not consume grace");

            safety.transform.position = OccupancyOrigin + Vector3.forward * 8f;
            Physics.SyncTransforms();
            yield return ClickAction("PressClose");
            yield return Until(() => outcomes.Contains("GraceRecovered"), "fresh close within remaining grace");
        }

        [UnityTest]
        public IEnumerator Hijack_EmergencyStopBeforeDeadline_PausesThenResumesTravel()
        {
            ConfigureHijack(0.4f, 0.2f);
            GameAccess.Set(machine, "resolveSeconds", 0.1f);
            yield return Diagnose();
            Assert.That(Travelling, Is.True);
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => states.Contains("Resolve"), "hijack emergency stop");
            Assert.That(Travelling, Is.False, "Accepted emergency stop must pause travel");
            yield return Until(() => outcomes.Contains("Correct"), "hijack travel resumes");
            Assert.That(Travelling, Is.True);
            Assert.That(commits, Is.EqualTo(new[] { "PressEmergencyStop" }));
        }

        [UnityTest]
        public IEnumerator Hijack_InitialDeadline_RevealsThenEmergencyStopRecovers()
        {
            ConfigureHijack(0.08f, 0.25f);
            yield return Diagnose();
            yield return Until(() => states.Contains("Grace"), "hijack deadline reveal");
            Assert.That(commits, Is.EqualTo(new[] { "None" }));
            Assert.That(outcomes, Does.Contain("WrongRevealed"));
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => outcomes.Contains("GraceRecovered"), "hijack grace recovery");
            Assert.That(Travelling, Is.True);
        }

        [UnityTest]
        public IEnumerator Hijack_CloseThenEmergencyStopWithinGrace_Recovers()
        {
            ConfigureHijack(0.4f, 0.25f);
            yield return Diagnose();
            yield return ClickAction("PressClose");
            yield return Until(() => states.Contains("Grace"), "hijack close reveal");
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => outcomes.Contains("GraceRecovered"), "hijack correction");
            Assert.That(commits, Is.EqualTo(new[] { "PressClose", "PressEmergencyStop" }));
            Assert.That(outcomes, Does.Not.Contain("Death"));
        }

        [UnityTest]
        public IEnumerator Hijack_SecondCloseDuringGrace_CausesImmediateDeath()
        {
            ConfigureHijack(0.4f, 0.25f);
            yield return Diagnose();
            yield return ClickAction("PressClose");
            yield return Until(() => states.Contains("Grace"), "hijack grace");
            yield return ClickAction("PressClose");
            yield return Until(() => outcomes.Contains("Death"), "hijack second close death");
            Assert.That(commits, Is.EqualTo(new[] { "PressClose", "PressClose" }));
        }

        [UnityTest]
        public IEnumerator Hijack_GraceDeadlineWithoutEmergencyStop_CausesDeath()
        {
            ConfigureHijack(0.06f, 0.08f);
            yield return Diagnose();
            yield return Until(() => outcomes.Contains("Death"), "hijack grace timeout death");
            Assert.That(commits, Is.EqualTo(new[] { "None" }));
            Assert.That(outcomes, Is.EqualTo(new[] { "WrongRevealed", "Death" }));
        }

        [UnityTest]
        public IEnumerator ResolveClick_IsIgnored()
        {
            GameAccess.Set(machine, "resolveSeconds", 0.25f);
            yield return Diagnose();
            yield return ClickAction("PressClose");
            yield return Until(() => states.Contains("Resolve"), "resolve");
            yield return ClickAction("PressEmergencyStop");
            yield return Until(() => outcomes.Contains("Correct"), "resolution");
            Assert.That(commits, Is.EqualTo(new[] { "PressClose" }));
            Assert.That(outcomes, Does.Not.Contain("WrongRevealed"));
        }

        [UnityTest]
        public IEnumerator WallBetweenCameraAndButton_BlocksClick()
        {
            yield return Diagnose();
            var wall = new GameObject("OccludingWall", typeof(BoxCollider));
            wall.transform.SetParent(root.transform);
            wall.transform.position = target.transform.position - Vector3.forward * 0.5f;
            wall.transform.localScale = new Vector3(1, 1, 0.1f);
            wall.layer = 1; // Not in the interactable mask; still must occlude the target.
            Physics.SyncTransforms();
            yield return ClickAction("PressClose");
            Assert.That(commits, Is.Empty);
            wall.SetActive(false);
            Physics.SyncTransforms();
            yield return ClickAction("PressClose");
            yield return Until(() => outcomes.Contains("Correct"), "unobstructed fresh click");
        }
    }
}
