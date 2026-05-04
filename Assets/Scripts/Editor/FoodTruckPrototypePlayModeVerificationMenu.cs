using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ZombieFoodcenter.Prototype;

namespace ZombieFoodcenter.Editor
{
    public static class FoodTruckPrototypePlayModeVerificationMenu
    {
        private const string MenuRoot = "Tools/Food Truck Prototype/";
        private const string VerificationDocRelativePath = "Docs/Prototype_PlayMode_Verification.md";
        private const string ScreenshotDirectoryRelativePath = "Docs/PlayModeScreenshots";
        private const string DraftRelativePath = "Docs/Prototype_PlayMode_Verification_Draft.txt";

        [MenuItem(MenuRoot + "Capture Play Mode Snapshot", true)]
        private static bool CanCapturePlayModeSnapshot()
        {
            return EditorApplication.isPlaying && !EditorApplication.isCompiling;
        }

        [MenuItem(MenuRoot + "Capture Play Mode Snapshot")]
        private static void CapturePlayModeSnapshot()
        {
            CapturePlayModeSnapshot(null, null);
        }

        private static void CapturePlayModeSnapshot(string preparedStateLabel, string preparedMessage)
        {
            string screenshotPath = CaptureScreenshot();
            string draftPath = GetProjectPath(DraftRelativePath);
            File.WriteAllText(draftPath, BuildSnapshotDraft(screenshotPath, preparedStateLabel, preparedMessage), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(draftPath);
            Debug.Log("FoodTruck prototype Play Mode snapshot draft written to " + draftPath);
        }

        [MenuItem(MenuRoot + "Prepare State/Draw Choice", true)]
        private static bool CanPrepareDrawChoiceState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare State/Draw Choice")]
        private static void PrepareDrawChoiceState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.DrawChoice, false);
        }

