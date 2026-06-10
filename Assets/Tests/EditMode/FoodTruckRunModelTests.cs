using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ZombieFoodcenter.Prototype;

namespace ZombieFoodcenter.Tests.EditMode
{
    public class FoodTruckRunModelTests
    {
        [Test]
        public void ResetRun_InitialCoreState_IsStable()
        {
            var model = new FoodTruckRunModel(seed: 1234);

            Assert.AreEqual(1, model.Wave);
            Assert.AreEqual(16, model.Supplies);
            Assert.IsFalse(model.HasDrawChoice);
            Assert.IsFalse(model.HasPendingBlock);
            Assert.AreEqual(0, model.PlacementAttemptCount);
            Assert.AreEqual(0, model.PlacementSuccessCount);
        }

        [Test]
        public void DrawIngredient_WhenEnoughSupplies_OffersThreeChoicesAndSpendsCost()
        {
            var model = new FoodTruckRunModel(seed: 42);
            int beforeSupplies = model.Supplies;
            int drawCost = model.GetDrawCost();

            bool drawn = model.DrawIngredient();

            Assert.IsTrue(drawn);
            Assert.IsTrue(model.HasDrawChoice);
            Assert.AreEqual(3, model.DrawChoices.Count);
            Assert.AreEqual(beforeSupplies - drawCost, model.Supplies);
        }

        [Test]
        public void ChooseDrawOption_TracksPickAndCreatesPendingBlock()
        {
            var model = new FoodTruckRunModel(seed: 42);
            Assert.IsTrue(model.DrawIngredient());

            bool chosen = model.ChooseDrawOption(0);

            Assert.IsTrue(chosen);
            Assert.IsFalse(model.HasDrawChoice);
            Assert.IsTrue(model.HasPendingBlock);
            Assert.AreEqual(1, model.GetDrawChoicePickCount(0));
            Assert.AreEqual(0, model.PendingRotationDegrees);
        }

        [Test]
        public void TryPlacePendingAtCell_WithoutPendingBlock_RecordsNoPendingFailure()
        {
            var model = new FoodTruckRunModel(seed: 7);

            bool placed = model.TryPlacePendingAtCell(0);

            Assert.IsFalse(placed);
            Assert.AreEqual(PlacementFailReason.NoPendingBlock, model.LastPlacementFailReason);
            Assert.AreEqual(1, model.PlacementAttemptCount);
            Assert.AreEqual(1, model.PlacementBlockedNoPendingCount);
        }

        [Test]
        public void TryPlacePendingAtCell_InvalidAnchor_RecordsInvalidAnchorFailure()
        {
            var model = new FoodTruckRunModel(seed: 7);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));

            bool placed = model.TryPlacePendingAtCell(-1);

