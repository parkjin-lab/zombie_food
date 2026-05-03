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
            string screenshotPath = CaptureScreenshot();
            string draftPath = GetProjectPath(DraftRelativePath);
            File.WriteAllText(draftPath, BuildSnapshotDraft(screenshotPath), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(draftPath);
            Debug.Log("FoodTruck prototype Play Mode snapshot draft written to " + draftPath);
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
            File.WriteAllText(GetProjectPath(DraftRelativePath), BuildSnapshotDraft(screenshotPath), Encoding.UTF8);
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

        private static string BuildSnapshotDraft(string screenshotPath)
        {
            bool hasHud = UnityEngine.Object.FindFirstObjectByType<FoodTruckPrototypeHud>() != null;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("FoodTruck Prototype Play Mode Verification Draft");
            builder.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " KST");
            builder.AppendLine("Unity version: " + Application.unityVersion);
            builder.AppendLine("Aspect ratio / resolution: " + BuildResolutionLabel());
            builder.AppendLine("HUD instance present: " + (hasHud ? "yes" : "no"));
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
