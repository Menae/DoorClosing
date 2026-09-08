using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GraduationProject.Tests
{
    public class NormalJourneyInputTests : InputTestFixture
    {
        private GameObject root, hall, corridor;
        private Component journey, elevator, target;
        private CharacterController body;
        private BoxCollider safety;
        private Mouse mouse;
        private float savedTimeScale;
        private static readonly Vector3 Origin = new Vector3(500, 500, 500);
        private string State => journey.GetType().GetProperty("State").GetValue(journey).ToString();
        private int Stops => (int)journey.GetType().GetProperty("AcceptedEmergencyStops").GetValue(journey);

        public override void Setup()
        {
            base.Setup();
            savedTimeScale = Time.timeScale; Time.timeScale = 1;
            mouse = InputSystem.AddDevice<Mouse>();
            root = new GameObject("NormalJourneyFixture"); root.SetActive(false);
            hall = new GameObject("Hall"); hall.transform.SetParent(root.transform);
            corridor = new GameObject("Corridor"); corridor.transform.SetParent(root.transform);
            elevator = root.AddComponent(GameAccess.Type("ElevatorController"));
            GameAccess.Set(elevator, "doorSlideSeconds", 0.1f);
            var bodyObject = new GameObject("Body"); bodyObject.transform.SetParent(root.transform);
            body = bodyObject.AddComponent<CharacterController>(); body.height = 1.8f; body.radius = 0.25f;
            bodyObject.transform.position = Origin + Vector3.right * 5;
            var cabinObject = new GameObject("Cabin"); cabinObject.transform.SetParent(root.transform); cabinObject.transform.position = Origin;
            var cabin = cabinObject.AddComponent<BoxCollider>(); cabin.size = Vector3.one * 3; cabin.isTrigger = true;
            var safetyObject = new GameObject("Safety"); safetyObject.transform.SetParent(root.transform); safetyObject.transform.position = Origin + Vector3.forward * 3;
            safety = safetyObject.AddComponent<BoxCollider>(); safety.size = new Vector3(2, 3, 0.5f); safety.isTrigger = true;
            GameAccess.Set(elevator, "doorSafetyZone", safety); GameAccess.Set(elevator, "passengerBody", body);
            journey = root.AddComponent(GameAccess.Type("NormalJourneyController"));
            GameAccess.Set(journey, "elevator", elevator); GameAccess.Set(journey, "passenger", body); GameAccess.Set(journey, "cabin", cabin);
            GameAccess.Set(journey, "entranceHall", hall); GameAccess.Set(journey, "homeCorridor", corridor);
            GameAccess.Set(journey, "travelSeconds", 0.7f); GameAccess.Set(journey, "emergencyStopSeconds", 0.3f);
            var cameraObject = new GameObject("RayCamera", typeof(Camera)); cameraObject.transform.SetParent(root.transform); cameraObject.transform.position = Origin;
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            var button = new GameObject("Button", typeof(BoxCollider)); button.transform.SetParent(root.transform); button.transform.position = Origin + Vector3.forward;
            button.transform.localScale = Vector3.one * 0.25f;
            target = button.AddComponent(GameAccess.Type("Interactable"));
            var raycaster = root.AddComponent(GameAccess.Type("InteractionRaycaster"));
            GameAccess.Set(raycaster, "playerCamera", camera); GameAccess.Set(raycaster, "normalJourney", journey); GameAccess.Set(raycaster, "interactableLayers", (LayerMask)1);
            root.SetActive(true); Physics.SyncTransforms();
        }

        public override void TearDown()
        {
            try { if (root != null) Object.DestroyImmediate(root); Time.timeScale = savedTimeScale; }
            finally { base.TearDown(); }
        }

        private static IEnumerator Until(Func<bool> condition, string label)
        {
            float end = Time.realtimeSinceStartup + 4;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, label);
        }

        private IEnumerator Click(string action, int floor = -1)
        {
            GameAccess.Set(target, "actionType", GameAccess.Enum("PlayerAction", action)); GameAccess.Set(target, "floorNumber", floor);
            Press(mouse.leftButton, queueEventOnly: true); yield return null;
            Release(mouse.leftButton, queueEventOnly: true); yield return null;
        }

        private void MoveBody(Vector3 position)
        {
            body.enabled = false; body.transform.position = position; body.enabled = true; Physics.SyncTransforms();
        }

        private IEnumerator Board()
        {
            yield return null;
            yield return Click("PressOpen");
            yield return new WaitForSeconds(0.15f);
            Assert.That(State, Is.EqualTo("Boarding"));
            MoveBody(Origin);
        }

        [UnityTest]
        public IEnumerator NormalJourney_ClickPath_ReachesHome_WithSafeEmergencyStop()
        {
            yield return Board();
            yield return Click("PressFloor", 7);
            Assert.That(State, Is.EqualTo("Boarding"));
            yield return Click("PressFloor", 8);
            yield return Until(() => State == "Travelling", "travelling");
            yield return null;
            yield return Click("PressEmergencyStop");
            Assert.That(State, Is.EqualTo("EmergencyStopped"));
            yield return Click("PressEmergencyStop");
            Assert.That(Stops, Is.EqualTo(1), "No repeat acceptance while stopped");
            yield return Click("PressOpen");
            yield return Until(() => State == "Travelling", "travel resumes after normal stop");
            yield return null;
            yield return Click("PressEmergencyStop");
            Assert.That(State, Is.EqualTo("EmergencyStopped"));
            Assert.That(Stops, Is.EqualTo(2), "A fresh stop is accepted after travel resumes");
            yield return Until(() => State == "Arrived", "safe arrival");
            Assert.That(hall.activeSelf, Is.False); Assert.That(corridor.activeSelf, Is.True);
            yield return null;
            yield return Click("PressFloor", 8);
            Assert.That(State, Is.EqualTo("Arrived"));
            yield return Click("PressClose");
            yield return Until(() => State == "Arrived", "safe genuine-floor representation");
            Assert.That(Stops, Is.EqualTo(2));
            MoveBody(Origin + Vector3.right * 5);
            yield return null;
            yield return Click("TouchHomeDoor");
            Assert.That(State, Is.EqualTo("Complete"));
        }

        [UnityTest]
        public IEnumerator Floor8OutsideCabin_IsIgnored_AndCloseObstructionReopens()
        {
            yield return null;
            yield return Click("PressOpen");
            yield return new WaitForSeconds(0.15f);
            yield return Click("PressFloor", 8);
            Assert.That(State, Is.EqualTo("Boarding"));
            MoveBody(safety.transform.position);
            yield return Click("PressClose");
            yield return Until(() => State == "Boarding", "obstruction returns to boarding");
            Assert.That((bool)elevator.GetType().GetProperty("LastCloseObstructed").GetValue(elevator), Is.True);
            Assert.That((bool)elevator.GetType().GetProperty("IsDoorOpen").GetValue(elevator), Is.True);
            MoveBody(Origin);
            yield return null;
            yield return Click("PressFloor", 8);
            yield return Until(() => State == "Arrived", "fresh attempt after obstruction arrives");
        }

        [UnityTest]
        public IEnumerator EnteringDoorwayDuringClosing_CancelsDeparture()
        {
            GameAccess.Set(elevator, "doorSlideSeconds", 0.3f);
            yield return null;
            yield return Click("PressOpen");
            yield return new WaitForSeconds(0.35f);
            MoveBody(Origin);
            yield return Click("PressFloor", 8);
            Assert.That(State, Is.EqualTo("Closing"));
            MoveBody(safety.transform.position);
            yield return Until(() => State == "Boarding", "mid-close obstruction cancels departure");
            Assert.That(hall.activeSelf, Is.True); Assert.That(corridor.activeSelf, Is.False);
        }
    }
}
