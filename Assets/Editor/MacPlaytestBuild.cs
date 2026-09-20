using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GraduationProject.EditorTools
{
    // Builds the saved full story in the existing Editor without changing its scene list.
    public static class MacPlaytestBuild
    {
        [MenuItem("Tools/Unity Agent/Build Homecoming Mac Playtest")]
        public static void RequestBuild() { EditorApplication.delayCall += Build; }

        [MenuItem("Tools/Unity Agent/Verify Homecoming Mac Source")]
        public static void VerifySource()
        {
            UnityAgentMenu.RunInputTests(new[] {
                "GraduationProject.Tests.PlayableDemoTests.Homecoming_FourNightsAndFinalNightRetry_UseInputSystem",
                "GraduationProject.Tests.PlayableDemoTests.Homecoming_SavedNight_Settings_TitleAndReload_UseInputSystem"
            });
        }

        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Requires saved Edit Mode.");
            var output = Environment.GetEnvironmentVariable("GRADUATION_MAC_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = Path.GetFullPath("artifacts/builds/mac-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Output must be absolute.");
            if (Directory.Exists(Path.Combine(output, "Homecoming.app")))
                throw new InvalidOperationException("Choose a new output folder to preserve the previous build.");
            Directory.CreateDirectory(output);
            var info = new BuildInfo();
            var settings = new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset",
                "ProjectSettings/ProjectSettings.asset", "ProjectSettings/UnityConnectSettings.asset", "ProjectSettings/Packages/com.unity.probuilder/Settings.json" }
                .Where(File.Exists).ToDictionary(f => f, File.ReadAllBytes);
            var oldArchitecture = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
            var oldBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var oldDefaultGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX);
            var oldGraphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneOSX);
            try
            {
                foreach (var file in new[] { "PerformanceTestRunInfo.json", "PerformanceTestRunSettings.json" })
                    if (File.Exists("Assets/Resources/" + file)) throw new InvalidOperationException("Review test metadata before packaging.");
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.path != "Assets/Scenes/Homecoming.unity") throw new InvalidOperationException("Open saved Homecoming first.");
                var campaign = UnityEngine.Object.FindFirstObjectByType<HomecomingCampaign>();
                if (campaign == null || !campaign.FullStory) throw new InvalidOperationException("Saved scene is not the full four-night story.");
                var authored = new SerializedObject(campaign);
                foreach (var name in new[] { "firstNight", "secondNight" })
                {
                    var beats = authored.FindProperty(name);
                    if (beats.arraySize != 3) throw new InvalidOperationException("Incomplete night: " + name);
                    for (var i = 0; i < 3; i++)
                        if (beats.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new InvalidOperationException("Missing encounter.");
                }
                var missing = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (missing != 0) throw new InvalidOperationException("Missing scripts: " + missing);
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64ARM64;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new[] { GraphicsDeviceType.Metal });
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { scene.path }, target = BuildTarget.StandaloneOSX,
                    locationPathName = Path.Combine(output, "Homecoming.app"),
                    options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport
                });
                info.status = report.summary.result.ToString();
                info.errors = report.summary.totalErrors;
                info.warnings = report.summary.totalWarnings;
                info.seconds = report.summary.totalTime.TotalSeconds;
                info.bytes = report.summary.totalSize;
                info.messages = report.steps.SelectMany(s => s.messages).Select(m => m.type + ": " + m.content).ToArray();
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Mac build failed.");
            }
            catch (Exception ex) { info.error = ex.ToString(); throw; }
            finally
            {
                File.WriteAllText(Path.Combine(output, "build.json"), JsonUtility.ToJson(info, true));
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = oldArchitecture;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, oldBackend);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, oldDefaultGraphics);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, oldGraphics);
                foreach (var pair in settings) File.WriteAllBytes(pair.Key, pair.Value);
                AssetDatabase.Refresh();
            }
        }
        [Serializable] private class BuildInfo
        {
            public string status = "Failed", error, scene = "Assets/Scenes/Homecoming.unity";
            public string unity = Application.unityVersion, architecture = "x86_64+arm64", graphics = "Metal", backend = "Mono";
            public bool development = false;
            public int errors, warnings;
            public double seconds;
            public ulong bytes;
            public string[] messages;
        }
    }
}
