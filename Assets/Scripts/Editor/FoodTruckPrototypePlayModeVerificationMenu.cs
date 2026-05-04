using System;
using System.Collections.Generic;
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
        private const string SuiteDraftRelativePath = "Docs/Prototype_PlayMode_Verification_Suite.txt";
        private const double SuiteCaptureDelaySeconds = 0.35d;

        private static readonly FoodTruckPrototypeHud.PlayModeVerificationState[] VerificationSuiteStates =
        {
            FoodTruckPrototypeHud.PlayModeVerificationState.DrawChoice,
            FoodTruckPrototypeHud.PlayModeVerificationState.PendingPlacement,
            FoodTruckPrototypeHud.PlayModeVerificationState.InvalidPlacement,
            FoodTruckPrototypeHud.PlayModeVerificationState.WaveCombat
        };

        private static VerificationSuiteCaptureState activeSuiteCapture;
        private static List<VerificationSuiteCaptureEntry> lastVerificationSuiteEntries = new List<VerificationSuiteCaptureEntry>();

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
            string screenshotPath = CaptureScreenshot(preparedStateLabel);
            string draftPath = GetProjectPath(DraftRelativePath);
            File.WriteAllText(draftPath, BuildSnapshotDraft(screenshotPath, preparedStateLabel, preparedMessage), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(draftPath);
            Debug.Log("FoodTruck prototype Play Mode snapshot draft written to " + draftPath);
        }

        [MenuItem(MenuRoot + "Capture Verification Suite", true)]
        private static bool CanCaptureVerificationSuite()
        {
            return CanPrepareVerificationState() && activeSuiteCapture == null;
        }

        [MenuItem(MenuRoot + "Capture Verification Suite")]
        private static void CaptureVerificationSuite()
        {
            FoodTruckPrototypeHud hud = FindHud();
            if (hud == null)
            {
                EditorUtility.DisplayDialog("FoodTruck HUD missing", "No FoodTruckPrototypeHud instance is present in Play Mode.", "OK");
                return;
            }

            activeSuiteCapture = new VerificationSuiteCaptureState(DateTime.Now);
            EditorApplication.update -= ContinueVerificationSuiteCapture;
            EditorApplication.update += ContinueVerificationSuiteCapture;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            Debug.Log("FoodTruck prototype verification suite capture started.");
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
                "Use this only after visually confirming Draw Choice, Pending Placement, Invalid Placement, and Wave Combat are readable and usable. If a verification suite manifest exists, its screenshots will be included as evidence.",
                "Record PASS",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            string screenshotPath = CaptureScreenshot("pass-confirmation");
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
            return CaptureScreenshot(null);
        }

        private static string CaptureScreenshot(string labelSuffix)
        {
            string screenshotDir = GetProjectPath(ScreenshotDirectoryRelativePath);
            Directory.CreateDirectory(screenshotDir);

            string suffix = BuildFileNameSuffix(labelSuffix);
            string fileName = "foodtruck-playmode-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") +
                (string.IsNullOrEmpty(suffix) ? string.Empty : "-" + suffix) +
                ".png";
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

        private static void ContinueVerificationSuiteCapture()
        {
            VerificationSuiteCaptureState capture = activeSuiteCapture;
            if (capture == null)
            {
                EditorApplication.update -= ContinueVerificationSuiteCapture;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                FinishVerificationSuiteCapture("Play Mode stopped before the suite completed.", true);
                return;
            }

            if (EditorApplication.timeSinceStartup < capture.NextActionTime)
            {
                return;
            }

            if (capture.PendingEntry != null)
            {
                capture.PendingEntry.ScreenshotPath = CaptureScreenshot(capture.PendingEntry.StateLabel);
                capture.Entries.Add(capture.PendingEntry);
                Debug.Log("FoodTruck prototype verification suite captured: " + capture.PendingEntry.StateLabel);
                capture.PendingEntry = null;
                capture.StateIndex++;
                capture.NextActionTime = EditorApplication.timeSinceStartup + SuiteCaptureDelaySeconds;
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
                return;
            }

            if (capture.StateIndex >= VerificationSuiteStates.Length)
            {
                FinishVerificationSuiteCapture(null, false);
                return;
            }

            FoodTruckPrototypeHud hud = FindHud();
            if (hud == null)
            {
                FinishVerificationSuiteCapture("FoodTruck HUD disappeared before the suite completed.", true);
                return;
            }

            FoodTruckPrototypeHud.PlayModeVerificationState state = VerificationSuiteStates[capture.StateIndex];
            string stateLabel = BuildStateLabel(state);
            VerificationSuiteCaptureEntry entry = new VerificationSuiteCaptureEntry
            {
                StateLabel = stateLabel
            };

            if (!hud.TryPreparePlayModeVerificationState(state, out string message))
            {
                entry.Prepared = false;
                entry.PrepareMessage = message;
                entry.HudStateSummary = hud.BuildPlayModeVerificationStateSummary();
                capture.Entries.Add(entry);
                capture.StateIndex++;
                capture.NextActionTime = EditorApplication.timeSinceStartup + SuiteCaptureDelaySeconds;
                Debug.LogWarning("FoodTruck prototype verification suite could not prepare " + stateLabel + ": " + message);
                return;
            }

            entry.Prepared = true;
            entry.PrepareMessage = message;
            entry.HudStateSummary = hud.BuildPlayModeVerificationStateSummary();
            capture.PendingEntry = entry;
            capture.NextActionTime = EditorApplication.timeSinceStartup + SuiteCaptureDelaySeconds;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            Debug.Log("FoodTruck prototype verification suite prepared: " + stateLabel + " | " + message);
        }

        private static void FinishVerificationSuiteCapture(string abortReason, bool aborted)
        {
            VerificationSuiteCaptureState capture = activeSuiteCapture;
            activeSuiteCapture = null;
            EditorApplication.update -= ContinueVerificationSuiteCapture;

            if (capture == null)
            {
                return;
            }

            if (capture.PendingEntry != null)
            {
                capture.Entries.Add(capture.PendingEntry);
                capture.PendingEntry = null;
            }

            capture.AbortReason = abortReason;
            capture.Aborted = aborted;
            string suitePath = GetProjectPath(SuiteDraftRelativePath);
            File.WriteAllText(suitePath, BuildVerificationSuiteDraft(capture), Encoding.UTF8);
            lastVerificationSuiteEntries = new List<VerificationSuiteCaptureEntry>(capture.Entries);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(suitePath);

            if (aborted)
            {
                Debug.LogWarning("FoodTruck prototype verification suite stopped: " + abortReason);
            }
            else
            {
                Debug.Log("FoodTruck prototype verification suite written to " + suitePath);
            }
        }

        private static string BuildVerificationSuiteDraft(VerificationSuiteCaptureState capture)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("FoodTruck Prototype Play Mode Verification Suite");
            builder.AppendLine("Date: " + capture.StartedAt.ToString("yyyy-MM-dd HH:mm") + " KST");
            builder.AppendLine("Completed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " KST");
            builder.AppendLine("Unity version: " + Application.unityVersion);
            builder.AppendLine("Aspect ratio / resolution: " + BuildResolutionLabel());
            builder.AppendLine("Suite status: " + (capture.Aborted ? "aborted" : "completed"));
            if (!string.IsNullOrEmpty(capture.AbortReason))
            {
                builder.AppendLine("Abort reason: " + capture.AbortReason);
            }

            builder.AppendLine();
            builder.AppendLine("Captured states:");
            for (int i = 0; i < capture.Entries.Count; i++)
            {
                VerificationSuiteCaptureEntry entry = capture.Entries[i];
                builder.AppendLine((i + 1) + ". " + entry.StateLabel + ": " +
                    (entry.Prepared ? "prepared" : "prepare_failed") +
                    (string.IsNullOrEmpty(entry.ScreenshotPath) ? "" : " | " + entry.ScreenshotPath));
            }

            builder.AppendLine();
            builder.AppendLine("State details:");
            foreach (VerificationSuiteCaptureEntry entry in capture.Entries)
            {
                builder.AppendLine("- " + entry.StateLabel);
                builder.AppendLine("  Prepared: " + (entry.Prepared ? "yes" : "no"));
                builder.AppendLine("  Prepare result: " + entry.PrepareMessage);
                builder.AppendLine("  HUD state summary: " + entry.HudStateSummary);
                builder.AppendLine("  Screenshot: " + (string.IsNullOrEmpty(entry.ScreenshotPath) ? "not captured" : entry.ScreenshotPath));
            }

            builder.AppendLine();
            builder.AppendLine("Manual result template:");
            builder.AppendLine("Draw Choice: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED");
            builder.AppendLine("Pending Placement: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED");
            builder.AppendLine("Invalid Placement: PASS / FIX_FEEDBACK / FIX_LAYOUT / BLOCKED");
            builder.AppendLine("Wave Combat: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED");
            builder.AppendLine();
            builder.AppendLine("After recording, run:");
            builder.AppendLine("powershell -ExecutionPolicy Bypass -File \"Tools\\Verify-PrototypePlayModeRecord.ps1\" -ProjectPath \"D:\\uni\\zombieFoodcenter\" -JsonOnly");
            return builder.ToString();
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
            string screenshotEvidence = BuildScreenshotEvidenceSection(screenshotPath);
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
                screenshotEvidence +
                "Top issue: None recorded during manual PASS check." + Environment.NewLine +
                "Next code target: None." + Environment.NewLine +
                "Verification command result: Recorded through Unity Editor menu; run Tools\\Verify-PrototypePlayModeRecord.ps1 -JsonOnly." + Environment.NewLine +
                Environment.NewLine;
        }

        private static string BuildScreenshotEvidenceSection(string fallbackScreenshotPath)
        {
            List<string> paths = CollectManualResultScreenshotEvidence(fallbackScreenshotPath);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Screenshots captured: " + paths.Count);
            for (int i = 0; i < paths.Count; i++)
            {
                builder.AppendLine((i + 1) + ". " + paths[i]);
            }

            return builder.ToString();
        }

        private static List<string> CollectManualResultScreenshotEvidence(string fallbackScreenshotPath)
        {
            List<string> paths = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (VerificationSuiteCaptureEntry entry in lastVerificationSuiteEntries)
            {
                AddEvidencePath(paths, seen, entry.ScreenshotPath, false);
            }

            AddSuiteManifestEvidence(paths, seen);
            AddEvidencePath(paths, seen, fallbackScreenshotPath, false);
            return paths;
        }

        private static void AddSuiteManifestEvidence(List<string> paths, HashSet<string> seen)
        {
            string suitePath = GetProjectPath(SuiteDraftRelativePath);
            if (!File.Exists(suitePath))
            {
                return;
            }

            string suiteText = File.ReadAllText(suitePath);
            MatchCollection screenshotMatches = Regex.Matches(
                suiteText,
                @"(?im)^\s*Screenshot:\s*(?<path>.+?)\s*$");

            foreach (Match match in screenshotMatches)
            {
                AddEvidencePath(paths, seen, match.Groups["path"].Value, true);
            }
        }

        private static void AddEvidencePath(List<string> paths, HashSet<string> seen, string candidatePath, bool requireExistingFile)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                return;
            }

            string normalized = candidatePath.Trim();
            if (string.Equals(normalized, "not captured", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "NOT_RECORDED", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fullPath = Path.IsPathRooted(normalized)
                ? Path.GetFullPath(normalized)
                : GetProjectPath(normalized);

            if (requireExistingFile && !File.Exists(fullPath))
            {
                return;
            }

            if (seen.Add(fullPath))
            {
                paths.Add(fullPath);
            }
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

        private static string BuildFileNameSuffix(string labelSuffix)
        {
            if (string.IsNullOrWhiteSpace(labelSuffix))
            {
                return string.Empty;
            }

            string slug = Regex.Replace(labelSuffix.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return slug;
        }

        private static string GetProjectPath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        }

        private sealed class VerificationSuiteCaptureState
        {
            public VerificationSuiteCaptureState(DateTime startedAt)
            {
                StartedAt = startedAt;
                NextActionTime = EditorApplication.timeSinceStartup;
                Entries = new List<VerificationSuiteCaptureEntry>();
            }

            public DateTime StartedAt { get; private set; }
            public double NextActionTime { get; set; }
            public int StateIndex { get; set; }
            public VerificationSuiteCaptureEntry PendingEntry { get; set; }
            public List<VerificationSuiteCaptureEntry> Entries { get; private set; }
            public bool Aborted { get; set; }
            public string AbortReason { get; set; }
        }

        private sealed class VerificationSuiteCaptureEntry
        {
            public string StateLabel { get; set; }
            public bool Prepared { get; set; }
            public string PrepareMessage { get; set; }
            public string HudStateSummary { get; set; }
            public string ScreenshotPath { get; set; }
        }
    }
}