            Assert.IsFalse(placed);
            Assert.AreEqual(PlacementFailReason.InvalidAnchor, model.LastPlacementFailReason);
            Assert.AreEqual(1, model.PlacementAttemptCount);
            Assert.AreEqual(1, model.PlacementBlockedInvalidAnchorCount);
            Assert.IsTrue(model.HasPendingBlock);
        }

        [Test]
        public void TryPlacePendingAtCell_ValidAnchor_SucceedsAndOccupiesInventory()
        {
            var model = new FoodTruckRunModel(seed: 7);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));

            bool placed = model.TryPlacePendingAtCell(0);

            Assert.IsTrue(placed);
            Assert.IsFalse(model.HasPendingBlock);
            Assert.AreEqual(1, model.PlacementAttemptCount);
            Assert.AreEqual(1, model.PlacementSuccessCount);
            Assert.AreEqual(0, model.AutoMergeSuccessCount);
            Assert.AreEqual(1, model.DirectPlacementSuccessCount);
            Assert.Greater(model.InventoryCellsFilled, 0);
        }

        [Test]
        public void VentHeat_WhenPrerequisitesNotMet_FailsWithoutSupplyLoss()
        {
            var model = new FoodTruckRunModel(seed: 123);
            int beforeSupplies = model.Supplies;
            float beforeHeat = model.Heat;

            bool vented = model.VentHeat();

            Assert.IsFalse(vented);
            Assert.AreEqual(beforeSupplies, model.Supplies);
            Assert.AreEqual(beforeHeat, model.Heat, 0.0001f);
            Assert.IsFalse(model.CanVentHeat);
        }

        [Test]
        public void ChooseDrawOption_WhenIndexOutOfRange_FailsAndKeepsChoices()
        {
            var model = new FoodTruckRunModel(seed: 11);
            Assert.IsTrue(model.DrawIngredient());

            bool chosen = model.ChooseDrawOption(99);

            Assert.IsFalse(chosen);
            Assert.IsTrue(model.HasDrawChoice);
            Assert.IsFalse(model.HasPendingBlock);
            Assert.AreEqual(0, model.DrawChoicePickTotal);
        }

        [Test]
        public void DrawIngredient_WhenPendingBlockExists_FailsWithoutSpendingSupplies()
        {
            var model = new FoodTruckRunModel(seed: 12);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));
            int beforeSupplies = model.Supplies;

            bool drawn = model.DrawIngredient();

            Assert.IsFalse(drawn);
            Assert.AreEqual(beforeSupplies, model.Supplies);
            Assert.IsTrue(model.HasPendingBlock);
            Assert.IsFalse(model.HasDrawChoice);
        }

        [Test]
        public void DrawIngredient_AfterWaveChoicePlacement_RefreshesButPausesNextWaveUntilPlaced()
        {
            var model = new FoodTruckRunModel(seed: 7);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));
            Assert.IsTrue(model.TryPlacePendingAtCell(0));
            Assert.IsTrue(model.WaveBlockChoiceUsed);
            Assert.IsFalse(model.CanDrawIngredient);
            StringAssert.Contains("Next choice on wave 2", model.DrawLockReason);

            int suppliesBeforeLockedDraw = model.Supplies;
            Assert.IsFalse(model.DrawIngredient());
            Assert.AreEqual(suppliesBeforeLockedDraw, model.Supplies);

            SetPrivateField(model, "waveTimer", 19f);
            SetAutoProperty(model, "Threat", 0f);
            SetAutoProperty(model, "MaxTruckHp", 100000f);
            SetAutoProperty(model, "TruckHp", 100000f);

            model.Tick(1f);

            Assert.AreEqual(2, model.Wave);
            Assert.IsFalse(model.WaveBlockChoiceUsed);
            Assert.IsTrue(model.CanDrawIngredient);
            Assert.IsTrue(model.CombatFlowLocked);
            Assert.AreEqual("Take wave choice to start combat", model.CombatFlowLockReason);

            model.Tick(3f);

            Assert.AreEqual(0f, GetPrivateField<float>(model, "waveTimer"), 0.0001f);
            Assert.AreEqual(2, model.Wave);

            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));
            PlacePendingAtFirstValidCell(model);

            Assert.IsTrue(model.WaveBlockChoiceUsed);
            Assert.IsFalse(model.CombatFlowLocked);
        }

        [Test]
        public void SellIngredient_WithPendingBeforePlacement_ReopensWaveChoice()
        {
            var model = new FoodTruckRunModel(seed: 14);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));

            Assert.IsTrue(model.SellIngredient());

            Assert.IsFalse(model.HasPendingBlock);
            Assert.IsFalse(model.WaveBlockChoiceUsed);
            Assert.IsTrue(model.CanDrawIngredient);
        }

        [Test]
        public void SellIngredient_LastCommittedWaveBlock_FailsToAvoidPreparationDeadlock()
        {
            var model = new FoodTruckRunModel(seed: 7);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));
            Assert.IsTrue(model.TryPlacePendingAtCell(0));
            int filledBeforeSell = model.InventoryCellsFilled;

            bool sold = model.SellIngredient();

            Assert.IsFalse(sold);
            Assert.AreEqual(filledBeforeSell, model.InventoryCellsFilled);
            Assert.IsFalse(model.CombatFlowLocked);
        }

        [Test]
        public void RotatePendingClockwise_AdvancesRotationByNinetyDegrees()
        {
            var model = new FoodTruckRunModel(seed: 13);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));

            bool rotated = model.RotatePendingClockwise();

            Assert.IsTrue(rotated);
            Assert.AreEqual(90, model.PendingRotationDegrees);
        }

        [Test]
        public void SellIngredient_WithPendingBlock_RefundsSuppliesAndClearsPending()
        {
            var model = new FoodTruckRunModel(seed: 14);
            int beforeDrawSupplies = model.Supplies;
            Assert.IsTrue(model.DrawIngredient());
            int afterDrawSupplies = model.Supplies;
            Assert.IsTrue(model.ChooseDrawOption(0));

            bool sold = model.SellIngredient();

            Assert.IsTrue(sold);
            Assert.IsFalse(model.HasPendingBlock);
            Assert.AreEqual(afterDrawSupplies + 2, model.Supplies);
            Assert.Less(model.Supplies, beforeDrawSupplies);
        }

        [Test]
        public void TryGetPendingFootprintCellsPreview_DoesNotOverwriteLastFailureReason()
        {
            var model = new FoodTruckRunModel(seed: 15);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));
            Assert.AreEqual(PlacementFailReason.None, model.LastPlacementFailReason);

            bool previewOk = model.TryGetPendingFootprintCellsPreview(-1, true, out _);

            Assert.IsFalse(previewOk);
            Assert.AreEqual(PlacementFailReason.None, model.LastPlacementFailReason);
        }

        [Test]
        public void SetBattleSpeed_ClampsToSupportedRange()
        {
            var model = new FoodTruckRunModel(seed: 16);

            model.SetBattleSpeed(0.25f);
            Assert.AreEqual(1f, model.BattleSpeed, 0.0001f);

            model.SetBattleSpeed(1.5f);
            Assert.AreEqual(1.5f, model.BattleSpeed, 0.0001f);

            model.SetBattleSpeed(8f);
            Assert.AreEqual(2f, model.BattleSpeed, 0.0001f);
        }

        [Test]
        public void Tick_LargeDelta_DoesNotProcessUnboundedSeconds()
        {
            var model = new FoodTruckRunModel(seed: 161);
            SeedStarterBlock(model);
            SetPrivateField(model, "waveTimer", 19f);
            SetAutoProperty(model, "Threat", 0f);
            SetAutoProperty(model, "MaxTruckHp", 100000f);
            SetAutoProperty(model, "TruckHp", 100000f);

            model.Tick(100f);

            Assert.AreEqual(2, model.Wave);
            float accumulator = GetPrivateField<float>(model, "simulationAccumulator");
            Assert.GreaterOrEqual(accumulator, 0f);
            Assert.Less(accumulator, 1f);
        }

        [Test]
        public void Tick_BeforeFirstBlock_DoesNotStartCombatOrWaveTimer()
        {
            var model = new FoodTruckRunModel(seed: 1601);
            SetAutoProperty(model, "Threat", 100f);
            SetAutoProperty(model, "Heat", 100f);

            model.Tick(5f);

            Assert.IsTrue(model.CombatFlowLocked);
            Assert.AreEqual("Draw/place 1 block to start combat", model.CombatFlowLockReason);
            Assert.AreEqual(0f, GetPrivateField<float>(model, "waveTimer"), 0.0001f);
            Assert.AreEqual(0, model.LaneEnemies.Count);
            Assert.AreEqual(120f, model.TruckHp, 0.0001f);
            Assert.AreEqual(PressureRampPhase.Build, model.CurrentPressureRampPhase);
            Assert.AreEqual(RhythmBeatType.Read, model.CurrentRhythmBeat);
        }

        [Test]
        public void Tick_WithPendingBlock_DoesNotStartCombatUntilPlaced()
        {
            var model = new FoodTruckRunModel(seed: 1602);
            Assert.IsTrue(model.DrawIngredient());
            Assert.IsTrue(model.ChooseDrawOption(0));
            SetAutoProperty(model, "Threat", 100f);

            model.Tick(3f);

            Assert.IsTrue(model.CombatFlowLocked);
            Assert.AreEqual("Place pending block to start combat", model.CombatFlowLockReason);
            Assert.AreEqual(0f, GetPrivateField<float>(model, "waveTimer"), 0.0001f);
            Assert.AreEqual(0, model.LaneEnemies.Count);

            Assert.IsTrue(model.TryPlacePendingAtCell(0));
            Assert.IsFalse(model.CombatFlowLocked);

            model.Tick(1f);

            Assert.Greater(GetPrivateField<float>(model, "waveTimer"), 0f);
        }

        [Test]
        public void Tick_WhenWaveAdvances_RecordsOutcomeSummary()
        {
            var model = new FoodTruckRunModel(seed: 162);
            SeedStarterBlock(model);
            SetPrivateField(model, "waveTimer", 19f);
            SetAutoProperty(model, "Threat", 0f);
            SetAutoProperty(model, "TruckHp", 95f);
            SetAutoProperty(model, "Heat", 12f);
            SetPrivateField(model, "waveStartTruckHp", 100f);
            SetPrivateField(model, "waveStartHeat", 5f);
            SetPrivateField(model, "waveStartSupplies", 10);
            SetPrivateField(model, "waveDamageDealt", 42f);
            SetPrivateField(model, "waveEnemiesDefeated", 2);
            SetPrivateField(model, "waveTruckHits", 1);
            SetPrivateField(model, "waveBestComboStreak", 3);
            SetPrivateField(model, "wavePeakHeat", 18f);

            model.Tick(1f);

            Assert.AreEqual(2, model.Wave);
            Assert.AreEqual("Wave 1: 2 KO, HP -5, Heat +7.", model.LastWaveOutcomeCue);
            StringAssert.Contains("Wave 1:", model.LastWaveOutcomeSummary);
            StringAssert.Contains("Sup +6", model.LastWaveOutcomeSummary);
            StringAssert.Contains("Dmg 42", model.LastWaveOutcomeSummary);
            StringAssert.Contains("PeakHeat +13", model.LastWaveOutcomeSummary);
            StringAssert.Contains("Combo x3", model.LastWaveOutcomeSummary);
            StringAssert.Contains("Leak x1", model.LastWaveOutcomeSummary);
            StringAssert.Contains("Best combo x3", model.LastWaveOutcomeSummary);
            Assert.AreEqual("Best combo x3", model.LastWaveOutcomeBestContributor);
            Assert.AreEqual("PeakHeat +13 -> Pick COOL/SAFE", model.LastWaveOutcomeNextHint);
        }

        [Test]
        public void BuildWaveOutcomeBestContributor_PrioritizesVisiblePayoffCause()
        {
            Assert.AreEqual(
                "Best KO chain",
                FoodTruckRunModel.BuildWaveOutcomeBestContributor(
                    enemiesDefeated: 3,
                    damageDealt: 20f,
                    bestComboStreak: 1,
                    suppliesDelta: 0,
                    hpDelta: 0f,
                    heatDelta: 0f));

            Assert.AreEqual(
                "Best damage",
                FoodTruckRunModel.BuildWaveOutcomeBestContributor(
                    enemiesDefeated: 1,
                    damageDealt: 50f,
                    bestComboStreak: 1,
                    suppliesDelta: 0,
                    hpDelta: 0f,
                    heatDelta: 0f));

            Assert.AreEqual(
                "Best cooling",
                FoodTruckRunModel.BuildWaveOutcomeBestContributor(
                    enemiesDefeated: 0,
                    damageDealt: 0f,
                    bestComboStreak: 0,
                    suppliesDelta: 0,
                    hpDelta: 0f,
                    heatDelta: -8f));
        }

        [Test]
        public void BuildWaveOutcomeNextHint_WhenTruckLeaks_PrioritizesLaneStability()
        {
            string hint = FoodTruckRunModel.BuildWaveOutcomeNextHint(
                truckHits: 2,
                hpDelta: -12f,
                heatDelta: 2f,
                peakHeatDelta: 3f,
                enemiesDefeated: 1,
                damageDealt: 20f,
                bestComboStreak: 1);

            Assert.AreEqual("Leak x2 -> Stabilize lanes", hint);
        }

        [Test]
        public void BuildWaveOutcomeNextHint_WhenHeatSpikes_RecommendsSafeCoolingDraw()
        {
            string hint = FoodTruckRunModel.BuildWaveOutcomeNextHint(
                truckHits: 0,
                hpDelta: -2f,
                heatDelta: 15f,
                peakHeatDelta: 19f,
                enemiesDefeated: 2,
                damageDealt: 30f,
                bestComboStreak: 2);

            Assert.AreEqual("PeakHeat +19 -> Pick COOL/SAFE", hint);
        }

        [Test]
        public void BuildWaveOutcomeNextHint_WhenDamagePaysOff_RecommendsPushingDamage()
        {
            string hint = FoodTruckRunModel.BuildWaveOutcomeNextHint(
                truckHits: 0,
                hpDelta: 0f,
                heatDelta: 4f,
                peakHeatDelta: 6f,
                enemiesDefeated: 3,
                damageDealt: 48f,
                bestComboStreak: 1);

            Assert.AreEqual("3 KO -> Push damage", hint);
        }

        [Test]
        public void BuildPayoffToReadHintChipText_TrimsLongHints()
        {
            string chipText = FoodTruckPrototypeHud.BuildPayoffToReadHintChipText("Keep combo window with a very long explanation");

            Assert.LessOrEqual(chipText.Length, 34);
            StringAssert.EndsWith("...", chipText);
        }

        [Test]
        public void ResolveRhythmBeat_ReadStates_PrioritizeChoices()
        {
            Assert.AreEqual(
                RhythmBeatType.Read,
                FoodTruckRunModel.ResolveRhythmBeat(
                    isRestPhase: false,
                    eventPending: true,
                    hasDrawChoice: false,
                    hasPendingBlock: false,
                    hasPayoffHint: false,
                    waveProgress01: 0.5f));

            Assert.AreEqual(
                RhythmBeatType.Read,
                FoodTruckRunModel.ResolveRhythmBeat(
                    isRestPhase: false,
                    eventPending: false,
                    hasDrawChoice: true,
                    hasPendingBlock: false,
                    hasPayoffHint: true,
                    waveProgress01: 0.02f));
        }

        [Test]
        public void ResolveRhythmBeat_CommitAndReleaseStates_AreExplicit()
        {
            Assert.AreEqual(
                RhythmBeatType.Commit,
                FoodTruckRunModel.ResolveRhythmBeat(
                    isRestPhase: false,
                    eventPending: false,
                    hasDrawChoice: false,
                    hasPendingBlock: true,
                    hasPayoffHint: false,
                    waveProgress01: 0.3f));

            Assert.AreEqual(
                RhythmBeatType.Release,
                FoodTruckRunModel.ResolveRhythmBeat(
                    isRestPhase: true,
                    eventPending: false,
                    hasDrawChoice: false,
                    hasPendingBlock: true,
                    hasPayoffHint: true,
                    waveProgress01: 0f));
        }

        [Test]
        public void ResolveRhythmBeat_PayoffOnlyCoversEarlyWaveWindow()
        {
            Assert.AreEqual(
                RhythmBeatType.Payoff,
                FoodTruckRunModel.ResolveRhythmBeat(
                    isRestPhase: false,
                    eventPending: false,
                    hasDrawChoice: false,
                    hasPendingBlock: false,
                    hasPayoffHint: true,
                    waveProgress01: 0.08f));

            Assert.AreEqual(
                RhythmBeatType.Pressure,
                FoodTruckRunModel.ResolveRhythmBeat(
                    isRestPhase: false,
                    eventPending: false,
                    hasDrawChoice: false,
                    hasPendingBlock: false,
                    hasPayoffHint: true,
                    waveProgress01: 0.45f));
        }

        [Test]
        public void BuildRhythmBeatLabel_ReturnsPlayerFacingNames()
        {
            Assert.AreEqual("Read", FoodTruckRunModel.BuildRhythmBeatLabel(RhythmBeatType.Read));
            Assert.AreEqual("Commit", FoodTruckRunModel.BuildRhythmBeatLabel(RhythmBeatType.Commit));
            Assert.AreEqual("Pressure", FoodTruckRunModel.BuildRhythmBeatLabel(RhythmBeatType.Pressure));
            Assert.AreEqual("Payoff", FoodTruckRunModel.BuildRhythmBeatLabel(RhythmBeatType.Payoff));
            Assert.AreEqual("Release", FoodTruckRunModel.BuildRhythmBeatLabel(RhythmBeatType.Release));
        }

        [Test]
        public void BuildRhythmBeatTelemetryLine_IncludesDurationTransitionsAndCadence()
        {
            string line = FoodTruckPrototypeHud.BuildRhythmBeatTelemetryLine(
                "Pressure",
                12.34f,
                3,
                "Wave 4: Unlock, Weather (planned spike)");

            StringAssert.Contains("Rhythm Beat: Pressure 12.3s", line);
            StringAssert.Contains("Transitions 3", line);
            StringAssert.Contains("Wave 4: Unlock, Weather", line);
        }

        [Test]
        public void BuildRhythmBeatMiniText_ClampsNegativeDuration()
        {
            string text = FoodTruckPrototypeHud.BuildRhythmBeatMiniText("Payoff", -4f);

            Assert.AreEqual("R:Payoff 0s", text);
        }

        [Test]
        public void ResolvePressureRampPhase_MapsWaveProgressToBuildClimbPeak()
        {
            Assert.AreEqual(
                PressureRampPhase.Build,
                FoodTruckRunModel.ResolvePressureRampPhase(0.20f, false, false, false));
            Assert.AreEqual(
                PressureRampPhase.Climb,
                FoodTruckRunModel.ResolvePressureRampPhase(0.50f, false, false, false));
            Assert.AreEqual(
                PressureRampPhase.Peak,
                FoodTruckRunModel.ResolvePressureRampPhase(0.90f, false, false, false));
        }

        [Test]
        public void ResolvePressureRampPhase_WhenFlowLocked_ReturnsBuild()
        {
            Assert.AreEqual(
                PressureRampPhase.Build,
                FoodTruckRunModel.ResolvePressureRampPhase(0.95f, true, false, false));
            Assert.AreEqual(
                PressureRampPhase.Build,
                FoodTruckRunModel.ResolvePressureRampPhase(0.95f, false, true, false));
            Assert.AreEqual(
                PressureRampPhase.Build,
                FoodTruckRunModel.ResolvePressureRampPhase(0.95f, false, false, true));
        }

        [Test]
        public void BuildPressureRampIntensity_UsesSmoothProgressAndFlowLocks()
        {
            Assert.AreEqual(0f, FoodTruckRunModel.BuildPressureRampIntensity01(0.8f, true, false, false), 0.0001f);
            Assert.AreEqual(0f, FoodTruckRunModel.BuildPressureRampIntensity01(0f, false, false, false), 0.0001f);
            Assert.AreEqual(0.5f, FoodTruckRunModel.BuildPressureRampIntensity01(0.5f, false, false, false), 0.0001f);
            Assert.AreEqual(1f, FoodTruckRunModel.BuildPressureRampIntensity01(1f, false, false, false), 0.0001f);
        }

        [Test]
        public void BuildPressureRampSpawnMultiplier_EasesFromLowToPeakPressure()
        {
            Assert.AreEqual(1f, FoodTruckRunModel.BuildPressureRampSpawnMultiplier(0.9f, true, false, false), 0.0001f);
            Assert.AreEqual(0.72f, FoodTruckRunModel.BuildPressureRampSpawnMultiplier(0f, false, false, false), 0.0001f);
            Assert.AreEqual(1.0f, FoodTruckRunModel.BuildPressureRampSpawnMultiplier(0.5f, false, false, false), 0.0001f);
            Assert.AreEqual(1.28f, FoodTruckRunModel.BuildPressureRampSpawnMultiplier(1f, false, false, false), 0.0001f);
        }

        [Test]
        public void ResolveWavePressureTheme_MapsCadenceToReadableThemes()
        {
            Assert.AreEqual(WavePressureTheme.LaneRush, FoodTruckRunModel.ResolveWavePressureTheme(2, false, false, false));
            Assert.AreEqual(WavePressureTheme.Swarm, FoodTruckRunModel.ResolveWavePressureTheme(3, false, false, true));
            Assert.AreEqual(WavePressureTheme.HeatSurge, FoodTruckRunModel.ResolveWavePressureTheme(4, false, true, false));
            Assert.AreEqual(WavePressureTheme.Bruiser, FoodTruckRunModel.ResolveWavePressureTheme(5, true, false, false));
            Assert.AreEqual(WavePressureTheme.Balanced, FoodTruckRunModel.ResolveWavePressureTheme(7, false, false, false));
        }

        [Test]
        public void BuildWavePressureThemeLabelAndHint_ArePlayerFacing()
        {
            Assert.AreEqual(1, FoodTruckRunModel.ResolveWavePressureThemeLaneIndex(2, WavePressureTheme.LaneRush));
            Assert.AreEqual("Lane Rush L2", FoodTruckRunModel.BuildWavePressureThemeLabel(WavePressureTheme.LaneRush, 1));
            Assert.AreEqual("Cover L2 first", FoodTruckRunModel.BuildWavePressureThemeHint(WavePressureTheme.LaneRush, 1));
            Assert.AreEqual("Swarm", FoodTruckRunModel.BuildWavePressureThemeLabel(WavePressureTheme.Swarm, -1));
            Assert.AreEqual("Thin small waves", FoodTruckRunModel.BuildWavePressureThemeHint(WavePressureTheme.Swarm, -1));
            Assert.AreEqual("Heat Surge", FoodTruckRunModel.BuildWavePressureThemeLabel(WavePressureTheme.HeatSurge, -1));
            Assert.AreEqual("Cool before burst", FoodTruckRunModel.BuildWavePressureThemeHint(WavePressureTheme.HeatSurge, -1));
        }

        [Test]
        public void ResolveRestRewardProfile_PrioritizesRepairCoolingThenStock()
        {
            Assert.AreEqual(
                RestRewardProfile.Repair,
                FoodTruckRunModel.ResolveRestRewardProfile(
                    truckHits: 2,
                    truckHp01: 0.9f,
                    heatDelta: 0f,
                    peakHeatDelta: 0f,
                    enemiesDefeated: 5,
                    comboActions: 5));

            Assert.AreEqual(
                RestRewardProfile.Cooling,
                FoodTruckRunModel.ResolveRestRewardProfile(
                    truckHits: 0,
                    truckHp01: 0.8f,
                    heatDelta: 11f,
                    peakHeatDelta: 13f,
                    enemiesDefeated: 1,
                    comboActions: 0));

            Assert.AreEqual(
                RestRewardProfile.StockUp,
                FoodTruckRunModel.ResolveRestRewardProfile(
                    truckHits: 0,
                    truckHp01: 0.8f,
                    heatDelta: 2f,
                    peakHeatDelta: 4f,
                    enemiesDefeated: 4,
                    comboActions: 0));
        }

        [Test]
        public void BuildRestRewardSummary_ReturnsReadablePayoff()
        {
            Assert.AreEqual("Repair", FoodTruckRunModel.BuildRestRewardLabel(RestRewardProfile.Repair));
            Assert.AreEqual("Cooling", FoodTruckRunModel.BuildRestRewardLabel(RestRewardProfile.Cooling));
            Assert.AreEqual("Stock", FoodTruckRunModel.BuildRestRewardLabel(RestRewardProfile.StockUp));
            Assert.AreEqual("Rest Reward: Repair +8 HP.", FoodTruckRunModel.BuildRestRewardSummary(RestRewardProfile.Repair));
            Assert.AreEqual("Rest Reward: Cooling -16 Heat.", FoodTruckRunModel.BuildRestRewardSummary(RestRewardProfile.Cooling));
            Assert.AreEqual("Rest Reward: Stock +6 Supplies, +4 Momentum.", FoodTruckRunModel.BuildRestRewardSummary(RestRewardProfile.StockUp));
        }

        [Test]
        public void BuildRestRewardChipText_CondensesReleaseReward()
        {
            Assert.AreEqual(
                "Release Cooling | -16 Heat",
                FoodTruckPrototypeHud.BuildRestRewardChipText("Cooling", "Rest Reward: Cooling -16 Heat."));
            Assert.AreEqual(
                "Release Stock | +6 Supplies, +4 Momentum",
                FoodTruckPrototypeHud.BuildRestRewardChipText("Stock", "Rest Reward: Stock +6 Supplies, +4 Momentum."));
            Assert.AreEqual(string.Empty, FoodTruckPrototypeHud.BuildRestRewardChipText("Repair", string.Empty));
        }

        [Test]
        public void BuildTruckDamageCauseLabel_ReturnsCauseForCombatFloaters()
        {
            Assert.AreEqual("BITE", FoodTruckRunModel.BuildTruckDamageCauseLabel(TruckDamageCause.Bite));
            Assert.AreEqual("PRESSURE", FoodTruckRunModel.BuildTruckDamageCauseLabel(TruckDamageCause.Pressure));
            Assert.AreEqual("OVERHEAT", FoodTruckRunModel.BuildTruckDamageCauseLabel(TruckDamageCause.Overheat));
            Assert.AreEqual("TRUCK", FoodTruckRunModel.BuildTruckDamageCauseLabel(TruckDamageCause.None));
        }

        [Test]
        public void GetTruckDamageCausePriority_KeepsDirectHitsMostVisible()
        {
            Assert.Greater(
                FoodTruckRunModel.GetTruckDamageCausePriority(TruckDamageCause.Bite),
                FoodTruckRunModel.GetTruckDamageCausePriority(TruckDamageCause.Overheat));
            Assert.Greater(
                FoodTruckRunModel.GetTruckDamageCausePriority(TruckDamageCause.Overheat),
                FoodTruckRunModel.GetTruckDamageCausePriority(TruckDamageCause.Pressure));
        }

        [Test]
        public void BuildTruckDamageFloaterLabel_IncludesCauseAndAmount()
        {
            Assert.AreEqual("BITE>TRK -7", FoodTruckPrototypeHud.BuildTruckDamageFloaterLabel("BITE", 6.2f));
            Assert.AreEqual("TRUCK>TRK -1", FoodTruckPrototypeHud.BuildTruckDamageFloaterLabel("", 0.2f));
        }

        [Test]
        public void BuildEnemyDamageFloaterLabel_UsesSourceTargetResultFlow()
        {
            Assert.AreEqual("HIT>Z -5", FoodTruckPrototypeHud.BuildEnemyDamageFloaterLabel(4.2f, false));
            Assert.AreEqual("HIT>Z KO", FoodTruckPrototypeHud.BuildEnemyDamageFloaterLabel(10f, true));
            Assert.AreEqual("RECIPE>LANE KO", FoodTruckPrototypeHud.BuildCombatHitFlowLabel("RECIPE", "LANE", "KO"));
        }

        [Test]
        public void BuildBoardLocalPlacementFailLabel_UsesShortCellLocalCopy()
        {
            Assert.AreEqual(
                "S2 OCCUPIED",
                FoodTruckPrototypeHud.BuildBoardLocalPlacementFailLabel(PlacementFailReason.Occupied, string.Empty, 1));
            Assert.AreEqual(
                "S4 BOUNDS",
                FoodTruckPrototypeHud.BuildBoardLocalPlacementFailLabel(PlacementFailReason.OutOfBounds, string.Empty, 3));
            Assert.AreEqual(
                "S1 ANCHOR",
                FoodTruckPrototypeHud.BuildBoardLocalPlacementFailLabel(PlacementFailReason.None, "Invalid anchor cell.", 0));
        }

        [Test]
        public void ResolveDrawChoiceIntentLabel_ReportsHoldWhenHeatWouldSpike()
        {
            var pending = new PendingBlockState(
                "Spicy Soup",
                2,
                0,
                7f,
                3.0f,
                new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up },
                19,
                BlockTargetType.HighestHp,
                "L3");

            string intent = FoodTruckPrototypeHud.ResolveDrawChoiceIntentLabel(
                pending,
                "BAL",
                "HIGH",
                "HIGH",
                fitSlots: 2,
                currentHeat: 94f,
                heatWarningThreshold: 70f,
                overheatThreshold: 100f);

            Assert.AreEqual("HOLD", intent);
        }

        [Test]
        public void ResolveDrawChoiceIntentLabel_SeparatesGreedySynergyAndHold()
        {
            var greedy = new PendingBlockState(
                "Steak",
                3,
                0,
                9f,
                2.0f,
                new[] { Vector2Int.zero, Vector2Int.right },
                23,
                BlockTargetType.HighestHp,
                "LineH2");
            var synergy = new PendingBlockState(
                "Combo Rice",
                1,
                0,
                4f,
                2.2f,
                new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up },
                31,
                BlockTargetType.RandomLane,
                "L3");
            var safe = new PendingBlockState(
                "Onion",
                1,
                0,
                2f,
                1.4f,
                new[] { Vector2Int.zero },
                7,
                BlockTargetType.Nearest,
                "Dot");

            Assert.AreEqual(
                "GREEDY",
                FoodTruckPrototypeHud.ResolveDrawChoiceIntentLabel(greedy, "POWER", "HIGH", "HIGH", 1, 20f, 70f, 100f));
            Assert.AreEqual(
                "SYNERGY",
                FoodTruckPrototypeHud.ResolveDrawChoiceIntentLabel(synergy, "BAL", "MID", "MID", 4, 20f, 70f, 100f));
            Assert.AreEqual(
                "SAFE",
                FoodTruckPrototypeHud.ResolveDrawChoiceIntentLabel(safe, "BAL", "LOW", "LOW", 2, 20f, 70f, 100f));
            Assert.AreEqual(
                "HOLD",
                FoodTruckPrototypeHud.ResolveDrawChoiceIntentLabel(synergy, "BAL", "MID", "MID", 0, 20f, 70f, 100f));
        }

        [Test]
        public void Tick_WhenWaveThreeStarts_ReportsEventCadence()
        {
            var model = new FoodTruckRunModel(seed: 163);

            AdvanceToWave(model, 3);

            Assert.AreEqual(3, model.LastWaveCadencePlan.Wave);
            Assert.IsTrue(model.LastWaveCadencePlan.RunEvent);
            Assert.IsFalse(model.LastWaveCadencePlannedSpike);
            Assert.AreEqual(1, model.LastWaveCadenceScheduledBeatCount);
            Assert.AreEqual("Wave 3: Event", model.LastWaveCadenceSummary);
            Assert.AreEqual(WavePressureTheme.Swarm, model.CurrentWavePressureTheme);
            Assert.AreEqual("Swarm", model.CurrentWavePressureThemeLabel);
            Assert.AreEqual("Thin small waves", model.CurrentWavePressureThemeHint);
        }

        [Test]
        public void Tick_WhenWaveFourStarts_ReportsUnlockWeatherPlannedSpike()
        {
            var model = new FoodTruckRunModel(seed: 164);

            AdvanceToWave(model, 4);

            Assert.IsTrue(model.LastWaveCadencePlan.ProgressionUnlock);
            Assert.IsTrue(model.LastWaveCadencePlan.WeatherRotation);
            Assert.IsTrue(model.LastWaveCadencePlannedSpike);
            Assert.AreEqual(2, model.LastWaveCadenceScheduledBeatCount);
            Assert.AreEqual("Wave 4: Unlock, Weather (planned spike)", model.LastWaveCadenceSummary);
            Assert.AreEqual(WavePressureTheme.HeatSurge, model.CurrentWavePressureTheme);
            Assert.AreEqual("Heat Surge", model.CurrentWavePressureThemeLabel);
            Assert.AreEqual("Cool before burst", model.CurrentWavePressureThemeHint);
        }

        [Test]
        public void Tick_WhenWaveFiveStarts_ReportsBossRestPlannedSpike()
        {
            var model = new FoodTruckRunModel(seed: 165);

            AdvanceToWave(model, 5);

            Assert.IsTrue(model.LastWaveCadencePlan.BossPressureSpike);
            Assert.IsTrue(model.LastWaveCadencePlan.RestGranted);
            Assert.IsTrue(model.LastWaveCadencePlannedSpike);
            Assert.AreEqual(2, model.LastWaveCadenceScheduledBeatCount);
            Assert.AreEqual("Wave 5: Boss, Rest (planned spike)", model.LastWaveCadenceSummary);
            Assert.AreEqual(WavePressureTheme.Bruiser, model.CurrentWavePressureTheme);
            Assert.AreEqual("Bruiser", model.CurrentWavePressureThemeLabel);
            Assert.AreEqual("Bring high damage", model.CurrentWavePressureThemeHint);
        }

        [Test]
        public void Tick_WhenWaveSevenStarts_ReportsUnlockCadenceWithoutSpike()
        {
            var model = new FoodTruckRunModel(seed: 167);

            AdvanceToWave(model, 7);

            Assert.IsTrue(model.LastWaveCadencePlan.ProgressionUnlock);
            Assert.IsFalse(model.LastWaveCadencePlannedSpike);
            Assert.AreEqual(1, model.LastWaveCadenceScheduledBeatCount);
            Assert.AreEqual("Wave 7: Unlock", model.LastWaveCadenceSummary);
            Assert.AreEqual(WavePressureTheme.Balanced, model.CurrentWavePressureTheme);
            Assert.AreEqual("Balanced", model.CurrentWavePressureThemeLabel);
            Assert.AreEqual("Hold all lanes", model.CurrentWavePressureThemeHint);
        }

        [Test]
        public void VentHeat_WhenPrerequisitesMet_SucceedsAndConsumesResources()
        {
            var model = new FoodTruckRunModel(seed: 17);
            SetAutoProperty(model, "Heat", 40f);
            int beforeSupplies = model.Supplies;

            bool vented = model.VentHeat();

            Assert.IsTrue(vented);
            Assert.AreEqual(beforeSupplies - model.VentSupplyCostValue, model.Supplies);
            Assert.AreEqual(12f, model.Heat, 0.0001f);
            Assert.Greater(model.VentCooldownRemaining, 0f);
        }

        [Test]
        public void ForceNextWave_WhenEventPending_DoesNotAdvance()
        {
            var model = new FoodTruckRunModel(seed: 18);
            SetPrivateField(
                model,
                "pendingEvent",
                new RunEvent(
                    "Blocked",
                    "Flow lock",
                    new[] { new RunEventOption(RunEventOptionType.ScavengeMarket, "Scavenge", "+18 Supplies") }));
            int beforeWave = model.Wave;

            model.ForceNextWave();

            Assert.AreEqual(beforeWave, model.Wave);
            Assert.IsTrue(model.EventPending);
            Assert.IsFalse(model.IsRestPhase);
        }

        [Test]
        public void ForceNextWave_WhenNotRestPhase_NoOp()
        {
            var model = new FoodTruckRunModel(seed: 181);
            SetPrivateField(model, "waveTimer", 7f);

            model.ForceNextWave();

            Assert.IsFalse(model.IsRestPhase);
            Assert.AreEqual(7f, GetPrivateField<float>(model, "waveTimer"), 0.0001f);
        }

        [Test]
        public void ForceNextWave_WhenDrawChoiceOpen_DoesNotSkipRest()
        {
            var model = new FoodTruckRunModel(seed: 182);
            SetPrivateField(model, "restTimer", 5f);

            var drawChoices = GetPrivateField<List<PendingBlockState>>(model, "drawChoices");
            drawChoices.Add(new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 8f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot"));

            model.ForceNextWave();

            Assert.IsTrue(model.IsRestPhase);
            Assert.AreEqual(5f, GetPrivateField<float>(model, "restTimer"), 0.0001f);
        }

        [Test]
        public void ForceNextWave_WhenRestPhase_SkipsRestImmediately()
        {
            var model = new FoodTruckRunModel(seed: 183);
            SetPrivateField(model, "restTimer", 6f);
            SetPrivateField(model, "waveTimer", 12f);
            SetAutoProperty(model, "Heat", 48f);
            SetAutoProperty(model, "TruckHp", 80f);

            model.ForceNextWave();

            Assert.IsFalse(model.IsRestPhase);
            Assert.AreEqual(0f, GetPrivateField<float>(model, "restTimer"), 0.0001f);
            Assert.AreEqual(0f, GetPrivateField<float>(model, "waveTimer"), 0.0001f);
            Assert.AreEqual(48f, model.Heat, 0.0001f);
            Assert.AreEqual(80f, model.TruckHp, 0.0001f);
        }

        [Test]
        public void ChooseEventOption_WithPendingEvent_ResolvesAndClearsEvent()
        {
            var model = new FoodTruckRunModel(seed: 19);
            SetPrivateField(
                model,
                "pendingEvent",
                new RunEvent(
                    "Supplies",
                    "Pick one",
                    new[] { new RunEventOption(RunEventOptionType.ScavengeMarket, "Scavenge", "+18 Supplies") }));
            int beforeSupplies = model.Supplies;

            bool chosen = model.ChooseEventOption(0);

            Assert.IsTrue(chosen);
            Assert.IsFalse(model.EventPending);
            Assert.AreEqual(beforeSupplies + 18, model.Supplies);
        }

        [Test]
        public void ActivateComboBurst_WhenComboTooLow_Fails()
        {
            var model = new FoodTruckRunModel(seed: 20);

            bool activated = model.ActivateComboBurst();

            Assert.IsFalse(activated);
        }

        [Test]
        public void ActivateComboBurst_WhenReadyAndEnemyExists_SucceedsAndResetsCombo()
        {
            var model = new FoodTruckRunModel(seed: 21);
            int requiredCombo = model.ComboBurstRequiredStreakValue;
            SetPrivateField(model, "comboStreak", requiredCombo);
            SetPrivateField(model, "comboTimerRemaining", 6f);
            var enemies = GetPrivateField<List<LaneEnemyState>>(model, "laneEnemies");
            enemies.Add(new LaneEnemyState(id: 1, laneIndex: 0, isSpecial: false, hp: 1f, speedPerSecond: 0.01f));
            int beforeSupplies = model.Supplies;

            bool activated = model.ActivateComboBurst();

            Assert.IsTrue(activated);
            Assert.AreEqual(0, model.ComboStreak);
            Assert.AreEqual(0, model.LaneEnemies.Count);
            Assert.Greater(model.Supplies, beforeSupplies);
        }

        [Test]
        public void TryPlacePendingAtCell_PartialOverlapSameBlock_AutoMerges()
        {
            var model = new FoodTruckRunModel(seed: 22);
            SetPrivateField(model, "placementSuccessCount", 0);

            var placed = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 0, 1 },
                damage: 10f,
                cooldownSeconds: 1.2f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");
            var pending = new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 10f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, placed);
            SetPrivateField(model, "pendingBlock", pending);

            bool result = model.TryPlacePendingAtCell(anchorCellIndex: 1);

            Assert.IsTrue(result);
            Assert.IsFalse(model.HasPendingBlock);
            Assert.AreEqual(1, model.PlacementSuccessCount);
            Assert.AreEqual(1, model.AutoMergeSuccessCount);
            Assert.AreEqual(0, model.DirectPlacementSuccessCount);

            var placedBlocks = GetPrivateField<Dictionary<int, PlacedBlockState>>(model, "placedBlocks");
            Assert.IsTrue(placedBlocks.TryGetValue(1, out var upgraded));
            Assert.AreEqual(2, upgraded.Grade);
            Assert.AreEqual(1, model.GetCellBlockId(0));
            Assert.AreEqual(1, model.GetCellBlockId(1));
            Assert.AreEqual(-1, model.GetCellBlockId(2));
        }

        [Test]
        public void CanAutoMergePendingAtCell_WhenSingleMatchingOverlap_ReturnsTrue()
        {
            var model = new FoodTruckRunModel(seed: 221);

            var placed = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 1 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var pending = new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 10f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, placed);
            SetPrivateField(model, "pendingBlock", pending);

            bool canAutoMerge = model.CanAutoMergePendingAtCell(anchorCellIndex: 1);

            Assert.IsTrue(canAutoMerge);
            Assert.IsTrue(model.HasPendingBlock);
            var placedBlocks = GetPrivateField<Dictionary<int, PlacedBlockState>>(model, "placedBlocks");
            Assert.IsTrue(placedBlocks.TryGetValue(1, out var target));
            Assert.AreEqual(1, target.Grade);
        }

        [Test]
        public void CanAutoMergePendingAtCell_WhenOverlapWithTwoBlocks_ReturnsFalse()
        {
            var model = new FoodTruckRunModel(seed: 222);

            var first = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 1 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var second = new PlacedBlockState(
                id: 2,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 2 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var pending = new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 10f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, first);
            SeedPlacedBlock(model, second);
            SetPrivateField(model, "pendingBlock", pending);

            bool canAutoMerge = model.CanAutoMergePendingAtCell(anchorCellIndex: 1);

            Assert.IsFalse(canAutoMerge);
            Assert.IsTrue(model.HasPendingBlock);
        }

        [Test]
        public void TryPlacePendingAtCell_DifferentShapeSameIngredientGrade_AutoMerges()
        {
            var model = new FoodTruckRunModel(seed: 24);
            SetPrivateField(model, "placementSuccessCount", 0);

            var placed = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 1 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var pending = new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 10f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, placed);
            SetPrivateField(model, "pendingBlock", pending);

            bool result = model.TryPlacePendingAtCell(anchorCellIndex: 1);

            Assert.IsTrue(result);
            Assert.IsFalse(model.HasPendingBlock);
            Assert.AreEqual(1, model.PlacementSuccessCount);
            Assert.AreEqual(1, model.AutoMergeSuccessCount);
            Assert.AreEqual(0, model.DirectPlacementSuccessCount);

            var placedBlocks = GetPrivateField<Dictionary<int, PlacedBlockState>>(model, "placedBlocks");
            Assert.IsTrue(placedBlocks.TryGetValue(1, out var upgraded));
            Assert.AreEqual(2, upgraded.Grade);
            Assert.AreEqual(1, model.GetCellBlockId(1));
            Assert.AreEqual(-1, model.GetCellBlockId(2));
        }

        [Test]
        public void TryMergeBlocksFromCells_DifferentShapeSameIngredientGrade_ManualMerges()
        {
            var model = new FoodTruckRunModel(seed: 241);
            var first = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 0 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var second = new PlacedBlockState(
                id: 2,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 1, 2 },
                damage: 10f,
                cooldownSeconds: 1.2f,
                colorSeed: 2,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, first);
            SeedPlacedBlock(model, second);

            bool result = model.TryMergeBlocksFromCells(0, 1);

            Assert.IsTrue(result);
            Assert.AreEqual(-1, model.GetCellBlockId(0));
            Assert.AreEqual(2, model.GetCellBlockId(1));
            Assert.AreEqual(2, model.GetCellBlockId(2));
            var placedBlocks = GetPrivateField<Dictionary<int, PlacedBlockState>>(model, "placedBlocks");
            Assert.IsFalse(placedBlocks.ContainsKey(1));
            Assert.IsTrue(placedBlocks.TryGetValue(2, out var upgraded));
            Assert.AreEqual(2, upgraded.Grade);
        }

        [Test]
        public void TryPlacePendingAtCell_AutoMerge_EmitsAutoMergeSuccessTrigger()
        {
            var model = new FoodTruckRunModel(seed: 25);
            PresentationTriggerType capturedType = PresentationTriggerType.EventSelected;
            string capturedPayload = null;
            int triggerCount = 0;
            model.PresentationTriggered += (type, payload) =>
            {
                capturedType = type;
                capturedPayload = payload;
                triggerCount += 1;
            };

            var placed = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 1 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var pending = new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 10f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, placed);
            SetPrivateField(model, "pendingBlock", pending);

            bool result = model.TryPlacePendingAtCell(anchorCellIndex: 1);

            Assert.IsTrue(result);
            Assert.GreaterOrEqual(triggerCount, 1);
            Assert.AreEqual(PresentationTriggerType.AutoMergeSuccess, capturedType);
            StringAssert.Contains("Auto Merge", capturedPayload);
        }

        [Test]
        public void TryPlacePendingAtCell_OverlapWithTwoBlocks_DoesNotAutoMerge()
        {
            var model = new FoodTruckRunModel(seed: 23);
            SetPrivateField(model, "placementBlockedOccupiedCount", 0);

            var first = new PlacedBlockState(
                id: 1,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 1 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var second = new PlacedBlockState(
                id: 2,
                ingredientName: "Onion",
                grade: 1,
                occupiedCellIndices: new[] { 2 },
                damage: 8f,
                cooldownSeconds: 1f,
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "Dot");
            var pending = new PendingBlockState(
                ingredientName: "Onion",
                grade: 1,
                drawCost: 3,
                damage: 10f,
                cooldownSeconds: 1.2f,
                cellOffsets: new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
                colorSeed: 1,
                targetType: BlockTargetType.Nearest,
                shapeKey: "LineH2");

            SeedPlacedBlock(model, first);
            SeedPlacedBlock(model, second);
            SetPrivateField(model, "pendingBlock", pending);

            bool result = model.TryPlacePendingAtCell(anchorCellIndex: 1);

            Assert.IsFalse(result);
            Assert.IsTrue(model.HasPendingBlock);
            Assert.AreEqual(PlacementFailReason.Occupied, model.LastPlacementFailReason);
            Assert.AreEqual(1, model.PlacementBlockedOccupiedCount);
        }

        [Test]
        public void TryPlacePendingAtCell_ThirdMatchingBlock_ActivatesRecipeBingo()
        {
            var model = new FoodTruckRunModel(seed: 24);
            int recipeTriggerCount = 0;
            string lastRecipePayload = null;
            model.PresentationTriggered += (type, payload) =>
            {
                if (type == PresentationTriggerType.RecipeActivated)
                {
                    recipeTriggerCount += 1;
                    lastRecipePayload = payload;
                }
            };

            SetPrivateField(model, "nextBlockId", 3);

            SeedPlacedBlock(
                model,
                new PlacedBlockState(
                    id: 1,
                    ingredientName: "Onion",
                    grade: 1,
                    occupiedCellIndices: new[] { 0 },
                    damage: 8f,
                    cooldownSeconds: 1f,
                    colorSeed: 1,
                    targetType: BlockTargetType.Nearest,
                    shapeKey: "Dot"));
            SeedPlacedBlock(
                model,
                new PlacedBlockState(
                    id: 2,
                    ingredientName: "Onion",
                    grade: 1,
                    occupiedCellIndices: new[] { 1 },
                    damage: 8f,
                    cooldownSeconds: 1f,
                    colorSeed: 2,
                    targetType: BlockTargetType.Nearest,
                    shapeKey: "Dot"));

            SetPrivateField(
                model,
                "pendingBlock",
                new PendingBlockState(
                    ingredientName: "Onion",
                    grade: 1,
                    drawCost: 3,
                    damage: 8f,
                    cooldownSeconds: 1f,
                    cellOffsets: new[] { new Vector2Int(0, 0) },
                    colorSeed: 3,
                    targetType: BlockTargetType.Nearest,
                    shapeKey: "Dot"));

            bool placed = model.TryPlacePendingAtCell(2);

            Assert.IsTrue(placed);
            Assert.IsTrue(HasActiveRecipe(model, "Bingo: Onion Line"));
            Assert.IsTrue(HasActiveRecipe(model, "Bingo: Dot Pattern"));
            Assert.GreaterOrEqual(recipeTriggerCount, 2);
            StringAssert.Contains("Bingo:", lastRecipePayload);
            StringAssert.Contains("from", lastRecipePayload);
            StringAssert.Contains("3x", lastRecipePayload);
            StringAssert.Contains("best G1", lastRecipePayload);
            StringAssert.Contains("3x", model.LastRecipeActivationSummary);
            StringAssert.Contains("best G1", model.LastRecipeActivationCue);
        }

        [Test]
        public void BuildDrawChoiceRecipeProgressLabel_PreviewsNearAndBingoStates()
        {
            var model = new FoodTruckRunModel(seed: 35);
            SeedPlacedBlock(
                model,
                new PlacedBlockState(
                    id: 1,
                    ingredientName: "Onion",
                    grade: 1,
                    occupiedCellIndices: new[] { 0 },
                    damage: 8f,
                    cooldownSeconds: 1f,
                    colorSeed: 1,
                    targetType: BlockTargetType.Nearest,
                    shapeKey: "Dot"));
            SeedPlacedBlock(
                model,
                new PlacedBlockState(
                    id: 2,
                    ingredientName: "Onion",
                    grade: 2,
                    occupiedCellIndices: new[] { 1 },
                    damage: 8f,
                    cooldownSeconds: 1f,
                    colorSeed: 2,
                    targetType: BlockTargetType.Nearest,
                    shapeKey: "Dot"));

            var seed = new PendingBlockState(
                "Rice",
                1,
                3,
                4f,
                1.5f,
                new[] { Vector2Int.zero },
                3,
                BlockTargetType.Nearest,
                "LineH2");
            var near = new PendingBlockState(
                "Rice",
                1,
                3,
                4f,
                1.5f,
                new[] { Vector2Int.zero },
                4,
                BlockTargetType.Nearest,
                "Dot");
            var bingo = new PendingBlockState(
                "Onion",
                1,
                3,
                4f,
                1.5f,
                new[] { Vector2Int.zero },
                5,
                BlockTargetType.Nearest,
                "Dot");

            Assert.AreEqual("Recipe Seed Rice/LineH2", model.BuildDrawChoiceRecipeProgressLabel(seed));
            Assert.AreEqual("Recipe Near Dot 2/3", model.BuildDrawChoiceRecipeProgressLabel(near));
            Assert.AreEqual("Recipe Bingo x2", model.BuildDrawChoiceRecipeProgressLabel(bingo));
        }

        [Test]
        public void TriggerRandomRecipe_LogsActivationCause()
        {
            var model = new FoodTruckRunModel(seed: 72);
            string lastLog = null;
            string lastRecipePayload = null;
            model.CombatLogAppended += message => lastLog = message;
            model.PresentationTriggered += (type, payload) =>
            {
                if (type == PresentationTriggerType.RecipeActivated)
                {
                    lastRecipePayload = payload;
                }
            };

            bool triggered = model.TriggerRandomRecipe();

            Assert.IsTrue(triggered);
            StringAssert.Contains("Recipe online:", lastLog);
            StringAssert.Contains("Random recipe roll", lastLog);
            StringAssert.Contains("Random recipe roll", lastRecipePayload);
            Assert.IsFalse(string.IsNullOrEmpty(model.LastRecipeActivationName));
            StringAssert.Contains("Random recipe roll", model.LastRecipeActivationSummary);
            StringAssert.Contains("Random recipe roll", model.LastRecipeActivationCue);
        }

        [Test]
        public void BuildRecipeEffectChipText_DescribesPassiveAndActiveRoles()
        {
            var passive = new RecipeState("Veggie Stir-fry", RecipeTier.Basic, true, 0.45f, 20f);
            var active = new RecipeState("Seafood Pasta", RecipeTier.Intermediate, false, 0.90f, 12f);

            string passiveText = FoodTruckPrototypeHud.BuildRecipeEffectChipText(passive);
            string activeText = FoodTruckPrototypeHud.BuildRecipeEffectChipText(active);

            StringAssert.Contains("Regen/Cool", passiveText);
            StringAssert.Contains("x0.5", passiveText);
            StringAssert.Contains("Lane Hit", activeText);
            StringAssert.Contains("x0.9", activeText);
        }

        [Test]
        public void BuildRecipeLiveProgressChipText_UsesPayoffOrWarmup()
        {
            var cold = new RecipeState("Veggie Stir-fry", RecipeTier.Basic, true, 0.45f, 20f);
            var hot = new RecipeState("Seafood Pasta", RecipeTier.Intermediate, false, 0.90f, 12f)
            {
                DamageDealt = 18.4f,
                EnemiesDefeated = 1
            };

            string coldText = FoodTruckPrototypeHud.BuildRecipeLiveProgressChipText(cold);
            string hotText = FoodTruckPrototypeHud.BuildRecipeLiveProgressChipText(hot);

            StringAssert.Contains("Warming up", coldText);
            StringAssert.Contains("Dmg 18", hotText);
            StringAssert.Contains("KO 1", hotText);
        }

        [Test]
        public void BuildRecipeImpactSummary_ReportsAccumulatedPayoff()
        {
            var recipe = new RecipeState("Seafood Pasta", RecipeTier.Intermediate, false, 0.90f, 12f)
            {
                DamageDealt = 24.4f,
                EnemiesDefeated = 2,
                HpRestored = 3.1f,
                HeatRelieved = 5.6f
            };

            string summary = FoodTruckRunModel.BuildRecipeImpactSummary(recipe);

            StringAssert.Contains("Dmg 24", summary);
            StringAssert.Contains("KO 2", summary);
            StringAssert.Contains("HP +3", summary);
            StringAssert.Contains("Heat -6", summary);
        }

        [Test]
        public void BuildRecipeImpactSummary_WhenNoPayoff_ReportsNoPayoff()
        {
            var recipe = new RecipeState("Veggie Stir-fry", RecipeTier.Basic, true, 0.45f, 20f);

            string summary = FoodTruckRunModel.BuildRecipeImpactSummary(recipe);

            StringAssert.Contains("No payoff recorded", summary);
        }

        [Test]
        public void BuildWaveOutcomeChipText_TrimsTrailingPeriod()
        {
            string chipText = FoodTruckPrototypeHud.BuildWaveOutcomeChipText("Wave 1: 2 KO, HP -5, Heat +7.");

            Assert.AreEqual("Wave 1: 2 KO, HP -5, Heat +7", chipText);
        }

        [Test]
        public void HudFocusLayout_CombatOnly_ExpandsBattlefieldPanel()
        {
            FoodTruckPrototypeHud.GameplayFocusLayoutMetrics layout =
                FoodTruckPrototypeHud.CalculateGameplayFocusLayout(
                    viewportWidth: 1080f,
                    viewportHeight: 1920f,
                    hasPlacementContext: false,
                    dragFocus: false,
                    minimalCombatStripRequested: false);

            Assert.GreaterOrEqual(1f - layout.TopStart01, 0.58f);
            Assert.GreaterOrEqual(layout.BottomTop01, 0.12f);
            Assert.LessOrEqual(layout.BottomTop01, 0.18f);
            Assert.GreaterOrEqual(layout.CenterViewport01, 0.20f);
        }

        [Test]
        public void HudFocusLayout_CapturedPortraitCombatOnly_KeepsBottomAsCommandStrip()
        {
            FoodTruckPrototypeHud.GameplayFocusLayoutMetrics layout =
                FoodTruckPrototypeHud.CalculateGameplayFocusLayout(
                    viewportWidth: 1170f,
                    viewportHeight: 2532f,
                    hasPlacementContext: false,
                    dragFocus: false,
                    minimalCombatStripRequested: false);

            Assert.GreaterOrEqual(1f - layout.TopStart01, 0.58f);
            Assert.LessOrEqual(layout.BottomTop01, 0.16f);
            Assert.GreaterOrEqual(layout.CenterViewport01, 0.24f);
        }

        [Test]
        public void HudFocusLayout_PlacementContext_KeepsBoardButProtectsCenter()
        {
            FoodTruckPrototypeHud.GameplayFocusLayoutMetrics layout =
                FoodTruckPrototypeHud.CalculateGameplayFocusLayout(
                    viewportWidth: 1080f,
                    viewportHeight: 1920f,
                    hasPlacementContext: true,
                    dragFocus: false,
                    minimalCombatStripRequested: false);

            Assert.GreaterOrEqual(1f - layout.TopStart01, 0.50f);
            Assert.GreaterOrEqual(layout.BottomTop01, 0.34f);
            Assert.LessOrEqual(layout.BottomTop01, 0.40f);
            Assert.GreaterOrEqual(layout.CenterViewport01, 0.04f);
            Assert.LessOrEqual(layout.CenterViewport01, 0.12f);
        }

        [Test]
        public void HudFocusLayout_DragPlacement_PreservesLargeDropViewport()
        {
            FoodTruckPrototypeHud.GameplayFocusLayoutMetrics layout =
                FoodTruckPrototypeHud.CalculateGameplayFocusLayout(
                    viewportWidth: 1080f,
                    viewportHeight: 1920f,
                    hasPlacementContext: true,
                    dragFocus: true,
                    minimalCombatStripRequested: false);

            Assert.GreaterOrEqual(layout.CenterViewport01, 0.70f);
            Assert.LessOrEqual(layout.BottomTop01, 0.22f);
        }

        [Test]
        public void HudActionVisibility_CombatOnlyIdle_ShowsBuildEntryRow()
        {
            FoodTruckPrototypeHud.GameplayActionVisibility visibility =
                FoodTruckPrototypeHud.CalculateGameplayActionVisibility(
                    gameplayFocusHud: true,
                    combatOnlyFocus: true,
                    hasPlacementContext: false,
                    hasDrawChoice: false,
                    eventPending: false,
                    canVentHeat: false,
                    canActivateComboBurst: false,
                    overheated: false);

            Assert.IsTrue(visibility.ShowPrimaryActionRow);
            Assert.IsFalse(visibility.ShowSecondaryActionRow);
            Assert.IsTrue(visibility.ShowsAnyActionRow);
            Assert.IsFalse(visibility.ShowInventoryGrid);
        }

        [Test]
        public void HudActionVisibility_CombatOnlyUrgent_ShowsOnlyReactionRow()
        {
            FoodTruckPrototypeHud.GameplayActionVisibility visibility =
                FoodTruckPrototypeHud.CalculateGameplayActionVisibility(
                    gameplayFocusHud: true,
                    combatOnlyFocus: true,
                    hasPlacementContext: false,
                    hasDrawChoice: false,
                    eventPending: false,
                    canVentHeat: true,
                    canActivateComboBurst: false,
                    overheated: false);

            Assert.IsFalse(visibility.ShowPrimaryActionRow);
            Assert.IsTrue(visibility.ShowSecondaryActionRow);
            Assert.IsTrue(visibility.ShowsAnyActionRow);
            Assert.IsFalse(visibility.ShowInventoryGrid);
        }

        [Test]
        public void HudActionVisibility_PlacementContext_KeepsInventoryGridVisible()
        {
            FoodTruckPrototypeHud.GameplayActionVisibility visibility =
                FoodTruckPrototypeHud.CalculateGameplayActionVisibility(
                    gameplayFocusHud: true,
                    combatOnlyFocus: false,
                    hasPlacementContext: true,
                    hasDrawChoice: false,
                    eventPending: false,
                    canVentHeat: false,
                    canActivateComboBurst: false,
                    overheated: false);

            Assert.IsTrue(visibility.ShowPrimaryActionRow);
            Assert.IsTrue(visibility.ShowInventoryGrid);
        }

        private static bool HasActiveRecipe(FoodTruckRunModel model, string name)
        {
            for (int i = 0; i < model.ActiveRecipes.Count; i++)
            {
                if (model.ActiveRecipes[i].Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SeedPlacedBlock(FoodTruckRunModel model, PlacedBlockState block)
        {
            var placedBlocks = GetPrivateField<Dictionary<int, PlacedBlockState>>(model, "placedBlocks");
            placedBlocks[block.Id] = block;

            int[] blockByCell = GetPrivateField<int[]>(model, "blockByCell");
            for (int i = 0; i < block.OccupiedCellIndices.Length; i++)
            {
                blockByCell[block.OccupiedCellIndices[i]] = block.Id;
            }
        }

        private static void SeedStarterBlock(FoodTruckRunModel model)
        {
            SeedPlacedBlock(
                model,
                new PlacedBlockState(
                    id: 9001,
                    ingredientName: "Onion",
                    grade: 1,
                    occupiedCellIndices: new[] { 0 },
                    damage: 1f,
                    cooldownSeconds: 99f,
                    colorSeed: 1,
                    targetType: BlockTargetType.Nearest,
                    shapeKey: "Dot"));
            SetPrivateField(model, "waveBlockChoiceUsedWave", model.Wave);
        }

        private static void PlacePendingAtFirstValidCell(FoodTruckRunModel model)
        {
            for (int anchor = 0; anchor < FoodTruckRunModel.InventoryCellCount; anchor++)
            {
                if (!model.TryGetPendingFootprintCellsPreview(anchor, true, out _))
                {
                    continue;
                }

                Assert.IsTrue(model.TryPlacePendingAtCell(anchor));
                return;
            }

            Assert.Fail("No valid pending placement anchor was available.");
        }

        private static void AdvanceToWave(FoodTruckRunModel model, int targetWave)
        {
            Assert.Greater(targetWave, 1);
            SetAutoProperty(model, "Wave", targetWave - 1);
            SeedStarterBlock(model);
            SetPrivateField(model, "waveTimer", 19f);
            SetAutoProperty(model, "Threat", 0f);
            SetAutoProperty(model, "MaxTruckHp", 100000f);
            SetAutoProperty(model, "TruckHp", 100000f);

            model.Tick(1f);

            Assert.AreEqual(targetWave, model.Wave);
        }

        private static void SetAutoProperty<TModel, TValue>(TModel instance, string propertyName, TValue value)
        {
            FieldInfo field = typeof(TModel).GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Backing field for property '{propertyName}' was not found.");
            field.SetValue(instance, value);
        }

        private static void SetPrivateField<TModel, TValue>(TModel instance, string fieldName, TValue value)
        {
            FieldInfo field = typeof(TModel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private field '{fieldName}' was not found.");
            return (TValue)field.GetValue(instance);
        }
    }
}
