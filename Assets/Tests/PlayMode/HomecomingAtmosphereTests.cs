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
    public partial class PlayableDemoTests
    {
        [UnityTest]
        public IEnumerator Homecoming_CreditsLongCopyFadesAndReturnsAutomatically()
        {
            yield return LoadPersistentScene();
            var credits = Find("HomecomingCredits"); var so = new UnityEditor.SerializedObject(credits);
            so.FindProperty("secondsPerPage").floatValue = 1;
            so.FindProperty("fadeSeconds").floatValue = .15f;
            var pages = so.FindProperty("pages"); pages.arraySize = 2;
            pages.GetArrayElementAtIndex(0).FindPropertyRelative("body").stringValue = string.Join("\n", Enumerable.Repeat("作者の長いスタッフロール", 40));
            pages.GetArrayElementAtIndex(1).FindPropertyRelative("heading").stringValue = "最後のページ";
            so.ApplyModifiedPropertiesWithoutUndo();
            // Presentation-only fixture; the result->credits input path is tested after a real final night.
            demo.GetType().GetMethod("ShowCredits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(demo, null);
            yield return new WaitForSecondsRealtime(.3f);
            var skip = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b => b.name == "タイトルへ");
            var rect = (RectTransform)skip.transform;
            var screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Assert.That(screen.x, Is.InRange(0, Screen.width)); Assert.That(screen.y, Is.InRange(0, Screen.height));
            Assert.That(skip.GetComponentInParent<UnityEngine.UI.ScrollRect>(), Is.Null, "Long copy cannot scroll the skip control offscreen");
            var fade = UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None).Single(g => g.name == "内容");
            Assert.That(fade.alpha, Is.EqualTo(1).Within(.01f));
            yield return Until(() => UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.text == "最後のページ"), "credits second page", 8);
            var old = demo; yield return Until(() => old == null, "credits auto title", 8); RebindPersistentScene();
            Assert.That(Flag("IsPaused"), Is.True);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Homecoming_ExitAndStairLoopsPreserveInputAndRoomTone()
        {
            const string path = "Assets/Scenes/Homecoming.unity";
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            fixtureScene = SceneManager.GetSceneByPath(path); SceneManager.SetActiveScene(fixtureScene);
            player = fixtureScene.GetRootGameObjects().Single(r => r.name == "Player").transform;
            camera = player.GetComponentInChildren<Camera>(); journey = Find("NormalJourneyController"); demo = Find("DemoSession");
            string evidence = Path.GetFullPath("artifacts/m5-01/input-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(evidence);
            yield return Ui("はじめる"); yield return Until(() => !Flag("IsPaused"), "start"); yield return new WaitForSeconds(1.3f);
            var tone = Find("ApartmentRoomTone"); Assert.That(tone, Is.Not.Null);
            var sounds = tone.GetComponentsInChildren<AudioSource>();
            Assert.That(sounds.Single(s => s.name == "入口の換気口").isPlaying, Is.True);
            Assert.That(sounds.Single(s => s.name == "廊下の設備室").isPlaying, Is.False, "Hidden destination cannot leak audio into entrance");
            Assert.That(player.GetComponents<AudioSource>().Where(s => s.loop).All(s => !s.isPlaying), Is.True, "No duplicated listener-following ventilation");

            var loops = fixtureScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(GameAccess.Type("HallwayLoop"), true)).Cast<Component>().ToArray();
            var exit = loops.Single(l => l.name.StartsWith("退出通路"));
            yield return TraversePassage(exit, false, evidence);
            yield return TraversePassage(exit, true, evidence);
            Assert.That(State, Is.EqualTo("WaitingForCall"), "An exit loop must not start or clear the journey");
            Assert.That(Flag("IsIntroduction"), Is.True);

            var mailbox = Control("805_郵便受け");
            yield return WalkTo(new Vector3(-2.05f, .95f, -9.0f)); yield return WalkTo(new Vector3(-2.05f, .95f, mailbox.z));
            yield return ClickAt(mailbox); yield return WalkTo(new Vector3(1.48f, .95f, -6.2f));
            foreach (var digit in new[] { "8", "0", "5" }) yield return ClickAt(Control("EntranceKey_" + digit));
            yield return Until(() => (bool)Find("EntranceAccessController").GetType().GetProperty("DoorOpen").GetValue(Find("EntranceAccessController")), "autolock opens");
            yield return WalkTo(new Vector3(0, .95f, -6.2f)); yield return WalkTo(new Vector3(0, .95f, -2.8f));
            var stairs = loops.Single(l => l.name.StartsWith("階段通路")); yield return TraversePassage(stairs, true, evidence);
            yield return WalkTo(new Vector3(0, .95f, -2.8f));
            yield return Aim(new Vector3(1.25f, 1.5f, 1.82f));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "returned-hall.png")); yield return null;
            yield return WalkTo(new Vector3(0, .95f, .5f)); yield return ClickAt(new Vector3(1.25f, 1.5f, 1.82f));
            yield return WaitState("Boarding"); yield return WaitForDoors(); yield return WalkTo(new Vector3(0, .95f, 3.6f));
            yield return new WaitForSeconds(1);
            Assert.That(sounds.Single(s => s.name == "かごの天井").isPlaying, Is.True);
            Assert.That(sounds.Single(s => s.name == "入口の換気口").isPlaying, Is.False);
            yield return ClickAt(Control("Floor8")); yield return WaitState("Arrived");
            yield return WalkTo(new Vector3(0, .95f, -4)); yield return new WaitForSeconds(1);
            Assert.That(sounds.Single(s => s.name == "廊下の設備室").isPlaying, Is.True);
            Assert.That(sounds.Single(s => s.name == "ホールの換気口").isPlaying, Is.False);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "normal-corridor.png")); yield return null;
            yield return WalkTo(new Vector3(0, .95f, -10.7f)); yield return ClickAt(new Vector3(0, 1.35f, -11.85f));
            yield return Until(() => !Flag("IsIntroduction") && State == "WaitingForCall", "following night", 20);
            File.WriteAllText(Path.Combine(evidence, "context.txt"), "Homecoming; Input System keyboard/mouse, world raycast clicks, real CharacterController movement. Exit twice (walk/sprint), stair access once, pause, view pitch continuity, fixed room audio, normal return and next night. " + Screen.width + "x" + Screen.height);
        }

        private IEnumerator TraversePassage(Component loop, bool sprint, string evidence)
        {
            var root = loop.transform;
            int Count() => (int)loop.GetType().GetProperty("Traversals").GetValue(loop);
            int initial = Count();
            yield return WalkTo(root.TransformPoint(new Vector3(0, .95f, -.65f)));
            yield return Aim(root.TransformPoint(new Vector3(0, 1.6f, 2)));
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, loop.name + "-approach-" + initial + ".png")); yield return null;
            yield return WalkTo(root.TransformPoint(new Vector3(0, .95f, 3)));
            yield return WalkTo(root.TransformPoint(new Vector3(1.4f, .95f, 3)));
            yield return Aim(root.TransformPoint(new Vector3(4, 1.8f, 3)));
            float pitch = camera.transform.localEulerAngles.x;
            Press(keyboard.escapeKey, queueEventOnly: true); yield return null; Release(keyboard.escapeKey, queueEventOnly: true); yield return null;
            var pausedAt = player.position; Press(keyboard.wKey, queueEventOnly: true); yield return new WaitForSecondsRealtime(.3f);
            Release(keyboard.wKey, queueEventOnly: true); yield return null;
            Assert.That(player.position, Is.EqualTo(pausedAt)); Assert.That(Count(), Is.EqualTo(initial));
            yield return Ui("再開");
            if (sprint) Press(keyboard.leftShiftKey, queueEventOnly: true);
            Press(keyboard.wKey, queueEventOnly: true);
            yield return Until(() => Count() == initial + 1, "passage crossing", 5);
            Release(keyboard.wKey, queueEventOnly: true); Release(keyboard.leftShiftKey, queueEventOnly: true); yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(pitch, camera.transform.localEulerAngles.x)), Is.LessThan(.01f), "Loop preserves look pitch");
            Assert.That(Vector3.Dot(player.forward, root.right), Is.LessThan(-.95f), "Forward movement connects to returning half");
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, loop.name + "-joined-" + initial + ".png")); yield return null;
            yield return WalkTo(root.TransformPoint(new Vector3(0, .95f, 3)));
            yield return WalkTo(root.TransformPoint(new Vector3(0, .95f, -.65f)));
            Assert.That(Count(), Is.EqualTo(initial + 1), "One crossing cannot bounce repeatedly");
        }
    }
}
#endif
