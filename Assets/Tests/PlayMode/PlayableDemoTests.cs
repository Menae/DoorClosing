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
using UnityEngine.UI;
namespace GraduationProject.Tests
{
 public class PlayableDemoTests : InputTestFixture
 {
  private Scene fixtureScene;
  private Mouse mouse;
  private Keyboard keyboard;
  private Transform player;
  private Camera camera;
  private Component journey, demo;
  private string State => journey.GetType().GetProperty("State").GetValue(journey).ToString();
  private bool Flag(string name) => (bool)demo.GetType().GetProperty(name).GetValue(demo);
  public override void Setup() { base.Setup(); mouse=InputSystem.AddDevice<Mouse>(); keyboard=InputSystem.AddDevice<Keyboard>(); }
  [UnityTearDown] public IEnumerator Cleanup()
  {
   var fallback=SceneManager.CreateScene("DemoTestCleanup"); SceneManager.SetActiveScene(fallback);
   if(fixtureScene.IsValid() && fixtureScene.isLoaded) yield return SceneManager.UnloadSceneAsync(fixtureScene);
  }
  private IEnumerator Ui(string label)
  {
   yield return null;
   var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==label);
   var rect=(RectTransform)button.transform;
   InputSystem.QueueDeltaStateEvent(mouse.position,RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
   yield return null; yield return null;
   Press(mouse.leftButton,queueEventOnly:true); yield return null;
   Release(mouse.leftButton,queueEventOnly:true); yield return null; yield return null; yield return null;
  }
  private IEnumerator Until(Func<bool> predicate,string message,float timeout=15)
  {
   float end=Time.realtimeSinceStartup+timeout;
   while(!predicate()&&Time.realtimeSinceStartup<end) yield return null;
   Assert.That(predicate(),Is.True,message);
  }
  private Component Find(string type) => UnityEngine.Object.FindFirstObjectByType(GameAccess.Type(type)) as Component;
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

