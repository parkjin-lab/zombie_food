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
        public void Tick_WhenWaveAdvances_RecordsOutcomeSummary()
        {
            var model = new FoodTruckRunModel(seed: 162);
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

            Assert.GreaterOrEqual(1f - layout.TopStart01, 0.32f);
            Assert.GreaterOrEqual(layout.BottomTop01, 0.52f);
            Assert.LessOrEqual(layout.BottomTop01, 0.60f);
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
