using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Persistence and its menu actions live here; the scene's existing menu renderer stays shared.
public sealed partial class DemoSession
{
    private CampaignSaveStore save;
    private bool runStarted, saveErrorShown;
    private double saveInterval;
#if UNITY_EDITOR
    // Fixtures isolate their files before loading a scene; never shipped in a Player.
    internal static string TestSaveDirectory;
#endif
    private void InitializePersistence()
    {
        if (campaign == null || !campaign.FullStory) return;
#if UNITY_EDITOR
        string directory = TestSaveDirectory ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/editor-saves/Homecoming"));
#else
        string directory = Path.Combine(Application.persistentDataPath, "Homecoming");
#endif
        save = new CampaignSaveStore(directory, fov);
        if (!save.Available) return;
        var profile = save.Profile;
        sensitivity = profile.sensitivity; inverted = profile.inverted; fov = profile.fov; brightness = profile.brightness;
        player.SetLookSettings(sensitivity, inverted); view.fieldOfView = fov;
        colorAdjustments.postExposure.value = brightness; AudioListener.volume = profile.volume;
#if !UNITY_EDITOR
        if (profile.displaySet) Screen.SetResolution(profile.width, profile.height, profile.fullscreen);
#endif
        run.PlayerDied += RecordDeath;
    }

    private void TickPersistence()
    {
        if (save == null) return;
        if (runStarted && !completed && !introduction && !transition && !menuOpen)
        {
            save.Progress.elapsedSeconds += Time.unscaledDeltaTime;
            saveInterval += Time.unscaledDeltaTime;
            if (saveInterval >= 10) { saveInterval = 0; PersistSession(); }
        }
        if (save.Error != null && !saveErrorShown && runStarted)
        {
            saveErrorShown = true; SetMenu(true); ShowPause();
        }
    }

    private void CaptureSettings()
    {
        var profile = save.Profile;
        profile.sensitivity = sensitivity; profile.inverted = inverted; profile.fov = fov;
        profile.brightness = brightness; profile.volume = AudioListener.volume;
    }
    private void PersistSession()
    {
        if (save == null || !save.Available) return;
        CaptureSettings();
        if (runStarted)
        {
            save.Progress.night = campaign.CurrentNight;
            if (!save.SaveProgress()) return;
        }
        save.SaveProfile();
    }
    private void ClosePersistence()
    {
        if (save == null) return;
        PersistSession(); run.PlayerDied -= RecordDeath;
        save.Dispose(); save = null;
    }
    private void OnApplicationQuit() => PersistSession();
    private void RecordDeath()
    {
        if (!runStarted || save == null || completed) return;
        save.Progress.deaths++; PersistSession();
    }

    private void SetDisplay(bool fullscreen, int width, int height)
    {
        if (save != null)
        {
            var profile = save.Profile;
            if (width == 0) { width = profile.displaySet ? profile.width : 1920; height = profile.displaySet ? profile.height : 1080; }
            profile.displaySet = true; profile.fullscreen = fullscreen; profile.width = width; profile.height = height;
        }
        if (width > 0) Screen.SetResolution(width, height, fullscreen);
        else Screen.fullScreen = fullscreen;
        PersistSession();
    }

