using System;
using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void UpdateFlowChecklist()
        {
            if (flowChecklistTexts == null || flowChecklistTexts.Length < 5 || model == null)
            {
                return;
            }

            bool chooseReady = model.HasDrawChoice;
            bool pendingReady = model.HasPendingBlock;
            bool placementReady = pendingReady && pendingHoverAnchorCell >= 0 && pendingHoverValid;
            bool placementBlocked = pendingReady && pendingHoverAnchorCell >= 0 && !pendingHoverValid;
            bool waveLive = !model.IsRestPhase && !model.EventPending;

            string drawLine = (chooseReady || pendingReady ? "[OK]" : "[ ]") + " Draw: spend supplies to reveal 3 options";
            string chooseLine = (chooseReady ? "[NOW]" : (pendingReady ? "[OK]" : "[ ]")) + " Choose: pick 1 of 3 blocks";
            string rotateLine = (pendingReady ? "[LIVE]" : "[ ]") + " Rotate: Q/E or ROT L/R to fit the grid";

            string placeState = placementReady ? "[READY]" : (placementBlocked ? "[BLOCKED]" : (pendingReady ? "[NEXT]" : "[ ]"));
            string placeLine = "Place: drag or tap a valid slot";
            if (placementBlocked)
            {
                string blockedReason = BuildPlacementBlockedHint();
                if (!string.IsNullOrEmpty(blockedReason))
                {
                    placeLine += " (" + blockedReason + ")";
                }
            }

            string placeChecklistLine = placeState + " " + placeLine;
            string waveLine = (waveLive ? "[LIVE]" : "[REST]") + " Wave: survive combat, manage heat, then reset";

            flowChecklistTexts[0].text = drawLine;
            flowChecklistTexts[1].text = chooseLine;
            flowChecklistTexts[2].text = rotateLine;
            flowChecklistTexts[3].text = placeChecklistLine;
            flowChecklistTexts[4].text = waveLine;

            TrySyncChecklistToPlayLog(drawLine, chooseLine, rotateLine, placeChecklistLine, waveLine);
        }

        private void TrySyncChecklistToPlayLog(string drawLine, string chooseLine, string rotateLine, string placeLine, string waveLine)
        {
            if (!syncChecklistToPlayLog || model == null)
            {
                return;
            }

            if (!flowChecklistLogPrimed)
            {
                lastFlowChecklistLogLines[0] = drawLine;
                lastFlowChecklistLogLines[1] = chooseLine;
                lastFlowChecklistLogLines[2] = rotateLine;
                lastFlowChecklistLogLines[3] = placeLine;
                lastFlowChecklistLogLines[4] = waveLine;
                flowChecklistLogPrimed = true;
                return;
            }

            int selectedPriority = int.MinValue;
            string selectedLine = null;

            UpdateChecklistChangeCandidate(0, drawLine, ref selectedLine, ref selectedPriority);
            UpdateChecklistChangeCandidate(1, chooseLine, ref selectedLine, ref selectedPriority);
            UpdateChecklistChangeCandidate(2, rotateLine, ref selectedLine, ref selectedPriority);
            UpdateChecklistChangeCandidate(3, placeLine, ref selectedLine, ref selectedPriority);
            UpdateChecklistChangeCandidate(4, waveLine, ref selectedLine, ref selectedPriority);

            if (string.IsNullOrEmpty(selectedLine) || flowChecklistLogCooldownTimer > 0f)
            {
                return;
            }

            string tag = GetChecklistTag(selectedLine);
            if (!ShouldEmitChecklistLog(tag))
            {
                return;
            }

            string message = "Flow: " + selectedLine;
            if (string.Equals(lastFlowChecklistLogMessage, message, StringComparison.Ordinal))
            {
                return;
            }

            AppendLog(message);
            lastFlowChecklistLogMessage = message;
            flowChecklistLogCooldownTimer = Mathf.Max(0.10f, checklistLogMinInterval);
        }

        private void UpdateChecklistChangeCandidate(int index, string currentLine, ref string selectedLine, ref int selectedPriority)
        {
            if (string.Equals(lastFlowChecklistLogLines[index], currentLine, StringComparison.Ordinal))
            {
                return;
            }

            lastFlowChecklistLogLines[index] = currentLine;
            int priority = GetChecklistLinePriority(currentLine, index);
            if (priority > selectedPriority)
            {
                selectedPriority = priority;
                selectedLine = currentLine;
            }
        }

        private static int GetChecklistLinePriority(string line, int index)
        {
            string tag = GetChecklistTag(line);
            int indexBonus = index == 3 ? 4 : (index == 1 ? 2 : 0);
            switch (tag)
            {
                case "BLOCKED":
                    return 100 + indexBonus;
                case "READY":
                    return 90 + indexBonus;
                case "NOW":
                    return 86 + indexBonus;
                case "LIVE":
                    return 74 + indexBonus;
                case "REST":
                    return 62 + indexBonus;
                case "NEXT":
                    return 56 + indexBonus;
                case "OK":
                    return 24 + indexBonus;
                default:
                    return 8 + indexBonus;
            }
        }

        private static bool ShouldEmitChecklistLog(string tag)
        {
            switch (tag)
            {
                case "BLOCKED":
                case "READY":
                case "NOW":
                case "LIVE":
                case "REST":
                case "NEXT":
                    return true;
                default:
                    return false;
            }
        }

        private static string GetChecklistTag(string line)
        {
            if (string.IsNullOrEmpty(line) || line[0] != '[')
            {
                return string.Empty;
            }

            int closeIndex = line.IndexOf(']');
            if (closeIndex <= 1)
            {
                return string.Empty;
            }

            return line.Substring(1, closeIndex - 1);
        }
    }
}