        [MenuItem(MenuRoot + "Prepare State/Pending Placement", true)]
        private static bool CanPreparePendingPlacementState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare State/Pending Placement")]
        private static void PreparePendingPlacementState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.PendingPlacement, false);
        }

        [MenuItem(MenuRoot + "Prepare State/Invalid Placement", true)]
        private static bool CanPrepareInvalidPlacementState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare State/Invalid Placement")]
        private static void PrepareInvalidPlacementState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.InvalidPlacement, false);
        }

        [MenuItem(MenuRoot + "Prepare State/Wave Combat", true)]
        private static bool CanPrepareWaveCombatState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare State/Wave Combat")]
        private static void PrepareWaveCombatState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.WaveCombat, false);
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Draw Choice", true)]
        private static bool CanPrepareAndCaptureDrawChoiceState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Draw Choice")]
        private static void PrepareAndCaptureDrawChoiceState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.DrawChoice, true);
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Pending Placement", true)]
        private static bool CanPrepareAndCapturePendingPlacementState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Pending Placement")]
        private static void PrepareAndCapturePendingPlacementState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.PendingPlacement, true);
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Invalid Placement", true)]
        private static bool CanPrepareAndCaptureInvalidPlacementState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Invalid Placement")]
        private static void PrepareAndCaptureInvalidPlacementState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.InvalidPlacement, true);
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Wave Combat", true)]
        private static bool CanPrepareAndCaptureWaveCombatState()
        {
            return CanPrepareVerificationState();
        }

        [MenuItem(MenuRoot + "Prepare and Capture State/Wave Combat")]
        private static void PrepareAndCaptureWaveCombatState()
        {
            PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState.WaveCombat, true);
        }

        [MenuItem(MenuRoot + "Record PASS Manual Result", true)]
        private static bool CanRecordPassManualResult()
        {
            return EditorApplication.isPlaying && !EditorApplication.isCompiling;
        }

        [MenuItem(MenuRoot + "Record PASS Manual Result")]
        private static void RecordPassManualResult()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Record prototype Play Mode PASS?",
                "Use this only after visually confirming Draw Choice, Pending Placement, Invalid Placement, and Wave Combat are readable and usable.",
                "Record PASS",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            string screenshotPath = CaptureScreenshot();
            string docPath = GetProjectPath(VerificationDocRelativePath);
            if (!File.Exists(docPath))
            {
                EditorUtility.DisplayDialog("Verification doc missing", "Missing file: " + docPath, "OK");
                return;
            }

            string content = File.ReadAllText(docPath);
            string replacement = BuildPassLatestResultSection(screenshotPath);
            string updated = Regex.Replace(
                content,
                @"(?ms)^## Latest Manual Result\s*.*?(?=^## |\z)",
                replacement);

            if (updated == content)
            {
                EditorUtility.DisplayDialog(
                    "Verification section missing",
                    "Could not find the Latest Manual Result section in " + docPath,
                    "OK");
                return;
            }

            File.WriteAllText(docPath, updated, Encoding.UTF8);
            File.WriteAllText(GetProjectPath(DraftRelativePath), BuildSnapshotDraft(screenshotPath, null, null), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(docPath);
            Debug.Log("FoodTruck prototype manual PASS result recorded in " + docPath);
        }

        [MenuItem(MenuRoot + "Open Verification Doc")]
        private static void OpenVerificationDoc()
        {
            string docPath = GetProjectPath(VerificationDocRelativePath);
            if (File.Exists(docPath))
            {
                EditorUtility.OpenWithDefaultApp(docPath);
                return;
            }

            EditorUtility.DisplayDialog("Verification doc missing", "Missing file: " + docPath, "OK");
        }

        private static string CaptureScreenshot()
        {
            string screenshotDir = GetProjectPath(ScreenshotDirectoryRelativePath);
            Directory.CreateDirectory(screenshotDir);

            string fileName = "foodtruck-playmode-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
            string screenshotPath = Path.Combine(screenshotDir, fileName);
            ScreenCapture.CaptureScreenshot(screenshotPath, 1);
            return screenshotPath;
        }

        private static string BuildSnapshotDraft(string screenshotPath, string preparedStateLabel, string preparedMessage)
        {
            FoodTruckPrototypeHud hud = FindHud();
            bool hasHud = hud != null;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("FoodTruck Prototype Play Mode Verification Draft");
            builder.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " KST");
            builder.AppendLine("Unity version: " + Application.unityVersion);
            builder.AppendLine("Aspect ratio / resolution: " + BuildResolutionLabel());
            builder.AppendLine("HUD instance present: " + (hasHud ? "yes" : "no"));
            if (!string.IsNullOrEmpty(preparedStateLabel))
            {
                builder.AppendLine("Prepared state: " + preparedStateLabel);
            }
            if (!string.IsNullOrEmpty(preparedMessage))
            {
                builder.AppendLine("Prepare result: " + preparedMessage);
            }
            if (hasHud)
            {
                builder.AppendLine("HUD state summary: " + hud.BuildPlayModeVerificationStateSummary());
            }
            builder.AppendLine("Screenshot: " + screenshotPath);
            builder.AppendLine();
            builder.AppendLine("Resource checks:");
            builder.AppendLine("- FoodTruck sprite: " + Loaded("FoodTruckPrototype/Sprites/Truck/FoodTruck"));
            builder.AppendLine("- KitchenModule sprite: " + Loaded("FoodTruckPrototype/Sprites/KitchenModules/KitchenModule"));
            builder.AppendLine("- Onion icon: " + Loaded("FoodTruckPrototype/Sprites/Ingredients/Onion"));
            builder.AppendLine("- Placement occupied icon: " + Loaded("FoodTruckPrototype/Sprites/PlacementErrors/placement_fail_occupied"));
            builder.AppendLine();
            builder.AppendLine("Manual checklist:");
            builder.AppendLine("- Draw Choice: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED");
            builder.AppendLine("- Pending Placement: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED");
            builder.AppendLine("- Invalid Placement: PASS / FIX_FEEDBACK / FIX_LAYOUT / BLOCKED");
            builder.AppendLine("- Wave Combat: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED");
            builder.AppendLine();
            builder.AppendLine("After recording, run:");
            builder.AppendLine("powershell -ExecutionPolicy Bypass -File \"Tools\\Verify-PrototypePlayModeRecord.ps1\" -ProjectPath \"D:\\uni\\zombieFoodcenter\" -JsonOnly");
            return builder.ToString();
        }

        private static bool CanPrepareVerificationState()
        {
            return EditorApplication.isPlaying &&
                !EditorApplication.isCompiling &&
                FindHud() != null;
        }

        private static void PrepareVerificationState(FoodTruckPrototypeHud.PlayModeVerificationState state, bool captureAfterPrepare)
        {
            FoodTruckPrototypeHud hud = FindHud();
            if (hud == null)
            {
                EditorUtility.DisplayDialog("FoodTruck HUD missing", "No FoodTruckPrototypeHud instance is present in Play Mode.", "OK");
                return;
            }

            string stateLabel = BuildStateLabel(state);
            if (!hud.TryPreparePlayModeVerificationState(state, out string message))
            {
                EditorUtility.DisplayDialog("Could not prepare state", stateLabel + ": " + message, "OK");
                return;
            }

            Debug.Log("FoodTruck prototype verification state prepared: " + stateLabel + " | " + message);

            if (!captureAfterPrepare)
            {
                return;
            }

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    return;
                }

                CapturePlayModeSnapshot(stateLabel, message);
            };
        }

        private static FoodTruckPrototypeHud FindHud()
        {
            return UnityEngine.Object.FindFirstObjectByType<FoodTruckPrototypeHud>();
        }

        private static string BuildStateLabel(FoodTruckPrototypeHud.PlayModeVerificationState state)
        {
            switch (state)
            {
                case FoodTruckPrototypeHud.PlayModeVerificationState.DrawChoice:
                    return "Draw Choice";
                case FoodTruckPrototypeHud.PlayModeVerificationState.PendingPlacement:
                    return "Pending Placement";
                case FoodTruckPrototypeHud.PlayModeVerificationState.InvalidPlacement:
                    return "Invalid Placement";
                case FoodTruckPrototypeHud.PlayModeVerificationState.WaveCombat:
                    return "Wave Combat";
                default:
                    return state.ToString();
            }
        }

        private static string BuildPassLatestResultSection(string screenshotPath)
        {
            return
                "## Latest Manual Result" + Environment.NewLine +
                "Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " KST" + Environment.NewLine +
                "Unity version: " + Application.unityVersion + Environment.NewLine +
                "Aspect ratio / resolution: " + BuildResolutionLabel() + Environment.NewLine +
                Environment.NewLine +
                "Draw Choice: PASS" + Environment.NewLine +
                "Pending Placement: PASS" + Environment.NewLine +
                "Invalid Placement: PASS" + Environment.NewLine +
                "Wave Combat: PASS" + Environment.NewLine +
                Environment.NewLine +
                "Screenshots captured: 1" + Environment.NewLine +
                "1. " + screenshotPath + Environment.NewLine +
                "Top issue: None recorded during manual PASS check." + Environment.NewLine +
                "Next code target: None." + Environment.NewLine +
                "Verification command result: Recorded through Unity Editor menu; run Tools\\Verify-PrototypePlayModeRecord.ps1 -JsonOnly." + Environment.NewLine +
                Environment.NewLine;
        }

        private static string BuildResolutionLabel()
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            int divisor = GreatestCommonDivisor(width, height);
            return width + "x" + height + " (" + (width / divisor) + ":" + (height / divisor) + ")";
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                int remainder = a % b;
                a = b;
                b = remainder;
            }

            return Mathf.Max(1, a);
        }

        private static string Loaded(string resourcesPath)
        {
            return Resources.Load<Sprite>(resourcesPath) != null ? "loaded" : "missing";
        }

        private static string GetProjectPath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        }
    }
}
