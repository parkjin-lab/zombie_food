using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void ResetTelemetryRunTracking(bool clearRunMetrics)
        {
            if (model == null)
            {
                return;
            }

            telemetryPrevHasDrawChoice = model.HasDrawChoice;
            telemetryPrevIsOverheated = model.IsOverheated;
            telemetryPrevPlacementSuccessCount = model.PlacementSuccessCount;
            telemetryPrevWave = model.Wave;
            telemetryCurrentRhythmBeat = model.CurrentRhythmBeatLabel;
            telemetryCurrentRhythmBeatSeconds = 0f;

            if (!clearRunMetrics)
            {
                return;
            }

            telemetryDrawToPlaceTimer = -1f;
            telemetryDrawToPlaceTotalSeconds = 0f;
            telemetryDrawToPlaceSamples = 0;
            telemetryOverheatEntryCountRun = 0;
            telemetryOverheatEntriesByWave.Clear();
            Array.Clear(telemetryDrawPickValueBucketCounts, 0, telemetryDrawPickValueBucketCounts.Length);
            Array.Clear(telemetryDrawPickRiskTagCounts, 0, telemetryDrawPickRiskTagCounts.Length);
            Array.Clear(telemetryWaveBaseDrawPickValueBucketCounts, 0, telemetryWaveBaseDrawPickValueBucketCounts.Length);
            Array.Clear(telemetryWaveBaseDrawPickRiskTagCounts, 0, telemetryWaveBaseDrawPickRiskTagCounts.Length);
            telemetryManualMergeSuccessCount = 0;
            telemetryRhythmBeatTransitionCount = 0;
            telemetryRunHasMeaningfulData = false;
        }

        private void AdvanceTelemetryRunSession()
        {
            telemetryRunSessionId = Mathf.Max(1, telemetryRunSessionId + 1);
        }

        private void RegisterTelemetryManualMergeSuccess()
        {
            telemetryManualMergeSuccessCount += 1;
            telemetryRunHasMeaningfulData = true;
        }

        private void RegisterTelemetryDrawPick(string valueBucket, string riskTag)
        {
            int valueIndex = ResolveTelemetryTierIndex(valueBucket);
            if (valueIndex >= 0 && valueIndex < telemetryDrawPickValueBucketCounts.Length)
            {
                telemetryDrawPickValueBucketCounts[valueIndex] += 1;
            }

            int riskIndex = ResolveTelemetryTierIndex(riskTag);
            if (riskIndex >= 0 && riskIndex < telemetryDrawPickRiskTagCounts.Length)
            {
                telemetryDrawPickRiskTagCounts[riskIndex] += 1;
            }

            telemetryRunHasMeaningfulData = true;
        }

        private static int ResolveTelemetryTierIndex(string tier)
        {
            switch (tier)
            {
                case "LOW":
                    return 0;
                case "MID":
                    return 1;
                case "HIGH":
                    return 2;
                default:
                    return -1;
            }
        }

        private void UpdateTelemetryAutomation(float dt)
        {
            if (model == null)
            {
                return;
            }

            if (model.Wave < telemetryPrevWave || model.PlacementSuccessCount < telemetryPrevPlacementSuccessCount)
            {
                AdvanceTelemetryRunSession();
                ResetTelemetryRunTracking(true);
            }

            bool hasDrawChoice = model.HasDrawChoice;
            if (hasDrawChoice && !telemetryPrevHasDrawChoice)
            {
                telemetryDrawToPlaceTimer = 0f;
            }
            else if (telemetryDrawToPlaceTimer >= 0f && !hasDrawChoice && !model.HasPendingBlock)
            {
                telemetryDrawToPlaceTimer = -1f;
            }

            if (telemetryDrawToPlaceTimer >= 0f)
            {
                telemetryDrawToPlaceTimer += Mathf.Max(0f, dt);
            }

            string rhythmBeat = model.CurrentRhythmBeatLabel;
            float clampedDeltaTime = Mathf.Max(0f, dt);
            if (string.IsNullOrEmpty(telemetryCurrentRhythmBeat))
            {
                telemetryCurrentRhythmBeat = rhythmBeat;
                telemetryCurrentRhythmBeatSeconds = 0f;
            }
            else if (!string.Equals(telemetryCurrentRhythmBeat, rhythmBeat, StringComparison.Ordinal))
            {
                telemetryCurrentRhythmBeat = rhythmBeat;
                telemetryCurrentRhythmBeatSeconds = 0f;
                telemetryRhythmBeatTransitionCount += 1;
                telemetryRunHasMeaningfulData = true;
            }
            else
            {
                telemetryCurrentRhythmBeatSeconds += clampedDeltaTime;
            }

            int currentSuccess = model.PlacementSuccessCount;
            if (currentSuccess > telemetryPrevPlacementSuccessCount)
            {
                if (telemetryDrawToPlaceTimer >= 0f)
                {
                    telemetryDrawToPlaceTotalSeconds += telemetryDrawToPlaceTimer;
                    telemetryDrawToPlaceSamples += 1;
                    telemetryDrawToPlaceTimer = -1f;
                }

                telemetryRunHasMeaningfulData = true;
            }

            bool isOverheated = model.IsOverheated;
            if (isOverheated && !telemetryPrevIsOverheated)
            {
                telemetryOverheatEntryCountRun += 1;
                if (telemetryOverheatEntriesByWave.TryGetValue(model.Wave, out int existing))
                {
                    telemetryOverheatEntriesByWave[model.Wave] = existing + 1;
                }
                else
                {
                    telemetryOverheatEntriesByWave[model.Wave] = 1;
                }

                telemetryRunHasMeaningfulData = true;
            }

            if (model.PlacementAttemptCount > 0 || model.DrawChoicePickTotal > 0)
            {
                telemetryRunHasMeaningfulData = true;
            }

            telemetryPrevHasDrawChoice = hasDrawChoice;
            telemetryPrevIsOverheated = isOverheated;
            telemetryPrevPlacementSuccessCount = currentSuccess;
            telemetryPrevWave = model.Wave;
        }

        private bool HasTelemetryDataToExport()
        {
            if (model == null)
            {
                return false;
            }

            if (telemetryWaveScope)
            {
                GetScopedTelemetryStats(
                    out int attempts,
                    out int success,
                    out int directPlacementSuccess,
                    out int autoMergeSuccess,
                    out int manualMergeSuccess,
                    out int blockedOutOfBounds,
                    out int blockedOccupied,
                    out int blockedInvalidAnchor,
                    out int blockedNoPending,
                    out int pick1,
                    out int pick2,
                    out int pick3,
                    out int valueLow,
                    out int valueMid,
                    out int valueHigh,
                    out int riskLow,
                    out int riskMid,
                    out int riskHigh);

                bool hasWaveOverheat = telemetryOverheatEntriesByWave.TryGetValue(model.Wave, out int waveOverheat) && waveOverheat > 0;
                return
                    attempts > 0 ||
                    success > 0 ||
                    directPlacementSuccess > 0 ||
                    autoMergeSuccess > 0 ||
                    manualMergeSuccess > 0 ||
                    blockedOutOfBounds > 0 ||
                    blockedOccupied > 0 ||
                    blockedInvalidAnchor > 0 ||
                    blockedNoPending > 0 ||
                    pick1 > 0 ||
                    pick2 > 0 ||
                    pick3 > 0 ||
                    valueLow > 0 ||
                    valueMid > 0 ||
                    valueHigh > 0 ||
                    riskLow > 0 ||
                    riskMid > 0 ||
                    riskHigh > 0 ||
                    hasWaveOverheat;
            }

            return telemetryRunHasMeaningfulData
                || model.PlacementAttemptCount > 0
                || model.DrawChoicePickTotal > 0
                || telemetryManualMergeSuccessCount > 0
                || telemetryDrawToPlaceSamples > 0
                || telemetryOverheatEntryCountRun > 0;
        }

        private string BuildOverheatByWaveSummary()
        {
            if (telemetryOverheatEntriesByWave.Count == 0)
            {
                return string.Empty;
            }

            List<int> waves = new List<int>(telemetryOverheatEntriesByWave.Keys);
            waves.Sort();
            StringBuilder builder = new StringBuilder(32);
            for (int i = 0; i < waves.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }

                int wave = waves[i];
                builder.Append(wave);
                builder.Append(':');
                builder.Append(telemetryOverheatEntriesByWave[wave]);
            }

            return builder.ToString();
        }

        private void ExportTelemetryReportFromHud(string trigger, bool showFeedback)
        {
            if (model == null)
            {
                return;
            }

            if (!HasTelemetryDataToExport())
            {
                if (showFeedback)
                {
                    ShowCueBanner("No telemetry data to export yet.", new Color(0.90f, 0.64f, 0.26f, 1f));
                }

                return;
            }

            string fileName = string.IsNullOrEmpty(telemetryCsvFileName) ? "foodtruck_ux_telemetry.csv" : telemetryCsvFileName.Trim();
            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".csv";
            }

            string directory = Path.Combine(Application.persistentDataPath, "FoodTruckPrototypeTelemetry");
            string csvPath = Path.Combine(directory, fileName);

            try
            {
                Directory.CreateDirectory(directory);

                bool writeHeader = !File.Exists(csvPath);
                GetScopedTelemetryStats(
                    out int attempts,
                    out int success,
                    out int directPlacementSuccess,
                    out int autoMergeSuccess,
                    out int manualMergeSuccess,
                    out int blockedOutOfBounds,
                    out int blockedOccupied,
                    out int blockedInvalidAnchor,
                    out int blockedNoPending,
                    out int pick1,
                    out int pick2,
                    out int pick3,
                    out int pickValueLow,
                    out int pickValueMid,
                    out int pickValueHigh,
                    out int pickRiskLow,
                    out int pickRiskMid,
                    out int pickRiskHigh);

                float successRate = attempts > 0 ? (float)success / attempts : 0f;
                float directPlacementSuccessRate = attempts > 0 ? (float)directPlacementSuccess / attempts : 0f;
                float autoMergeShare = success > 0 ? (float)autoMergeSuccess / success : 0f;
                bool waveScopedExport = telemetryWaveScope;
                string exportScope = waveScopedExport ? "wave" : "run";
                string scopeWave = waveScopedExport
                    ? model.Wave.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;

                int drawToPlaceSamples = 0;
                float drawToPlaceAvg = -1f;
                int overheatEntries = 0;
                string overheatByWave = string.Empty;
                if (waveScopedExport)
                {
                    if (telemetryOverheatEntriesByWave.TryGetValue(model.Wave, out int waveOverheat))
                    {
                        overheatEntries = waveOverheat;
                        overheatByWave = model.Wave.ToString(CultureInfo.InvariantCulture) + ":" + waveOverheat.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    drawToPlaceSamples = telemetryDrawToPlaceSamples;
                    drawToPlaceAvg = drawToPlaceSamples > 0
                        ? telemetryDrawToPlaceTotalSeconds / drawToPlaceSamples
                        : -1f;
                    overheatEntries = telemetryOverheatEntryCountRun;
                    overheatByWave = BuildOverheatByWaveSummary();
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string rhythmBeat = model.CurrentRhythmBeatLabel;
                string waveCadence = model.LastWaveCadenceSummary;
                int scopedRhythmBeatTransitions = ApplyTelemetryScope(telemetryRhythmBeatTransitionCount, telemetryWaveBaseRhythmBeatTransitionCount);

                using (var writer = new StreamWriter(csvPath, true, Encoding.UTF8))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine("timestamp_local,trigger,run_session_id,export_sequence,export_scope,scope_wave,current_wave,placement_attempt,placement_success,placement_direct_success,placement_auto_merge_success,manual_merge_success,placement_success_rate,placement_direct_success_rate,placement_auto_merge_share,blocked_out_of_bounds,blocked_occupied,blocked_invalid_anchor,blocked_no_pending,draw_pick_1,draw_pick_2,draw_pick_3,draw_pick_value_low,draw_pick_value_mid,draw_pick_value_high,draw_pick_risk_low,draw_pick_risk_mid,draw_pick_risk_high,draw_to_place_avg_s,draw_to_place_samples,overheat_entries,overheat_by_wave,rhythm_beat,rhythm_beat_duration_s,rhythm_beat_transition_count,wave_cadence");
                    }

                    string[] row =
                    {
                        CsvEscape(timestamp),
                        CsvEscape(trigger),
                        telemetryRunSessionId.ToString(CultureInfo.InvariantCulture),
                        telemetryExportSequence.ToString(CultureInfo.InvariantCulture),
                        CsvEscape(exportScope),
                        CsvEscape(scopeWave),
                        model.Wave.ToString(CultureInfo.InvariantCulture),
                        attempts.ToString(CultureInfo.InvariantCulture),
                        success.ToString(CultureInfo.InvariantCulture),
                        directPlacementSuccess.ToString(CultureInfo.InvariantCulture),
                        autoMergeSuccess.ToString(CultureInfo.InvariantCulture),
                        manualMergeSuccess.ToString(CultureInfo.InvariantCulture),
                        successRate.ToString("0.###", CultureInfo.InvariantCulture),
                        directPlacementSuccessRate.ToString("0.###", CultureInfo.InvariantCulture),
                        autoMergeShare.ToString("0.###", CultureInfo.InvariantCulture),
                        blockedOutOfBounds.ToString(CultureInfo.InvariantCulture),
                        blockedOccupied.ToString(CultureInfo.InvariantCulture),
                        blockedInvalidAnchor.ToString(CultureInfo.InvariantCulture),
                        blockedNoPending.ToString(CultureInfo.InvariantCulture),
                        pick1.ToString(CultureInfo.InvariantCulture),
                        pick2.ToString(CultureInfo.InvariantCulture),
                        pick3.ToString(CultureInfo.InvariantCulture),
                        pickValueLow.ToString(CultureInfo.InvariantCulture),
                        pickValueMid.ToString(CultureInfo.InvariantCulture),
                        pickValueHigh.ToString(CultureInfo.InvariantCulture),
                        pickRiskLow.ToString(CultureInfo.InvariantCulture),
                        pickRiskMid.ToString(CultureInfo.InvariantCulture),
                        pickRiskHigh.ToString(CultureInfo.InvariantCulture),
                        drawToPlaceAvg >= 0f ? drawToPlaceAvg.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty,
                        drawToPlaceSamples.ToString(CultureInfo.InvariantCulture),
                        overheatEntries.ToString(CultureInfo.InvariantCulture),
                        CsvEscape(overheatByWave),
                        CsvEscape(rhythmBeat),
                        telemetryCurrentRhythmBeatSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                        scopedRhythmBeatTransitions.ToString(CultureInfo.InvariantCulture),
                        CsvEscape(waveCadence)
                    };

                    writer.WriteLine(string.Join(",", row));
                }

                if (telemetryConsoleSummaryOnExport)
                {
                    Debug.Log(
                        "[FoodTruck UX Telemetry] " +
                        "run=" + telemetryRunSessionId +
                        ", seq=" + telemetryExportSequence +
                        ", scope=" + exportScope +
                        ", " +
                        "trigger=" + trigger +
                        ", wave=" + model.Wave +
                        ", place=" + success + "/" + attempts +
                        ", direct=" + directPlacementSuccess +
                        ", autoMerge=" + autoMergeSuccess +
                        ", manualMerge=" + manualMergeSuccess +
                        ", pickValue[L/M/H]=" + pickValueLow + "/" + pickValueMid + "/" + pickValueHigh +
                        ", pickRisk[L/M/H]=" + pickRiskLow + "/" + pickRiskMid + "/" + pickRiskHigh +
                        ", rhythmBeat=" + rhythmBeat +
                        ", rhythmBeatDuration=" + telemetryCurrentRhythmBeatSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s" +
                        ", rhythmTransitions=" + scopedRhythmBeatTransitions +
                        ", drawToPlaceAvg=" + (drawToPlaceAvg >= 0f ? drawToPlaceAvg.ToString("0.00", CultureInfo.InvariantCulture) + "s" : "n/a") +
                        ", overheatEntries=" + overheatEntries +
                        ", file=" + csvPath);
                }

                telemetryExportSequence += 1;

                AppendLog("Telemetry exported (" + trigger + "): " + Path.GetFileName(csvPath));
                if (showFeedback)
                {
                    ShowCueBanner("Telemetry saved [" + trigger + "]", new Color(0.34f, 0.74f, 0.96f, 1f));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FoodTruck UX Telemetry] Export failed: " + ex.Message);
                if (showFeedback)
                {
                    ShowCueBanner("Telemetry export failed.", new Color(0.92f, 0.36f, 0.30f, 1f));
                }
            }
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool needsQuotes = value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0;
            if (!needsQuotes)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void ToggleTelemetryPanel()
        {
            telemetryPanelExpanded = !telemetryPanelExpanded;
            RefreshTelemetryControls();
            RefreshTelemetryPanel();
            RefreshGameplayHudContextImmediate();
            ShowCueBanner(telemetryPanelExpanded ? "UX telemetry panel enabled." : "UX telemetry panel hidden.",
                telemetryPanelExpanded ? new Color(0.34f, 0.72f, 0.96f, 1f) : new Color(0.60f, 0.68f, 0.78f, 1f));
        }

        private void ToggleTelemetryScope()
        {
            telemetryWaveScope = !telemetryWaveScope;
            RefreshTelemetryControls();
            RefreshTelemetryPanel();
            ShowCueBanner(telemetryWaveScope ? "Telemetry scope: current wave." : "Telemetry scope: full run.",
                new Color(0.44f, 0.82f, 0.52f, 1f));
        }

        private void CaptureTelemetryWaveBaseline()
        {
            if (model == null)
            {
                return;
            }

            telemetryWaveBaseWave = model.Wave;
            telemetryWaveBaseAttempts = model.PlacementAttemptCount;
            telemetryWaveBaseSuccess = model.PlacementSuccessCount;
            telemetryWaveBaseAutoMergeSuccess = model.AutoMergeSuccessCount;
            telemetryWaveBaseBlockedOutOfBounds = model.PlacementBlockedOutOfBoundsCount;
            telemetryWaveBaseBlockedOccupied = model.PlacementBlockedOccupiedCount;
            telemetryWaveBaseBlockedInvalidAnchor = model.PlacementBlockedInvalidAnchorCount;
            telemetryWaveBaseBlockedNoPending = model.PlacementBlockedNoPendingCount;
            telemetryWaveBasePick1 = model.GetDrawChoicePickCount(0);
            telemetryWaveBasePick2 = model.GetDrawChoicePickCount(1);
            telemetryWaveBasePick3 = model.GetDrawChoicePickCount(2);
            telemetryWaveBaseManualMergeSuccess = telemetryManualMergeSuccessCount;
            telemetryWaveBaseRhythmBeatTransitionCount = telemetryRhythmBeatTransitionCount;
            Array.Copy(telemetryDrawPickValueBucketCounts, telemetryWaveBaseDrawPickValueBucketCounts, telemetryDrawPickValueBucketCounts.Length);
            Array.Copy(telemetryDrawPickRiskTagCounts, telemetryWaveBaseDrawPickRiskTagCounts, telemetryDrawPickRiskTagCounts.Length);
        }

        private void EnsureTelemetryWaveBaseline()
        {
            if (model == null)
            {
                return;
            }

            bool resetDetected =
                model.PlacementAttemptCount < telemetryWaveBaseAttempts ||
                model.PlacementSuccessCount < telemetryWaveBaseSuccess ||
                model.AutoMergeSuccessCount < telemetryWaveBaseAutoMergeSuccess ||
                telemetryManualMergeSuccessCount < telemetryWaveBaseManualMergeSuccess ||
                telemetryRhythmBeatTransitionCount < telemetryWaveBaseRhythmBeatTransitionCount ||
                model.GetDrawChoicePickCount(0) < telemetryWaveBasePick1 ||
                model.GetDrawChoicePickCount(1) < telemetryWaveBasePick2 ||
                model.GetDrawChoicePickCount(2) < telemetryWaveBasePick3;

            if (!resetDetected)
            {
                for (int i = 0; i < telemetryDrawPickValueBucketCounts.Length; i++)
                {
                    if (telemetryDrawPickValueBucketCounts[i] < telemetryWaveBaseDrawPickValueBucketCounts[i])
                    {
                        resetDetected = true;
                        break;
                    }
                }
            }

            if (!resetDetected)
            {
                for (int i = 0; i < telemetryDrawPickRiskTagCounts.Length; i++)
                {
                    if (telemetryDrawPickRiskTagCounts[i] < telemetryWaveBaseDrawPickRiskTagCounts[i])
                    {
                        resetDetected = true;
                        break;
                    }
                }
            }

            if (telemetryWaveBaseWave < 0 || model.Wave != telemetryWaveBaseWave || resetDetected)
            {
                CaptureTelemetryWaveBaseline();
            }
        }

        private int ApplyTelemetryScope(int runValue, int waveBaseValue)
        {
            return telemetryWaveScope ? Mathf.Max(0, runValue - waveBaseValue) : runValue;
        }

        private int GetScopedTelemetryValueBucketCount(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= telemetryDrawPickValueBucketCounts.Length)
            {
                return 0;
            }

            return ApplyTelemetryScope(telemetryDrawPickValueBucketCounts[tierIndex], telemetryWaveBaseDrawPickValueBucketCounts[tierIndex]);
        }

        private int GetScopedTelemetryRiskTagCount(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= telemetryDrawPickRiskTagCounts.Length)
            {
                return 0;
            }

            return ApplyTelemetryScope(telemetryDrawPickRiskTagCounts[tierIndex], telemetryWaveBaseDrawPickRiskTagCounts[tierIndex]);
        }

        private void GetScopedTelemetryStats(
            out int attempts,
            out int success,
            out int directPlacementSuccess,
            out int autoMergeSuccess,
            out int manualMergeSuccess,
            out int blockedOutOfBounds,
            out int blockedOccupied,
            out int blockedInvalidAnchor,
            out int blockedNoPending,
            out int pick1,
            out int pick2,
            out int pick3,
            out int valueLow,
            out int valueMid,
            out int valueHigh,
            out int riskLow,
            out int riskMid,
            out int riskHigh)
        {
            attempts = 0;
            success = 0;
            directPlacementSuccess = 0;
            autoMergeSuccess = 0;
            manualMergeSuccess = 0;
            blockedOutOfBounds = 0;
            blockedOccupied = 0;
            blockedInvalidAnchor = 0;
            blockedNoPending = 0;
            pick1 = 0;
            pick2 = 0;
            pick3 = 0;
            valueLow = 0;
            valueMid = 0;
            valueHigh = 0;
            riskLow = 0;
            riskMid = 0;
            riskHigh = 0;

            if (model == null)
            {
                return;
            }

            EnsureTelemetryWaveBaseline();

            attempts = ApplyTelemetryScope(model.PlacementAttemptCount, telemetryWaveBaseAttempts);
            success = ApplyTelemetryScope(model.PlacementSuccessCount, telemetryWaveBaseSuccess);
            autoMergeSuccess = ApplyTelemetryScope(model.AutoMergeSuccessCount, telemetryWaveBaseAutoMergeSuccess);
            manualMergeSuccess = ApplyTelemetryScope(telemetryManualMergeSuccessCount, telemetryWaveBaseManualMergeSuccess);
            directPlacementSuccess = Mathf.Max(0, success - autoMergeSuccess);
            blockedOutOfBounds = ApplyTelemetryScope(model.PlacementBlockedOutOfBoundsCount, telemetryWaveBaseBlockedOutOfBounds);
            blockedOccupied = ApplyTelemetryScope(model.PlacementBlockedOccupiedCount, telemetryWaveBaseBlockedOccupied);
            blockedInvalidAnchor = ApplyTelemetryScope(model.PlacementBlockedInvalidAnchorCount, telemetryWaveBaseBlockedInvalidAnchor);
            blockedNoPending = ApplyTelemetryScope(model.PlacementBlockedNoPendingCount, telemetryWaveBaseBlockedNoPending);
            pick1 = ApplyTelemetryScope(model.GetDrawChoicePickCount(0), telemetryWaveBasePick1);
            pick2 = ApplyTelemetryScope(model.GetDrawChoicePickCount(1), telemetryWaveBasePick2);
            pick3 = ApplyTelemetryScope(model.GetDrawChoicePickCount(2), telemetryWaveBasePick3);
            valueLow = GetScopedTelemetryValueBucketCount(0);
            valueMid = GetScopedTelemetryValueBucketCount(1);
            valueHigh = GetScopedTelemetryValueBucketCount(2);
            riskLow = GetScopedTelemetryRiskTagCount(0);
            riskMid = GetScopedTelemetryRiskTagCount(1);
            riskHigh = GetScopedTelemetryRiskTagCount(2);
        }

        private void RefreshTelemetryControls()
        {
            if (telemetryToggleButtonText != null)
            {
                telemetryToggleButtonText.text = telemetryPanelExpanded ? "UX Panel On\n[Y]" : "UX Panel Off\n[Y]";
            }

            if (telemetryScopeButtonText != null)
            {
                telemetryScopeButtonText.text = telemetryWaveScope ? "Scope Wave\n[U]" : "Scope Run\n[U]";
            }

            if (telemetryToggleButton != null)
            {
                Image image = telemetryToggleButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = telemetryPanelExpanded
                        ? new Color(0.20f, 0.52f, 0.80f, 0.98f)
                        : new Color(0.22f, 0.28f, 0.36f, 0.97f);
                }
            }

            if (telemetryScopeButton != null)
            {
                Image image = telemetryScopeButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = telemetryWaveScope
                        ? new Color(0.25f, 0.45f, 0.28f, 0.97f)
                        : new Color(0.22f, 0.28f, 0.36f, 0.97f);
                }
            }

            if (telemetryPanelLayoutElement != null)
            {
                bool tallPortrait = IsTallPortraitLayout();
                telemetryPanelLayoutElement.preferredHeight = telemetryPanelExpanded ? (tallPortrait ? 156f : 134f) : 0f;
                telemetryPanelLayoutElement.minHeight = telemetryPanelExpanded ? (tallPortrait ? 132f : 112f) : 0f;
            }

            if (telemetryPanelRect != null)
            {
                telemetryPanelRect.gameObject.SetActive(telemetryPanelExpanded);
            }

            if (telemetryPanelText != null)
            {
                telemetryPanelText.fontSize = IsTallPortraitLayout() ? 12 : 13;
            }
        }

        private void RefreshTelemetryPanel()
        {
            if (telemetryPanelText == null || model == null)
            {
                return;
            }

            GetScopedTelemetryStats(
                out int attempts,
                out int success,
                out int directPlacementSuccess,
                out int autoMergeSuccess,
                out int manualMergeSuccess,
                out int blockedOutOfBounds,
                out int blockedOccupied,
                out int blockedInvalidAnchor,
                out int blockedNoPending,
                out int pick1,
                out int pick2,
                out int pick3,
                out int valueLow,
                out int valueMid,
                out int valueHigh,
                out int riskLow,
                out int riskMid,
                out int riskHigh);

            float successRate = attempts > 0 ? (float)success / attempts : 0f;
            float directRate = attempts > 0 ? (float)directPlacementSuccess / attempts : 0f;
            float autoShare = success > 0 ? (float)autoMergeSuccess / success : 0f;
            string scopeLabel = telemetryWaveScope ? "Wave " + model.Wave : "Run";
            string coachLine = BuildTelemetryCoachLine(attempts, success, blockedOutOfBounds, blockedOccupied, blockedInvalidAnchor);

            telemetryPanelText.text =
                "UX Telemetry (" + scopeLabel + ")\n" +
                BuildRhythmBeatTelemetryLine(
                    model.CurrentRhythmBeatLabel,
                    telemetryCurrentRhythmBeatSeconds,
                    ApplyTelemetryScope(telemetryRhythmBeatTransitionCount, telemetryWaveBaseRhythmBeatTransitionCount),
                    model.LastWaveCadenceSummary) + "\n" +
                "Draw Assist: " + model.DrawAssistTag + "\n" +
                "Placement: " + success + "/" + attempts + " (" + (successRate * 100f).ToString("0") + "%)\n" +
                "Placement Type: Direct " + directPlacementSuccess + " (" + (directRate * 100f).ToString("0") + "%) | AutoMerge " + autoMergeSuccess + " (" + (autoShare * 100f).ToString("0") + "%)\n" +
                "Merges: Manual " + manualMergeSuccess + " | Auto " + autoMergeSuccess + "\n" +
                "Blocked: Bound " + blockedOutOfBounds + " | Occupied " + blockedOccupied + " | Anchor " + blockedInvalidAnchor + " | NoPending " + blockedNoPending + "\n" +
                "Draw Picks: [1] " + pick1 + " | [2] " + pick2 + " | [3] " + pick3 + "\n" +
                "Pick Value: LOW " + valueLow + " | MID " + valueMid + " | HIGH " + valueHigh + "\n" +
                "Pick Risk: LOW " + riskLow + " | MID " + riskMid + " | HIGH " + riskHigh + "\n" +
                "Coach: " + coachLine;
        }

        private static string BuildTelemetryCoachLine(int attempts, int success, int blockedOutOfBounds, int blockedOccupied, int blockedInvalidAnchor)
        {
            if (attempts < 4)
            {
                return "Need more samples. Keep placing blocks to read flow friction.";
            }

            float successRate = (float)success / Mathf.Max(1, attempts);
            if (successRate < 0.45f)
            {
                return "Placement friction is high. Offer simpler shapes or larger empty slots.";
            }

            if (blockedOccupied > blockedOutOfBounds && blockedOccupied > blockedInvalidAnchor)
            {
                return "Occupied blocks dominate. Encourage merge/sell or reward early spacing.";
            }

            if (blockedOutOfBounds >= blockedOccupied)
            {
                return "Bounds misses are frequent. Surface rotation hints before drag release.";
            }

            return "Flow is stable. Add streak rewards for clean placement chains.";
        }

        private string BuildTelemetryMiniLine()
        {
            if (model == null)
            {
                return string.Empty;
            }

            GetScopedTelemetryStats(
                out int attempts,
                out int success,
                out int directPlacementSuccess,
                out int autoMergeSuccess,
                out int manualMergeSuccess,
                out int blockedOutOfBounds,
                out int blockedOccupied,
                out int blockedInvalidAnchor,
                out int blockedNoPending,
                out int pick1,
                out int pick2,
                out int pick3,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _);

            int pickTotal = pick1 + pick2 + pick3;
            if (attempts <= 0 && pickTotal <= 0)
            {
                return string.Empty;
            }

            float successRate = attempts > 0 ? (float)success / attempts : 0f;
            string scopeTag = telemetryWaveScope ? "W" + model.Wave : "RUN";

            return
                "   UX[" + scopeTag + "] P " + success + "/" + attempts +
                "(" + (successRate * 100f).ToString("0") + "%)" +
                " " + BuildRhythmBeatMiniText(model.CurrentRhythmBeatLabel, telemetryCurrentRhythmBeatSeconds) +
                " M[d" + directPlacementSuccess + " a" + autoMergeSuccess + " m" + manualMergeSuccess + "]" +
                " B[o" + blockedOutOfBounds +
                " c" + blockedOccupied +
                " a" + blockedInvalidAnchor +
                " n" + blockedNoPending + "]" +
                " Pick[" + pick1 + "/" + pick2 + "/" + pick3 + "]";
        }

        public static string BuildRhythmBeatTelemetryLine(string beatLabel, float beatSeconds, int transitionCount, string cadenceSummary)
        {
            string safeBeat = string.IsNullOrEmpty(beatLabel) ? "Unknown" : beatLabel;
            string cadence = string.IsNullOrEmpty(cadenceSummary) ? "No cadence" : cadenceSummary;
            return "Rhythm Beat: " + safeBeat +
                " " + Mathf.Max(0f, beatSeconds).ToString("0.0", CultureInfo.InvariantCulture) + "s" +
                " | Transitions " + Mathf.Max(0, transitionCount) +
                " | Cadence: " + cadence;
        }

        public static string BuildRhythmBeatMiniText(string beatLabel, float beatSeconds)
        {
            string safeBeat = string.IsNullOrEmpty(beatLabel) ? "Unknown" : beatLabel;
            return "R:" + safeBeat + " " + Mathf.Max(0f, beatSeconds).ToString("0", CultureInfo.InvariantCulture) + "s";
        }
    }
}
