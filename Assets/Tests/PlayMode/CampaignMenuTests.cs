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
 public partial class PlayableDemoTests
 {
  private object Save => CampaignSaveTests.Field<object>(demo,"save");
  private object Progress => CampaignSaveTests.Get(Save,"Progress");
  private double Elapsed => CampaignSaveTests.Field<double>(Progress,"elapsedSeconds");

  private IEnumerator LoadPersistentScene()
  {
   const string path="Assets/Scenes/Homecoming.unity";
   yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
   RebindPersistentScene(); yield return null;
  }
  private void RebindPersistentScene()
  {
   fixtureScene=SceneManager.GetSceneByPath("Assets/Scenes/Homecoming.unity"); SceneManager.SetActiveScene(fixtureScene);
   player=fixtureScene.GetRootGameObjects().Single(g=>g.name=="Player").transform;
   camera=player.GetComponentInChildren<Camera>(); journey=Find("NormalJourneyController"); demo=Find("DemoSession");
  }
  private IEnumerator Escape()
  {
   Press(keyboard.escapeKey,queueEventOnly:true); yield return null;
   Release(keyboard.escapeKey,queueEventOnly:true); yield return null;
  }

  [UnityTest,Timeout(120000)] public IEnumerator Homecoming_SavedNight_Settings_TitleAndReload_UseInputSystem()
  {
   // An isolated existing save is the input fixture. Every in-scene action uses UI input.
   var store=Activator.CreateInstance(GameAccess.Type("CampaignSaveStore"),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{saveDirectory,65f},null);
   var data=CampaignSaveTests.Get(store,"Progress");
   CampaignSaveTests.Set(data,"hasRun",true); CampaignSaveTests.Set(data,"night",2); CampaignSaveTests.Set(data,"deaths",3); CampaignSaveTests.Set(data,"elapsedSeconds",123d);
   Assert.That(CampaignSaveTests.Call(store,"SaveProgress"),Is.True); ((IDisposable)store).Dispose();
   yield return LoadPersistentScene();
   yield return new WaitForSecondsRealtime(.3f); Assert.That(Elapsed,Is.EqualTo(123),"Title is not timed");
   yield return Ui("はじめる"); Assert.That(Elapsed,Is.EqualTo(123));
   Assert.That(CampaignSaveTests.Field<int>(Progress,"deaths"),Is.EqualTo(3),"Unconfirmed new game must not overwrite");
   yield return Ui("戻る"); yield return Ui("続きから");
   Assert.That(Flag("IsTransitioning"),Is.True); Assert.That(Elapsed,Is.EqualTo(123),"Load fade is not timed");
   yield return Until(()=>!Flag("IsTransitioning"),"saved night revealed");
   Assert.That(StoryInt(Find("HomecomingCampaign"),"CurrentNight"),Is.EqualTo(2));
   Assert.That(Flag("IsIntroduction"),Is.False); Assert.That(State,Is.EqualTo("WaitingForCall"));
   Assert.That(player.position.z,Is.GreaterThan(-4),"Continue starts in the hall, not at the mailbox");
   yield return new WaitForSecondsRealtime(.4f); Assert.That(Elapsed,Is.GreaterThan(123.3));
   yield return Escape(); double paused=Elapsed;
   yield return new WaitForSecondsRealtime(.3f); Assert.That(Elapsed,Is.EqualTo(paused),"Pause is not timed");
   yield return Ui("設定");
   var row=UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Single(s=>s.transform.parent.name=="音量");
   var rect=(RectTransform)row.transform;
   var at=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,.25f),rect.rect.center.y)));
   InputSystem.QueueDeltaStateEvent(mouse.position,at); yield return null; yield return null;
   Press(mouse.leftButton,queueEventOnly:true); yield return null; Release(mouse.leftButton,queueEventOnly:true); yield return null;
   Assert.That(AudioListener.volume,Is.InRange(.20f,.30f)); float volume=AudioListener.volume;
   yield return Ui("戻る"); yield return Ui("タイトルへ");
   Assert.That(CampaignSaveTests.Field<int>(Progress,"night"),Is.EqualTo(2));
   var oldDemo=demo; yield return Ui("タイトルへ");
   yield return Until(()=>oldDemo==null,"scene reload to title"); RebindPersistentScene(); yield return null;
   Assert.That(Flag("IsPaused"),Is.True); Assert.That(Elapsed,Is.EqualTo(paused).Within(.1));
   Assert.That(AudioListener.volume,Is.EqualTo(volume).Within(.001));
   Assert.That(CampaignSaveTests.Field<int>(Progress,"deaths"),Is.EqualTo(3));
   ScreenCapture.CaptureScreenshot(Path.Combine(saveDirectory,"continue-title.png")); yield return null;
   yield return Ui("続きから"); yield return Until(()=>!Flag("IsTransitioning"),"second resume");
   Assert.That(StoryInt(Find("HomecomingCampaign"),"CurrentNight"),Is.EqualTo(2));
   yield return Escape(); ScreenCapture.CaptureScreenshot(Path.Combine(saveDirectory,"resumed-hall.png")); yield return null;
  }

  [UnityTest] public IEnumerator Homecoming_CorruptSave_ShowsWarningWithoutOverwriting()
  {
   Directory.CreateDirectory(saveDirectory); string path=Path.Combine(saveDirectory,"progress.json"); File.WriteAllText(path,"unknown saved data");
   yield return LoadPersistentScene();
   Assert.That(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name=="はじめる"||b.name=="続きから"),Is.False);
   Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("セーブを読み書きできません")),Is.True);
   yield return Ui("設定"); yield return Ui("戻る");
   Assert.That(File.ReadAllText(path),Is.EqualTo("unknown saved data"));
   ScreenCapture.CaptureScreenshot(Path.Combine(saveDirectory,"unreadable-title.png")); yield return null;
  }

  [UnityTest] public IEnumerator Homecoming_ClearedProfile_SkipIntroAndOverwriteKeepBests_UseInputSystem()
  {
   var store=Activator.CreateInstance(GameAccess.Type("CampaignSaveStore"),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{saveDirectory,65f},null);
   var data=CampaignSaveTests.Get(store,"Progress");
   CampaignSaveTests.Set(data,"hasRun",true); CampaignSaveTests.Set(data,"completed",true); CampaignSaveTests.Set(data,"night",3);
   CampaignSaveTests.Set(data,"deaths",2); CampaignSaveTests.Set(data,"elapsedSeconds",600d);
   Assert.That(CampaignSaveTests.Call(store,"SaveProgress"),Is.True); ((IDisposable)store).Dispose();
   yield return LoadPersistentScene(); yield return Ui("はじめる");
   ScreenCapture.CaptureScreenshot(Path.Combine(saveDirectory,"skip-choice.png")); yield return null;
   yield return Ui("導入を省略する"); yield return Until(()=>!Flag("IsTransitioning"),"skip intro reveal");
   Assert.That(Flag("IsIntroduction"),Is.False); Assert.That(StoryInt(Find("HomecomingCampaign"),"CurrentNight"),Is.EqualTo(1));
   Assert.That(CampaignSaveTests.Field<int>(Progress,"deaths"),Is.Zero); Assert.That(Elapsed,Is.LessThan(2));
   Assert.That(CampaignSaveTests.Field<double>(CampaignSaveTests.Get(Save,"Profile"),"bestSeconds"),Is.EqualTo(600));
   yield return Escape(); yield return Ui("タイトルへ"); var oldDemo=demo; yield return Ui("タイトルへ");
   yield return Until(()=>oldDemo==null,"reload"); RebindPersistentScene(); yield return null;
   yield return Ui("はじめる"); yield return Ui("新しく始める"); yield return Ui("導入から始める");
   yield return Until(()=>!Flag("IsTransitioning"),"new introduction");
   Assert.That(Flag("IsIntroduction"),Is.True); Assert.That(StoryInt(Find("HomecomingCampaign"),"CurrentNight"),Is.Zero);
   yield return new WaitForSecondsRealtime(.3f); Assert.That(Elapsed,Is.Zero,"Introduction is not timed");
   Assert.That(CampaignSaveTests.Field<int>(CampaignSaveTests.Get(Save,"Profile"),"bestDeaths"),Is.EqualTo(2));
  }

  [UnityTest,Timeout(180000)] public IEnumerator Homecoming_ContinueFinalNightToResults_UseInputSystem()
  {
   var store=Activator.CreateInstance(GameAccess.Type("CampaignSaveStore"),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{saveDirectory,65f},null);
   var data=CampaignSaveTests.Get(store,"Progress");
   CampaignSaveTests.Set(data,"hasRun",true); CampaignSaveTests.Set(data,"night",3); CampaignSaveTests.Set(data,"deaths",1); CampaignSaveTests.Set(data,"elapsedSeconds",220d);
   Assert.That(CampaignSaveTests.Call(store,"SaveProgress"),Is.True); ((IDisposable)store).Dispose();
   yield return LoadPersistentScene(); yield return Ui("続きから"); yield return Until(()=>!Flag("IsTransitioning"),"continue final night");
   yield return VerifyFourNights(Find("HomecomingCampaign"),saveDirectory,3,false);
  }
 }
}
#endif
