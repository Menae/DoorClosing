using System;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationProject.EditorTools
{
    [InitializeOnLoad]
    public static class UnityAgentMenu
    {
        private const string ResultKey = "GraduationProject.UnityAgent.TestResults";
        private static readonly TestRunnerApi Runner;
        private const string ArchiveKey = "GraduationProject.UnityAgent.PendingArchive";
        private const string ExistingFilesKey = "GraduationProject.UnityAgent.ExistingTestFiles";
        private static readonly string[] TestMetadata = {
            "Assets/Resources/PerformanceTestRunInfo.json",
            "Assets/Resources/PerformanceTestRunSettings.json"
        };
        private static readonly string[] BuildSettingsFiles = {
            "Assets/Settings/PC_RPAsset.asset",
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/UnityConnectSettings.asset",
            "ProjectSettings/Packages/com.unity.probuilder/Settings.json"
        };
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static UnityAgentMenu()
        {
            Runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            Runner.RegisterCallbacks(new Results());
            EditorApplication.update += ArchiveGeneratedTestMetadata;
            EditorApplication.delayCall += TryConnectLocalMcp;
        }

        private static async void TryConnectLocalMcp()
        {
            if (MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http)) return;
            if (!EditorPrefs.GetBool("MCPForUnity.UseHttpTransport", true) ||
                EditorPrefs.GetString("MCPForUnity.HttpTransportScope", "local") != "local" ||
                EditorPrefs.GetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080").TrimEnd('/') != "http://127.0.0.1:8080" ||
                !MCPServiceLocator.Server.IsLocalHttpServerReachable()) return;
            if (await MCPServiceLocator.Bridge.StartAsync())
                Debug.Log("[Unity Agent] Reconnected to the pinned local MCP after domain reload.");
        }

        [MenuItem("Tools/Unity Agent/Connect Local MCP")]
        public static async void Connect()
        {
            try
            {
                if (MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http))
                {
                    Debug.Log("[Unity Agent] Local MCP is already connected.");
                    return;
                }
                // Do not change another project's/global MCP endpoint or start an unpinned server.
                if (!EditorPrefs.GetBool("MCPForUnity.UseHttpTransport", true) ||
                    EditorPrefs.GetString("MCPForUnity.HttpTransportScope", "local") != "local" ||
                    EditorPrefs.GetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080").TrimEnd('/') != "http://127.0.0.1:8080")
                    throw new InvalidOperationException("Review the MCP endpoint: expected HTTP Local at http://127.0.0.1:8080.");
                if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
                    throw new InvalidOperationException("Start the pinned server with tools/UnityAgent.ps1 Start first.");
                if (!await MCPServiceLocator.Bridge.StartAsync())
                    throw new InvalidOperationException("MCP connection failed; inspect Console.");
                Debug.Log("[Unity Agent] Local MCP connected.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        private static void RequireCleanEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for idle Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Unsaved scene changes detected. Preserve/review them before verification.");
        }

        [MenuItem("Tools/Unity Agent/Prepare Click Smoke Test (Play Mode Only)")]
        public static void PrepareClickSmokeTest()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
                throw new InvalidOperationException("Requires Main in Play Mode. Changes are temporary; do not save the scene.");
            var player = UnityEngine.Object.FindFirstObjectByType<InteractionRaycaster>();
            var button = GameObject.Find("Elevator/Panel1/Close");
            if (player == null || button == null || Camera.main == null)
                throw new InvalidOperationException("Expected Main player/camera/close button was not found.");
            var interactable = button.GetComponent<Interactable>();
            if (interactable == null) throw new InvalidOperationException("Close has no Interactable.");
            var buttonData = new SerializedObject(interactable);
            buttonData.FindProperty("actionType").enumValueIndex = (int)PlayerAction.PressClose;
            buttonData.ApplyModifiedPropertiesWithoutUndo();
            var look = player.GetComponent<PlayerLook>();
            if (look != null) look.enabled = false; // Fixed aim isolates the real mouse click path.
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.rotation = Quaternion.Euler(0, 270, 0);
            // Preserve the eye's authored local offset: ElevatorController restores it after travel.
            player.transform.position = button.transform.position + Vector3.right * 1.5f
                - player.transform.TransformVector(Camera.main.transform.localPosition);
            Camera.main.transform.rotation = player.transform.rotation;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameEvents.OnPlayerCommitted -= LogSmokeCommit;
            GameEvents.OnPlayerCommitted += LogSmokeCommit;
            Debug.Log("[ClickSmoke] Temporary Main fixture: Close=PressClose, camera 1.5m inside, fixed aim. Use an OS left click in Game View; no SubmitAction was invoked. Stop Play Mode to restore.");
        }

        private static void LogSmokeCommit(PlayerAction action) => Debug.Log("[ClickSmoke] Player committed " + action);

        [MenuItem("Tools/Unity Agent/Run Input Regression Tests")]
        public static void RunInputTests()
        {
            RunInputTests(null);
        }

        public static void RunInputTests(string[] testNames)
        {
            RequireCleanEditMode();
            RecordExistingTestMetadata();
            var directory = Path.Combine(Root, "artifacts", "tests", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(directory);
            SessionState.SetString(ResultKey, Path.Combine(directory, "playmode.xml"));
            Runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = testNames,
                assemblyNames = new[] { "GraduationProject.PlayModeTests" }
            }));
            Debug.Log("[Unity Agent] Input regression requested; results: " + directory);
        }

        [MenuItem("Tools/Unity Agent/Run Serialization Checks")]
        public static void RunSerializationChecks()
        {
            RequireCleanEditMode();
            RecordExistingTestMetadata();
            var directory = Path.Combine(Root, "artifacts", "tests", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(directory);
            SessionState.SetString(ResultKey, Path.Combine(directory, "editmode.xml"));
            Runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "GraduationProject.EditorTests" }
            }));
        }

        private static void RecordExistingTestMetadata()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(ResultKey, "")) ||
                !string.IsNullOrEmpty(SessionState.GetString(ArchiveKey, "")))
                throw new InvalidOperationException("Another verification run or its cleanup is still pending.");
            var existing = TestMetadata.SelectMany(p => new[] { p, p + ".meta" })
                .Where(p => File.Exists(Path.Combine(Root, p)));
            SessionState.SetString(ExistingFilesKey, string.Join("|", existing));
        }

        private static void ArchiveGeneratedTestMetadata()
        {
            var directory = SessionState.GetString(ArchiveKey, "");
            if (string.IsNullOrEmpty(directory) || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var existing = SessionState.GetString(ExistingFilesKey, "").Split('|');
            try
            {
                // Archive only files absent before our test run; never replace pre-existing user assets.
                foreach (var relative in TestMetadata.SelectMany(p => new[] { p, p + ".meta" }))
                {
                    var source = Path.Combine(Root, relative);
                    if (existing.Contains(relative) || !File.Exists(source)) continue;
                    var destination = Path.Combine(directory, "generated", Path.GetFileName(relative));
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Move(source, destination);
                }
                SessionState.EraseString(ArchiveKey);
                SessionState.EraseString(ExistingFilesKey);
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                SessionState.EraseString(ArchiveKey);
                Debug.LogException(ex);
            }
        }

        [MenuItem("Tools/Unity Agent/Capture Game View")]
        public static void CaptureGameView()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                throw new InvalidOperationException("Capture requires running Play Mode and a visible Game View.");
            var directory = Path.Combine(Root, "artifacts", "captures");
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "game-view-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            ScreenCapture.CaptureScreenshot(stem + ".png");
            File.WriteAllText(stem + ".json", JsonUtility.ToJson(new CaptureInfo
            {
                utc = DateTime.UtcNow.ToString("o"), project = Root,
                unity = Application.unityVersion, scene = SceneManager.GetActiveScene().path,
                width = Screen.width, height = Screen.height, frame = Time.frameCount,
                colorSpace = QualitySettings.activeColorSpace.ToString(),
                source = "ScreenCapture.CaptureScreenshot: final Game View, including overlay UI; inspect PNG after it exists"
            }, true));
            Debug.Log("[Unity Agent] Screenshot requested: " + stem + ".png");
        }

        [MenuItem("Tools/Unity Agent/Build Windows Development")]
        public static void RequestBuild()
        {
            RequireCleanEditMode();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Windows64 must already be active; no automatic platform migration.");
            EditorApplication.delayCall += BuildWindows;
        }

        private static void BuildWindows()
        {
            BuildWindows(null);
        }

        [MenuItem("Tools/Unity Agent/Build M1 Normal Route Windows Development")]
        public static void RequestM1Build()
        {
            RequireCleanEditMode();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 || !File.Exists(M1NormalRouteBuilder.ScenePath))
                throw new InvalidOperationException("Requires Windows64 and the saved M1 normal-route scene.");
            EditorApplication.delayCall += () => BuildWindows(new[] { M1NormalRouteBuilder.ScenePath });
        }

        [MenuItem("Tools/Unity Agent/Build M2 Vertical Slice Windows Development")]
        public static void RequestM2Build()
        {
            RequireCleanEditMode();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 || !File.Exists(M2VerticalSliceBuilder.ScenePath))
                throw new InvalidOperationException("Requires Windows64 and the saved M2 vertical-slice scene.");
            EditorApplication.delayCall += () => BuildWindows(new[] { M2VerticalSliceBuilder.ScenePath });
        }

        private static void BuildWindows(string[] requestedScenes)
        {
            RequireCleanEditMode();
            if (TestMetadata.Any(p => File.Exists(Path.Combine(Root, p))))
                throw new InvalidOperationException("Review existing performance-test metadata in Assets/Resources before building; it may contain machine information.");
            var scenes = requestedScenes ?? EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
            var directory = Path.Combine(Root, "artifacts", "builds", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(directory);
            var statusFile = Path.Combine(directory, "build.json");
            var info = new BuildInfo { utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion,
                project = Root, scenes = scenes, status = "Running", executable = Path.Combine(directory, "GraduationProject.exe") };
            File.WriteAllText(statusFile, JsonUtility.ToJson(info, true));
            var originalSettings = BuildSettingsFiles.Where(p => File.Exists(Path.Combine(Root, p)))
                .ToDictionary(p => p, p => File.ReadAllBytes(Path.Combine(Root, p)));
            foreach (var entry in originalSettings)
            {
                var backup = Path.Combine(directory, "before-build", entry.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.WriteAllBytes(backup, entry.Value);
            }
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = info.executable,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.StrictMode | BuildOptions.DetailedBuildReport
                });
                info.status = report.summary.result.ToString();
                info.errors = report.summary.totalErrors;
                info.warnings = report.summary.totalWarnings;
                info.seconds = report.summary.totalTime.TotalSeconds;
                info.bytes = report.summary.totalSize;
                if (report.summary.result != BuildResult.Succeeded) Debug.LogError("[Unity Agent] Build failed: " + statusFile);
                else Debug.Log("[Unity Agent] Windows build succeeded: " + info.executable);
            }
            catch (Exception ex) { info.status = "Exception"; info.error = ex.ToString(); Debug.LogException(ex); }
            finally
            {
                File.WriteAllText(statusFile, JsonUtility.ToJson(info, true));
                // URP/Input System/ProBuilder build callbacks can persist generated settings.
                // Preserve those bytes as evidence, then restore the settings from immediately before this build.
                foreach (var entry in originalSettings)
                {
                    var path = Path.Combine(Root, entry.Key);
                    var generated = File.ReadAllBytes(path);
                    if (entry.Value.SequenceEqual(generated)) continue;
                    var backup = Path.Combine(directory, "after-build", entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.WriteAllBytes(backup, generated);
                    File.WriteAllBytes(path, entry.Value);
                }
                AssetDatabase.Refresh();
            }
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var path = SessionState.GetString(ResultKey, "");
                if (string.IsNullOrEmpty(path)) return;
                SessionState.EraseString(ResultKey);
                TestRunnerApi.SaveResultToFile(result, path);
                SessionState.SetString(ArchiveKey, Path.GetDirectoryName(path));
                Debug.Log($"[Unity Agent] Tests {result.TestStatus}: {result.PassCount} passed, {result.FailCount} failed. {path}");
            }
        }
        [Serializable] private class CaptureInfo
        {
            public string utc, project, unity, scene, colorSpace, source;
            public int width, height, frame;
        }
        [Serializable] private class BuildInfo
        {
            public string utc, unity, project, status, executable, error;
            public string[] scenes;
            public int errors, warnings;
            public double seconds;
            public ulong bytes;
        }
    }
}