    private string S(string key) => GameTextCollection.Get(this, "save." + key, CampaignMenuCopy.Get(key));
    private void SaveWarning()
    {
        if (save.Error == null) return;
        Copy(S("error"), 23);
    }
    private void PersistentTitle()
    {
        if (save.Available && save.Progress.hasRun && !save.Progress.completed)
        {
            Copy(save.Progress.night == 0 ? S("introduction") : S("night") + " " + save.Progress.night + " / 3", 23);
            Button(S("continue"), () => { runStarted = true; StartCoroutine(ResumeSavedNight()); });
        }
        SaveWarning();
    }
    private void RequestNewGame()
    {
        if (!save.Available) return;
        if (save.Progress.hasRun && !save.Progress.completed)
        {
            Clear("Confirm"); Heading(S("newConfirm")); Copy(S("overwrite"));
            Button(S("new"), ChooseIntroduction); Button(S("back"), ShowTitle);
        }
        else ChooseIntroduction();
    }
    private void ChooseIntroduction()
    {
        if (!save.Profile.cleared) { StartNewGame(false); return; }
        Clear("IntroductionChoice"); Heading(S("introChoice"));
        Button(S("withIntro"), () => StartNewGame(false));
        Button(S("skipIntro"), () => StartNewGame(true));
        Button(S("back"), ShowTitle);
    }
    private void StartNewGame(bool skip)
    {
        save.Progress = new CampaignSaveStore.ProgressData { hasRun = true, night = skip ? 1 : 0 };
        if (!save.SaveProgress()) { ShowTitle(); return; }
        runStarted = true;
        if (skip) StartCoroutine(ResumeSavedNight()); else BeginOpening();
    }
    private IEnumerator ResumeSavedNight()
    {
        transition = true; Resume();
        campaign.RestoreNight(save.Progress.night);
        introduction = campaign.CurrentNight == 0;
        if (!introduction)
        {
            run.PrepareFollowingNight(); opening.PrepareFollowingNight(journey);
            journey.SetDemoNight(true); journey.RestartNightAtEntrance();
        }
        if (opening != null) yield return opening.Reveal();
        transition = false; blockedFrame = Time.frameCount;
    }
    private void PersistentPause()
    {
        SaveWarning();
        Button(S("title"), () =>
        {
            Clear("Confirm"); Heading(S("titleConfirm")); Copy(S("resumeRule")); SaveWarning();
            Button(S("title"), Restart); Button(S("back"), ShowPause);
        });
    }
    private void CompletePersistentRun()
    {
        run.HideClearMessage();
        save.Progress.completed = true; save.Progress.night = 3;
        // Persist the completed result before updating the independent best values.
        save.SaveProgress(); save.Profile.RecordClear(save.Progress); save.SaveProfile();
        Copy(S("time") + " " + FormatDuration(save.Progress.elapsedSeconds) + "\n" + S("deaths") + " " + save.Progress.deaths);
        Copy(S("bestTime") + " " + FormatDuration(save.Profile.bestSeconds) + "\n" + S("bestDeaths") + " " + save.Profile.bestDeaths, 24);
        SaveWarning(); Button(S("title"), Restart); Button(W("menu.AfterHome.4", "終了"), Quit);
    }
    private static string FormatDuration(double seconds)
    { var duration = TimeSpan.FromSeconds(Math.Floor(seconds)); return ((long)duration.TotalMinutes).ToString("00") + ":" + duration.Seconds.ToString("00"); }
}

// Shared with the additive authoring installer. Keys are stable; all displayed copy is editable.
internal static class CampaignMenuCopy
{
    internal static readonly string[][] Entries =
    {
        new[] { "continue", "続きから" }, new[] { "night", "再開する夜" }, new[] { "introduction", "再開：導入の帰宅" },
        new[] { "new", "新しく始める" }, new[] { "newConfirm", "新しく始めますか？" },
        new[] { "overwrite", "現在の進行を上書きします。自己ベストと設定は残ります。" },
        new[] { "back", "戻る" }, new[] { "title", "タイトルへ" }, new[] { "titleConfirm", "タイトルへ戻りますか？" },
        new[] { "resumeRule", "進行を保存します。続きは現在の夜のホールからです。時間と死亡数は引き継がれます。" },
        new[] { "introChoice", "導入の帰宅から始めますか？" }, new[] { "withIntro", "導入から始める" },
        new[] { "skipIntro", "導入を省略する" }, new[] { "time", "今回の時間" }, new[] { "deaths", "今回の死亡数" },
        new[] { "bestTime", "最短時間" }, new[] { "bestDeaths", "最少死亡数" },
        new[] { "settings", "設定はこのPCに保存されます。" },
        new[] { "error", "セーブを読み書きできません。保存ファイルはそのまま保持しています。\n別のゲームが起動中なら終了し、保存先の空き容量・アクセス権をご確認のうえ、ゲームを再起動してください。\nこの状態での進行や設定変更は保存されません。" }
    };
    internal static string Get(string key)
    {
        foreach (var entry in Entries) if (entry[0] == key) return entry[1];
        throw new ArgumentException("Unknown campaign copy: " + key);
    }
}
