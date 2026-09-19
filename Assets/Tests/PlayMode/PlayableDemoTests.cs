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
   if(!predicate()) ScreenCapture.CaptureScreenshot("artifacts/feedback-01/demo-timeout.png");
   Assert.That(predicate(),Is.True,message+" timeScale="+Time.timeScale+" paused="+(demo!=null && Flag("IsPaused")));
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


  private void Author(string key, string value)
  {
   foreach (var root in fixtureScene.GetRootGameObjects())
   foreach (var component in root.GetComponentsInChildren(GameAccess.Type("GameTextCollection"), true))
   {
    var so = new UnityEditor.SerializedObject(component); var entries = so.FindProperty("entries");
    for (int i = 0; i < entries.arraySize; i++)
    {
     var entry = entries.GetArrayElementAtIndex(i);
     if (entry.FindPropertyRelative("key").stringValue != key) continue;
     entry.FindPropertyRelative("value").stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); return;
    }
   }
   Assert.Fail("Missing authored entry: " + key);
  }
  [UnityTest] public IEnumerator AuthoredMenu_BlankLongAndRenamedButtonRemainUsable()
  {
   yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/PlayableDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
   fixtureScene=SceneManager.GetSceneByPath("Assets/Scenes/PlayableDemo.unity"); SceneManager.SetActiveScene(fixtureScene);
   demo=Find("DemoSession"); journey=Find("NormalJourneyController");
   Author("menu.ShowTitle.1", "執筆したタイトル");
   Author("menu.ShowTitle.2", string.Join("\n", Enumerable.Repeat("長文の執筆内容が最後まで読めることを確認します。", 35)));
   Author("menu.ShowTitle.4", "入館する");
   var redraw=demo.GetType().GetMethod("ShowTitle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
   redraw.Invoke(demo,null); yield return null; yield return null;
   var scroll=UnityEngine.Object.FindFirstObjectByType<ScrollRect>();
   Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height),"Long copy must scroll instead of losing its controls");
   scroll.verticalNormalizedPosition=0; yield return null;
   var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name=="入館する");
   Assert.That(RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, RectTransformUtility.WorldToScreenPoint(null,button.transform.position)),Is.True);
   Author("menu.ShowTitle.2", ""); redraw.Invoke(demo,null); yield return null; yield return null;
   var captions=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None);
   Assert.That(captions.Any(t=>t.text=="執筆したタイトル"),Is.True);
   Assert.That(captions.Any(t=>t.text.Contains("あなたの自宅は")),Is.False,"Empty authored text must not restore defaults");
   Author("floor.format", "{broken");
   var floor=Find("FloorIndicator");
   Assert.DoesNotThrow(()=>floor.GetType().GetMethod("SetFloor").Invoke(floor,new object[]{1}));
   yield return Ui("入館する"); Assert.That(Flag("IsPaused"),Is.False); Assert.That(State,Is.EqualTo("WaitingForCall"));
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

  [UnityTest] public IEnumerator Homecoming_PostboxKeypadGateAndContinuousNight_UseInputSystem()
  {
   const string path = "Assets/Scenes/Homecoming.unity";
   yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
   fixtureScene=SceneManager.GetSceneByPath(path); SceneManager.SetActiveScene(fixtureScene);
   player=fixtureScene.GetRootGameObjects().Single(g=>g.name=="Player").transform;
   camera=player.GetComponentInChildren<Camera>(); journey=Find("NormalJourneyController"); demo=Find("DemoSession");
   var entrance=Find("EntranceAccessController");
   bool EntryFlag(string name)=>(bool)entrance.GetType().GetProperty(name).GetValue(entrance);
   string evidence=Path.GetFullPath("artifacts/opening-02/input-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(evidence);
   yield return null;
   Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("初日は何も")),Is.False);
   yield return Ui("はじめる");
   Press(keyboard.escapeKey,queueEventOnly:true); yield return null; Release(keyboard.escapeKey,queueEventOnly:true); yield return null;
   Assert.That(Flag("IsPaused"),Is.True,"Opening fade must allow pause");
   yield return new WaitForSecondsRealtime(.2f);
   Assert.That(Flag("IsTransitioning"),Is.True,"Paused fade must not advance");
   yield return Ui("再開"); yield return Until(()=>!Flag("IsTransitioning"),"opening fade");
   var boxes=fixtureScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).Where(t=>t.name.EndsWith("_郵便受け")).ToArray();
   Assert.That(boxes.Length,Is.EqualTo(54),"One mailbox for every approved apartment");
   foreach(int floor in Enumerable.Range(2,9)) foreach(int unit in Enumerable.Range(1,6))
    Assert.That(boxes.Count(t=>t.name==(floor*100+unit)+"_郵便受け"),Is.EqualTo(1));
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"01-entrance.png"));
   yield return WalkTo(new Vector3(1.48f,.95f,-6.2f));
   yield return ClickAt(Control("EntranceKey_8")); yield return ClickAt(Control("EntranceKey_0")); yield return ClickAt(Control("EntranceKey_5"));
   Assert.That(EntryFlag("Unlocked"),Is.False,"Number alone cannot bypass examining own mailbox");
   Assert.That(entrance.GetType().GetProperty("EnteredNumber").GetValue(entrance),Is.EqualTo(""));
   yield return WalkTo(new Vector3(0,.95f,-6.3f)); yield return Aim(new Vector3(0,.95f,-3.5f),true);
   Press(keyboard.wKey,queueEventOnly:true); yield return new WaitForSeconds(.8f); Release(keyboard.wKey,queueEventOnly:true); yield return null;
   Assert.That(player.position.z,Is.LessThan(-5.1f),"Closed glass doors physically block entry");
   var mailbox=Control("805_郵便受け");
   yield return WalkTo(new Vector3(-2.05f,.95f,mailbox.z)); yield return Aim(mailbox);
   yield return new WaitForSeconds(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"02-postbox-marker.png"));
   var marker=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Single(t=>t.name=="805_郵便受け_▲");
   Assert.That(marker.color.a,Is.GreaterThan(.9f)); Assert.That(marker.transform.localScale.x,Is.GreaterThan(1.1f),"Focused target uses shape/size as well as color");
   Assert.That(marker.transform.position.y,Is.LessThan(camera.WorldToScreenPoint(mailbox+Vector3.up*.056f).y),"Mailbox cue must stay in its own row, not point at 905");
   Vector2 initialMarkPosition=marker.rectTransform.anchoredPosition;
   yield return new WaitForSeconds(.6f);
   Assert.That(Vector2.Distance(initialMarkPosition,marker.rectTransform.anchoredPosition),Is.GreaterThan(.05f),"A stationary view has a subtle floating cue");
   yield return Aim(mailbox+new Vector3(0,.28f,0)); yield return new WaitForSeconds(.3f);
   Assert.That(marker.transform.localScale.x,Is.EqualTo(1).Within(.02f),"Unfocused near target remains discoverable without focus emphasis");
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"02b-near-marker.png"));
   Press(keyboard.escapeKey,queueEventOnly:true); yield return null; Release(keyboard.escapeKey,queueEventOnly:true); yield return null;
   Assert.That(marker.gameObject.activeSelf,Is.False,"No world prompts over pause menu");
   yield return Ui("再開");
   yield return ClickAt(Control("805_郵便受け"));
   Assert.That(EntryFlag("MailboxInspected"),Is.True);
   yield return new WaitForSeconds(.5f); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"03-postbox-open.png"));
   Assert.That(marker.gameObject.activeSelf,Is.False,"Examined mailbox no longer advertises an action");
   yield return WalkTo(new Vector3(1.48f,.95f,-6.2f));
   yield return Aim(Control("EntranceKey_5")); yield return new WaitForSeconds(.3f);
   Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("EntranceKey_")&&t.name.EndsWith("_▲")),Is.EqualTo(1),"One grouped cue, not eleven overlapping triangles");
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"03b-keypad-marker.png"));
   foreach(string n in new[]{"9","9","9"}) yield return ClickAt(Control("EntranceKey_"+n));
   Assert.That(EntryFlag("Unlocked"),Is.False,"Wrong room number must not open entrance");
   yield return new WaitForSeconds(.8f);
   yield return ClickAt(Control("EntranceKey_8")); yield return ClickAt(Control("EntranceKey_C"));
   Assert.That(entrance.GetType().GetProperty("EnteredNumber").GetValue(entrance),Is.EqualTo(""),"Cancel clears entry");
   foreach(string n in new[]{"8","0","5"}) yield return ClickAt(Control("EntranceKey_"+n));
   Assert.That(EntryFlag("Unlocked"),Is.True);
   yield return Until(()=>EntryFlag("DoorOpen"),"entrance opens");
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"04-unlocked.png"));
   yield return WalkTo(new Vector3(0,.95f,-6.2f)); yield return WalkTo(new Vector3(0,.95f,-3.5f));
   yield return Aim(new Vector3(-2.55f,1.75f,1.85f)); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"05-hall.png"));
   yield return WalkTo(new Vector3(0,.95f,.5f)); yield return ClickAt(new Vector3(1.25f,1.5f,1.82f));
   yield return WaitState("Boarding"); yield return WaitForDoors(); yield return WalkTo(new Vector3(0,.95f,3.6f));
   yield return ClickAt(Control("Floor8")); yield return WaitState("Arrived");
   yield return Aim(new Vector3(0,1.6f,-10)); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"06-normal-corridor.png"));
   yield return WalkTo(new Vector3(0,.95f,-10.7f)); yield return ClickAt(new Vector3(0,1.35f,-11.85f));
   yield return Until(()=>!Flag("IsIntroduction")&&!Flag("IsTransitioning"),"continuous next night",20);
   Assert.That(Flag("IsPaused"),Is.False,"No next-night explanation/menu interrupt");
   Assert.That(player.position.z,Is.GreaterThan(-2),"Following night begins in the hall, no repeated postbox chore");
   ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"07-following-night.png"));
   yield return WalkTo(new Vector3(0,.95f,.5f)); yield return ClickAt(new Vector3(1.25f,1.5f,1.82f));
   yield return WaitState("Boarding"); yield return WaitForDoors(); yield return WalkTo(new Vector3(0,.95f,3.6f));
   yield return ClickAt(Control("Floor8")); yield return Until(()=>Find("LureAnomaly")!=null,"first anomaly",40);
   yield return Until(()=>Find("BeatStateMachine").GetType().GetField("currentState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(Find("BeatStateMachine")).ToString()=="Diagnosis","lure diagnosis");
   yield return Aim(new Vector3(0,1.6f,-6)); ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"08-first-lure.png"));
   yield return ClickAt(Control("SideRightClose")); yield return Until(()=>Find("ProvocationAnomaly")!=null,"Lure still rejects correctly",20);
   File.WriteAllText(Path.Combine(evidence,"context.txt"),"Homecoming: synthetic Input System Keyboard/Mouse -> world/UI raycast clicks; no teleport/SubmitAction. "+Screen.width+"x"+Screen.height);
  }
 }
}
#endif