        private IEnumerator WaitState(string expected)
        {
            float end = Time.realtimeSinceStartup + (expected == "Arrived" ? 32 : 8);
            while (State != expected && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(State, Is.EqualTo(expected));
            yield return null;
        }


  [UnityTest] public IEnumerator MenuIntroductionThreeEncountersAndReplay_UseInputSystem()
  {
   yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/PlayableDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
   fixtureScene=SceneManager.GetSceneByPath("Assets/Scenes/PlayableDemo.unity"); SceneManager.SetActiveScene(fixtureScene);
   player=fixtureScene.GetRootGameObjects().Single(g=>g.name=="Player").transform;
   camera=player.GetComponentInChildren<Camera>(); journey=Find("NormalJourneyController"); demo=Find("DemoSession");
   string evidence=Path.GetFullPath("artifacts/demo-02/input-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(evidence);
   yield return null; ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"title.png"));
   Assert.That(Flag("IsPaused"),Is.True); Assert.That(Time.timeScale,Is.Zero);
   var before=player.position;
   Press(keyboard.wKey,queueEventOnly:true); yield return new WaitForSecondsRealtime(.2f); Release(keyboard.wKey,queueEventOnly:true); yield return null;
   Assert.That(player.position,Is.EqualTo(before));
   yield return Ui("設定"); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"settings.png"));
   yield return Ui("戻る"); yield return Ui("はじめる"); Assert.That(Flag("IsPaused"),Is.False);
   Press(keyboard.escapeKey,queueEventOnly:true); yield return null; Release(keyboard.escapeKey,queueEventOnly:true); yield return null;
   Assert.That(Flag("IsPaused"),Is.True); Assert.That(Time.timeScale,Is.Zero);
   yield return Ui("再開"); Assert.That(Flag("IsPaused"),Is.False);
   Assert.That(State,Is.EqualTo("WaitingForCall"),"Menu click must not call the elevator");
   yield return WalkTo(new Vector3(2.12f,.95f,1.15f)); yield return Aim(new Vector3(2.12f,1.68f,1.875f));
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"rules.png")); yield return null;
   yield return WalkTo(new Vector3(0,.95f,.5f)); yield return ClickAt(new Vector3(1.25f,1.5f,1.82f));
   yield return WaitState("Boarding"); yield return WaitForDoors(); yield return WalkTo(new Vector3(0,.95f,3.6f));
   yield return ClickAt(Control("Floor8")); yield return WaitState("Arrived");
   yield return Aim(new Vector3(0,1.6f,-10)); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"baseline.png"));
   yield return WalkTo(new Vector3(0,.95f,-10.7f)); yield return ClickAt(new Vector3(0,1.35f,-11.85f));
   yield return Until(()=>!Flag("IsIntroduction")&&Flag("IsPaused"),"Normal home must lead to next night");
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"next-night.png")); yield return Ui("夜を始める");
   Assert.That(State,Is.EqualTo("WaitingForCall"));
   yield return WalkTo(new Vector3(0,.95f,.5f)); yield return ClickAt(new Vector3(1.25f,1.5f,1.82f));
   yield return WaitState("Boarding"); yield return WaitForDoors(); yield return WalkTo(new Vector3(0,.95f,3.6f));
   yield return ClickAt(Control("Floor8"));
   yield return Until(()=>Find("LureAnomaly")!=null,"lure",40);
   yield return Until(()=>Find("BeatStateMachine").GetType().GetField("currentState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Find("BeatStateMachine")).ToString()=="Diagnosis","lure diagnosis");
   Press(keyboard.escapeKey,queueEventOnly:true); yield return null; Release(keyboard.escapeKey,queueEventOnly:true);yield return null;
   Assert.That(Flag("IsPaused"),Is.True);Assert.That(AudioListener.pause,Is.True);
   yield return new WaitForSecondsRealtime(.3f);
   yield return Ui("再開");Assert.That(AudioListener.pause,Is.False);
   // A deliberate first error and second error must restart the anomaly night, never the introduction.
   yield return ClickAt(Control("SideRightEmergency"));
   yield return Until(()=>Find("BeatStateMachine").GetType().GetField("currentState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Find("BeatStateMachine")).ToString()=="Grace","lure grace");
   yield return ClickAt(Control("SideRightEmergency"));
   yield return Until(()=>State=="WaitingForCall","death returns to entrance",20);
   Assert.That(Flag("IsIntroduction"),Is.False);Assert.That(Flag("IsPaused"),Is.False);
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"death-retry.png"));
   yield return WalkTo(new Vector3(0,.95f,.5f));yield return ClickAt(new Vector3(1.25f,1.5f,1.82f));
   yield return WaitState("Boarding");yield return WaitForDoors();yield return WalkTo(new Vector3(0,.95f,3.6f));
   yield return ClickAt(Control("Floor8"));yield return Until(()=>Find("LureAnomaly")!=null,"retry lure",40);
   yield return Until(()=>Find("BeatStateMachine").GetType().GetField("currentState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Find("BeatStateMachine")).ToString()=="Diagnosis","retry diagnosis");
   yield return ClickAt(Control("SideRightClose"));
   yield return Until(()=>Find("ProvocationAnomaly")!=null,"provocation");
   yield return Until(()=>Find("HijackAnomaly")!=null,"hijack",25);
   yield return Until(()=>Find("BeatStateMachine").GetType().GetField("currentState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Find("BeatStateMachine")).ToString()=="Diagnosis","hijack diagnosis");
   yield return Until(()=>(int)Find("FloorIndicator").GetType().GetProperty("CurrentDisplayedFloor").GetValue(Find("FloorIndicator"))>8,"hijack floor cue");
   yield return ClickAt(Control("SideRightEmergency"));
   yield return WaitState("Arrived"); yield return WalkTo(new Vector3(0,.95f,-10.7f)); yield return ClickAt(new Vector3(0,1.35f,-11.85f));
   yield return Until(()=>Flag("IsComplete"),"demo completion"); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"complete.png"));
   yield return Ui("最初から遊ぶ"); yield return null; yield return null;
   yield return Until(()=>Find("DemoSession")!=null&&Find("DemoSession")!=demo,"replay scene");
   demo=Find("DemoSession"); fixtureScene=SceneManager.GetSceneByPath("Assets/Scenes/PlayableDemo.unity");
   Assert.That(Flag("IsIntroduction"),Is.True); Assert.That(Flag("IsPaused"),Is.True);
   File.WriteAllText(Path.Combine(evidence,"context.txt"),"Input System synthetic mouse/keyboard, real menu raycasts and world raycast clicks. No direct gameplay actions or teleport. "+Screen.width+"x"+Screen.height);
  }
 }
}
#endif
