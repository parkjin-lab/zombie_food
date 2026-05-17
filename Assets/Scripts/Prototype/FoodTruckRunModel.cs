
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public enum WeatherType
    {
        Clear,
        Night,
        Rain,
        Fog
    }

    public enum RecipeTier
    {
        Basic,
        Intermediate,
        Advanced
    }

    public enum BlockTargetType
    {
        Nearest,
        Farthest,
        HighestHp,
        RandomLane
    }

    public enum PlacementFailReason
    {
        None,
        NoPendingBlock,
        InvalidAnchor,
        OutOfBounds,
        Occupied
    }

    public enum PresentationTriggerType
    {
        EventSelected,
        PlacementSuccess,
        AutoMergeSuccess,
        PlacementBlocked,
        RecipeActivated,
        RecipeExpired,
        ComboBurst,
        OverheatSpike,
        ProgressionUnlock,
        HeatWarningEnter,
        HeatStabilized
    }

    public enum RunEventOptionType
    {
        ScavengeMarket,
        ReinforceTruck,
        CoolKitchen,
        StreetShow,
        HireMechanic,
        RiskyShortcut,
        SalvageFuel,
        SealLeak,
        RecipeRush
    }

    public enum RhythmBeatType
    {
        Read,
        Commit,
        Pressure,
        Payoff,
        Release
    }

    public enum PressureRampPhase
    {
        Build,
        Climb,
        Peak
    }


    public sealed class RunEventOption
    {
        public RunEventOption(RunEventOptionType type, string label, string effectSummary)
        {
            Type = type;
            Label = label;
            EffectSummary = effectSummary;
        }

        public RunEventOptionType Type { get; }
        public string Label { get; }
        public string EffectSummary { get; }
    }

    public sealed class RunEvent
    {
        public RunEvent(string title, string description, RunEventOption[] options)
        {
            Title = title;
            Description = description;
            Options = options;
        }

        public string Title { get; }
        public string Description { get; }
        public RunEventOption[] Options { get; }
    }

    public sealed class WaveCadencePlan
    {
        public WaveCadencePlan(
            int wave,
            bool progressionUnlock,
            bool weatherRotation,
            bool bossPressureSpike,
            bool runEvent,
            bool restGranted,
            int scheduledBeatCount,
            bool plannedSpike,
            string summary)
        {
            Wave = wave;
            ProgressionUnlock = progressionUnlock;
            WeatherRotation = weatherRotation;
            BossPressureSpike = bossPressureSpike;
            RunEvent = runEvent;
            RestGranted = restGranted;
            ScheduledBeatCount = scheduledBeatCount;
            PlannedSpike = plannedSpike;
            Summary = summary;
        }

        public int Wave { get; }
        public bool ProgressionUnlock { get; }
        public bool WeatherRotation { get; }
        public bool BossPressureSpike { get; }
        public bool RunEvent { get; }
        public bool RestGranted { get; }
        public int ScheduledBeatCount { get; }
        public bool PlannedSpike { get; }
        public string Summary { get; }
    }

    public sealed class RecipeState
    {
        public RecipeState(string name, RecipeTier tier, bool isPassive, float potency, float durationSeconds)
        {
            Name = name;
            Tier = tier;
            IsPassive = isPassive;
            Potency = potency;
            RemainingSeconds = durationSeconds;
        }

        public string Name { get; }
        public RecipeTier Tier { get; }
        public bool IsPassive { get; }
        public float Potency { get; set; }
        public float RemainingSeconds { get; set; }
        public float DamageDealt { get; set; }
        public float HpRestored { get; set; }
        public float HeatRelieved { get; set; }
        public int EnemiesDefeated { get; set; }
    }

    public sealed class PendingBlockState
    {
        public PendingBlockState(
            string ingredientName,
            int grade,
            int drawCost,
            float damage,
            float cooldownSeconds,
            Vector2Int[] cellOffsets,
            int colorSeed,
            BlockTargetType targetType,
            string shapeKey)
        {
            IngredientName = ingredientName;
            Grade = grade;
            DrawCost = drawCost;
            Damage = damage;
            CooldownSeconds = cooldownSeconds;
            CellOffsets = cellOffsets;
            ColorSeed = colorSeed;
            TargetType = targetType;
            ShapeKey = shapeKey;
        }

        public string IngredientName { get; }
        public int Grade { get; }
        public int DrawCost { get; }
        public float Damage { get; }
        public float CooldownSeconds { get; }
        public Vector2Int[] CellOffsets { get; }
        public int ColorSeed { get; }
        public BlockTargetType TargetType { get; }
        public string ShapeKey { get; }

        public int CellCount => CellOffsets.Length;
        public string Label => IngredientName + " G" + Grade + " " + TargetCode(TargetType) + " [" + CellCount + "]";

        private static string TargetCode(BlockTargetType type)
        {
            switch (type)
            {
                case BlockTargetType.Farthest:
                    return "FAR";
                case BlockTargetType.HighestHp:
                    return "HP";
                case BlockTargetType.RandomLane:
                    return "RND";
                default:
                    return "NEAR";
            }
        }
    }

    public sealed class PlacedBlockState
    {
        public PlacedBlockState(
            int id,
            string ingredientName,
            int grade,
            int[] occupiedCellIndices,
            float damage,
            float cooldownSeconds,
            int colorSeed,
            BlockTargetType targetType,
            string shapeKey)
        {
            Id = id;
            IngredientName = ingredientName;
            Grade = grade;
            OccupiedCellIndices = occupiedCellIndices;
            Damage = damage;
            CooldownSeconds = cooldownSeconds;
            CooldownRemaining = UnityEngine.Random.Range(0f, cooldownSeconds);
            ColorSeed = colorSeed;
            TargetType = targetType;
            ShapeKey = shapeKey;
        }

        public int Id { get; }
        public string IngredientName { get; }
        public int Grade { get; set; }
        public int[] OccupiedCellIndices { get; }
        public float Damage { get; set; }
        public float CooldownSeconds { get; set; }
        public float CooldownRemaining { get; set; }
        public int ColorSeed { get; }
        public BlockTargetType TargetType { get; }
        public string ShapeKey { get; }
    }

    public sealed class LaneEnemyState
    {
        public LaneEnemyState(int id, int laneIndex, bool isSpecial, float hp, float speedPerSecond)
        {
            Id = id;
            LaneIndex = laneIndex;
            IsSpecial = isSpecial;
            Hp = hp;
            MaxHp = hp;
            SpeedPerSecond = speedPerSecond;
            DistanceToTruck01 = 1f;
        }

        public int Id { get; }
        public int LaneIndex { get; }
        public bool IsSpecial { get; }
        public float Hp { get; set; }
        public float MaxHp { get; }
        public float SpeedPerSecond { get; }
        public float DistanceToTruck01 { get; set; }
    }

    public sealed class FoodTruckRunModel
    {
        public const int InventoryWidth = 3;
        public const int InventoryHeight = 3;
        public const int InventoryCellCount = InventoryWidth * InventoryHeight;

        private enum DrawProfile
        {
            Safe,
            Balanced,
            Power
        }
        private const float WaveDurationSeconds = 20f;
        private const float RestDurationSeconds = 10f;
        private const float ComboWindowSeconds = 8f;
        private const int ComboMaxStreak = 10;
        private const int ComboBurstRequiredStreak = 5;
        private const float ComboBurstBaseDamage = 22f;
        private const float OverheatHeatThreshold = 85f;
        private const float OverheatHeatRange = 15f;
        private const float OverheatAttackBonusMax = 0.18f;
        private const float HeatWarningAttackBonusMax = 0.08f;
        private const float OverheatSelfDamageBase = 0.9f;
        private const float OverheatSelfDamageScale = 1.4f;
        private const float HeatWarningRiskMultiplier = 1.12f;
        private const float OverheatRiskMultiplier = 1.28f;
        private const float HeatWarningLootMultiplier = 1.20f;
        private const float OverheatLootMultiplier = 1.45f;
        private const int VentSupplyCost = 4;
        private const float VentMinHeatThreshold = 30f;
        private const float VentMomentumCost = 3f;
        private const float VentHeatRecovery = 28f;
        private const float HeatWarningThreshold = 70f;
        private const float VentCooldownSeconds = 7f;
        private const int MaxSimulatedSecondsPerTick = 8;

        private readonly System.Random random;
        private readonly int[] blockByCell = new int[InventoryCellCount];
        private readonly Dictionary<int, PlacedBlockState> placedBlocks = new Dictionary<int, PlacedBlockState>();
        private readonly List<RecipeState> activeRecipes = new List<RecipeState>();
        private readonly HashSet<string> activeRecipeBingoKeys = new HashSet<string>();
        private readonly List<LaneEnemyState> laneEnemies = new List<LaneEnemyState>();
        private readonly float[] lanePressures = new float[3];

        private readonly ShapeTemplate[] shapeTemplates =
        {
            new ShapeTemplate("Dot", new[] { new Vector2Int(0, 0) }),
            new ShapeTemplate("LineH2", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
            new ShapeTemplate("LineV2", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }),
            new ShapeTemplate("L3", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),
            new ShapeTemplate("T4", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) }),
            new ShapeTemplate("Square4", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) })
        };

        private readonly IngredientTemplate[] ingredientTemplates =
        {
            new IngredientTemplate("Onion", 1.1f, 0.95f),
            new IngredientTemplate("Beef", 1.35f, 1.20f),
            new IngredientTemplate("Shrimp", 1.0f, 0.85f),
            new IngredientTemplate("Chili", 1.2f, 1.0f),
            new IngredientTemplate("Rice", 0.9f, 0.8f),
            new IngredientTemplate("Seaweed", 1.05f, 0.9f),
            new IngredientTemplate("Garlic", 1.12f, 0.92f),
            new IngredientTemplate("Pork", 1.22f, 1.15f)
        };

        private readonly RecipeTemplate[] recipePool =
        {
            new RecipeTemplate("Veggie Stir-fry", RecipeTier.Basic, true, 0.45f, 20f),
            new RecipeTemplate("Steak", RecipeTier.Basic, false, 0.60f, 10f),
            new RecipeTemplate("Kimbap", RecipeTier.Basic, true, 0.50f, 24f),
            new RecipeTemplate("Seafood Pasta", RecipeTier.Intermediate, false, 0.90f, 12f),
            new RecipeTemplate("Beef Rice Bowl", RecipeTier.Intermediate, true, 0.85f, 22f),
            new RecipeTemplate("Sushi Set", RecipeTier.Intermediate, false, 1.00f, 9f),
            new RecipeTemplate("Korean Set Meal", RecipeTier.Advanced, true, 1.30f, 18f),
            new RecipeTemplate("Ultimate Curry", RecipeTier.Advanced, false, 1.45f, 8f)
        };

        private float simulationAccumulator;
        private float waveTimer;
        private float restTimer;
        private int nextBlockId;
        private int nextEnemyId;

        private PendingBlockState pendingBlock;
        private RunEvent pendingEvent;
        private readonly List<PendingBlockState> drawChoices = new List<PendingBlockState>();
        private readonly List<DrawProfile> drawChoiceProfiles = new List<DrawProfile>();
        private int pendingRotationQuarterTurns;
        private int comboStreak;
        private float comboTimerRemaining;
        private float ventCooldownRemaining;
        private int lastHeatBand;
        private PlacementFailReason lastPlacementFailReason;
        private readonly int[] drawChoicePickCounts = new int[3];
        private int placementAttemptCount;
        private int placementSuccessCount;
        private int autoMergeSuccessCount;
        private int placementBlockedOutOfBoundsCount;
        private int placementBlockedOccupiedCount;
        private int placementBlockedInvalidAnchorCount;
        private int placementBlockedNoPendingCount;
        private int waveStartSupplies;
        private float waveStartTruckHp;
        private float waveStartHeat;
        private float waveDamageDealt;
        private int waveEnemiesDefeated;
        private int waveTruckHits;
        private int waveComboActions;
        private int waveBestComboStreak;
        private float wavePeakHeat;
        private string lastWaveOutcomeSummary = string.Empty;
        private string lastWaveOutcomeCue = string.Empty;
        private string lastWaveOutcomeNextHint = string.Empty;
        private WaveCadencePlan lastWaveCadencePlan;
        private string lastRecipeActivationName = string.Empty;
        private string lastRecipeActivationSummary = string.Empty;
        private string lastRecipeActivationCue = string.Empty;
        private string lastRecipeResultSummary = string.Empty;
        private string lastRecipeResultCue = string.Empty;
        private string drawAssistTag = "BAL";
        public event Action StateChanged;
        public event Action<string> CombatLogAppended;
        public event Action<PresentationTriggerType, string> PresentationTriggered;

        public FoodTruckRunModel(int seed = 0)
        {
            random = seed == 0 ? new System.Random(Environment.TickCount) : new System.Random(seed);
            ResetRun();
        }

        public int Wave { get; private set; }
        public float TruckHp { get; private set; }
        public float MaxTruckHp { get; private set; }
        public int Supplies { get; private set; }
        public int Fuel { get; private set; }
        public float Threat { get; private set; }
        public float Heat { get; private set; }
        public float Momentum { get; private set; }
        public WeatherType Weather { get; private set; }
        public float BattleSpeed { get; private set; } = 1f;

        public int InventoryCellsFilled { get; private set; }
        public bool IsRunOver => TruckHp <= 0f;
        public bool IsRestPhase => restTimer > 0f;
        public bool EventPending => pendingEvent != null;
        public RunEvent PendingEvent => pendingEvent;
        public bool HasDrawChoice => drawChoices.Count > 0;
        public IReadOnlyList<PendingBlockState> DrawChoices => drawChoices;
        public bool HasPendingBlock => pendingBlock != null;
        public PendingBlockState PendingBlock => pendingBlock;
        public int PendingRotationDegrees => pendingRotationQuarterTurns * 90;
        public int ComboStreak => comboStreak;
        public float ComboTimerRemaining => comboTimerRemaining;
        public float ComboProgress01 => comboStreak > 0 ? comboTimerRemaining / ComboWindowSeconds : 0f;
        public float ComboMultiplier => 1f + Mathf.Min(0.45f, comboStreak * 0.05f);
        public bool IsHeatWarning => Heat >= HeatWarningThreshold && Heat < OverheatHeatThreshold;
        public bool IsOverheated => Heat >= OverheatHeatThreshold;
        public float OverheatSeverity01 => IsOverheated ? Mathf.Clamp01((Heat - OverheatHeatThreshold) / OverheatHeatRange) : 0f;
        public float HeatWarningThresholdValue => HeatWarningThreshold;
        public float OverheatThresholdValue => OverheatHeatThreshold;
        public int VentSupplyCostValue => VentSupplyCost;
        public float VentMinHeatThresholdValue => VentMinHeatThreshold;
        public float VentCooldownRemaining => ventCooldownRemaining;
        public float HeatAttackMultiplier => GetHeatAttackMultiplier();
        public float HeatRiskMultiplier => GetHeatRiskMultiplier();
        public float HeatLootMultiplier => GetHeatLootMultiplier();
        public int ComboBurstRequiredStreakValue => ComboBurstRequiredStreak;
        public bool CanActivateComboBurst => comboStreak >= ComboBurstRequiredStreak && !HasDrawChoice && !EventPending;
        public bool CanVentHeat => !IsRunOver && !EventPending && !HasDrawChoice && ventCooldownRemaining <= 0f && Supplies >= VentSupplyCost && Heat >= VentMinHeatThreshold;
        public PlacementFailReason LastPlacementFailReason => lastPlacementFailReason;
        public string LastPlacementFailReasonText => GetPlacementFailReasonText(lastPlacementFailReason);
        public int PlacementAttemptCount => placementAttemptCount;
        public int PlacementSuccessCount => placementSuccessCount;
        public int AutoMergeSuccessCount => autoMergeSuccessCount;
        public int DirectPlacementSuccessCount => Mathf.Max(0, placementSuccessCount - autoMergeSuccessCount);
        public int PlacementBlockedOutOfBoundsCount => placementBlockedOutOfBoundsCount;
        public int PlacementBlockedOccupiedCount => placementBlockedOccupiedCount;
        public int PlacementBlockedInvalidAnchorCount => placementBlockedInvalidAnchorCount;
        public int PlacementBlockedNoPendingCount => placementBlockedNoPendingCount;
        public int DrawChoicePickTotal => drawChoicePickCounts[0] + drawChoicePickCounts[1] + drawChoicePickCounts[2];
        public float PlacementSuccessRate => placementAttemptCount > 0 ? (float)placementSuccessCount / placementAttemptCount : 0f;
        public string LastWaveOutcomeSummary => lastWaveOutcomeSummary;
        public string LastWaveOutcomeCue => lastWaveOutcomeCue;
        public string LastWaveOutcomeNextHint => lastWaveOutcomeNextHint;
        public WaveCadencePlan LastWaveCadencePlan => lastWaveCadencePlan;
        public string LastWaveCadenceSummary => lastWaveCadencePlan != null ? lastWaveCadencePlan.Summary : string.Empty;
        public bool LastWaveCadencePlannedSpike => lastWaveCadencePlan != null && lastWaveCadencePlan.PlannedSpike;
        public int LastWaveCadenceScheduledBeatCount => lastWaveCadencePlan != null ? lastWaveCadencePlan.ScheduledBeatCount : 0;
        public RhythmBeatType CurrentRhythmBeat => ResolveRhythmBeat(
            IsRestPhase,
            EventPending,
            HasDrawChoice,
            HasPendingBlock,
            !string.IsNullOrEmpty(lastWaveOutcomeNextHint),
            GetWaveProgress01());
        public string CurrentRhythmBeatLabel => BuildRhythmBeatLabel(CurrentRhythmBeat);
        public PressureRampPhase CurrentPressureRampPhase => ResolvePressureRampPhase(GetWaveProgress01(), IsRestPhase, EventPending, HasDrawChoice);
        public string CurrentPressureRampLabel => BuildPressureRampLabel(CurrentPressureRampPhase);
        public float CurrentPressureRampIntensity01 => BuildPressureRampIntensity01(GetWaveProgress01(), IsRestPhase, EventPending, HasDrawChoice);
        public string LastRecipeActivationName => lastRecipeActivationName;
        public string LastRecipeActivationSummary => lastRecipeActivationSummary;
        public string LastRecipeActivationCue => lastRecipeActivationCue;
        public string LastRecipeResultSummary => lastRecipeResultSummary;
        public string LastRecipeResultCue => lastRecipeResultCue;
        public string DrawAssistTag => drawAssistTag;
        public IReadOnlyList<RecipeState> ActiveRecipes => activeRecipes;
        public IReadOnlyList<LaneEnemyState> LaneEnemies => laneEnemies;

        public void ResetRun()
        {
            Wave = 1;
            MaxTruckHp = 120f;
            TruckHp = MaxTruckHp;
            Supplies = 16;
            Fuel = 0;
            Threat = 8f;
            Heat = 0f;
            Momentum = 0f;
            Weather = WeatherType.Clear;
            simulationAccumulator = 0f;
            waveTimer = 0f;
            restTimer = 0f;
            nextBlockId = 1;
            nextEnemyId = 1;
            pendingBlock = null;
            pendingEvent = null;
            drawChoices.Clear();
            drawChoiceProfiles.Clear();
            pendingRotationQuarterTurns = 0;
            lastPlacementFailReason = PlacementFailReason.None;
            comboStreak = 0;
            comboTimerRemaining = 0f;
            ventCooldownRemaining = 0f;
            lastHeatBand = 0;
            drawAssistTag = "BAL";
            placementAttemptCount = 0;
            placementSuccessCount = 0;
            autoMergeSuccessCount = 0;
            placementBlockedOutOfBoundsCount = 0;
            placementBlockedOccupiedCount = 0;
            placementBlockedInvalidAnchorCount = 0;
            placementBlockedNoPendingCount = 0;
            for (int i = 0; i < drawChoicePickCounts.Length; i++)
            {
                drawChoicePickCounts[i] = 0;
            }
            activeRecipes.Clear();
            activeRecipeBingoKeys.Clear();
            laneEnemies.Clear();
            placedBlocks.Clear();
            lastWaveOutcomeSummary = string.Empty;
            lastWaveOutcomeCue = string.Empty;
            lastWaveOutcomeNextHint = string.Empty;
            lastWaveCadencePlan = BuildWaveCadencePlan(Wave);
            lastRecipeActivationName = string.Empty;
            lastRecipeActivationSummary = string.Empty;
            lastRecipeActivationCue = string.Empty;
            lastRecipeResultSummary = string.Empty;
            lastRecipeResultCue = string.Empty;
            for (int i = 0; i < blockByCell.Length; i++)
            {
                blockByCell[i] = -1;
            }

            RecalculateInventoryFill();
            UpdateLanePressures();
            CaptureWaveOutcomeBaseline();
            AppendLog("Run started. Draw, rotate, and drag blocks into the 3x3 inventory.");
            AppendLog("Progression stage 1 active: NEAREST targeting + basic shapes.");
            RaiseChanged();
        }

        public void Tick(float deltaTime)
        {
            if (IsRunOver)
            {
                return;
            }

            simulationAccumulator += Mathf.Max(0f, deltaTime) * BattleSpeed;
            int simulatedSteps = 0;
            while (simulationAccumulator >= 1f && simulatedSteps < MaxSimulatedSecondsPerTick)
            {
                SimulateSecond();
                simulationAccumulator -= 1f;
                simulatedSteps += 1;

                if (IsRunOver)
                {
                    break;
                }
            }

            if (simulationAccumulator >= 1f)
            {
                // Drop excessive backlog to avoid a long catch-up spiral after stalls or focus loss.
                simulationAccumulator = Mathf.Repeat(simulationAccumulator, 1f);
            }
        }

        public float GetWaveProgress01()
        {
            if (IsRestPhase)
            {
                return 0f;
            }

            return Mathf.Clamp01(waveTimer / WaveDurationSeconds);
        }

        public int GetDrawCost()
        {
            return 4 + Mathf.FloorToInt(Wave * 0.6f);
        }

        public int GetCellBlockId(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= InventoryCellCount)
            {
                return -1;
            }

            return blockByCell[cellIndex];
        }

        public string GetBlockShortLabel(int blockId)
        {
            if (!placedBlocks.TryGetValue(blockId, out PlacedBlockState block))
            {
                return "--";
            }

            string ing = block.IngredientName.Length >= 2
                ? block.IngredientName.Substring(0, 2).ToUpperInvariant()
                : block.IngredientName.ToUpperInvariant();
            return ing + block.Grade + TargetTypeToShortCode(block.TargetType);
        }


        public int GetLaneEnemyCount(int laneIndex)
        {
            int count = 0;
            for (int i = 0; i < laneEnemies.Count; i++)
            {
                if (laneEnemies[i].LaneIndex == laneIndex)
                {
                    count++;
                }
            }

            return count;
        }

        public float GetLanePressure(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= lanePressures.Length)
            {
                return 0f;
            }

            return lanePressures[laneIndex];
        }

        public void SetBattleSpeed(float newSpeed)
        {
            BattleSpeed = Mathf.Clamp(newSpeed, 1f, 2f);
            RaiseChanged();
        }

        public bool DrawIngredient()
        {
            if (IsRunOver || EventPending)
            {
                return false;
            }

            if (HasDrawChoice)
            {
                AppendLog("Choose one of the 3 draw options first.");
                return false;
            }

            if (pendingBlock != null)
            {
                AppendLog("A block is already waiting. Place or sell it first.");
                return false;
            }

            int drawCost = GetDrawCost();
            if (Supplies < drawCost)
            {
                AppendLog("Not enough supplies to draw.");
                return false;
            }

            Supplies -= drawCost;
            drawChoices.Clear();
            drawChoiceProfiles.Clear();

            float friction01 = EstimatePlacementFriction01();
            drawAssistTag = GetDrawAssistTag(friction01);

            for (int i = 0; i < 3; i++)
            {
                DrawProfile profile = GetDrawProfileForSlot(i, friction01);
                PendingBlockState candidate = BuildUniquePendingBlockForProfile(drawCost, profile, friction01, 6);
                drawChoices.Add(candidate);
                drawChoiceProfiles.Add(profile);
            }

            EnsureRecoveryChoice(drawCost, friction01);
            AppendDrawAssistHint(friction01);
            AppendLog("Draw ready: choose 1 of 3 blocks.");
            RaiseChanged();
            return true;
        }

        private void AppendDrawAssistHint(float friction01)
        {
            if (friction01 >= 0.68f)
            {
                AppendLog("Assist bias SAFE: small shapes are prioritized for recovery.");
            }
            else if (friction01 <= 0.22f && placementAttemptCount >= 6)
            {
                AppendLog("Assist bias POWER: high-impact shapes are more likely now.");
            }
        }

        public bool ChooseDrawOption(int choiceIndex)
        {
            if (!HasDrawChoice)
            {
                return false;
            }

            if (choiceIndex < 0 || choiceIndex >= drawChoices.Count)
            {
                return false;
            }

            pendingBlock = drawChoices[choiceIndex];
            string selectedAssistTag = GetDrawChoiceAssistTag(choiceIndex);
            if (!string.IsNullOrEmpty(selectedAssistTag))
            {
                drawAssistTag = selectedAssistTag;
            }
            if (choiceIndex >= 0 && choiceIndex < drawChoicePickCounts.Length)
            {
                drawChoicePickCounts[choiceIndex] += 1;
            }

            drawChoices.Clear();
            drawChoiceProfiles.Clear();
            pendingRotationQuarterTurns = 0;
            lastPlacementFailReason = PlacementFailReason.None;
            comboStreak = 0;
            comboTimerRemaining = 0f;
            ventCooldownRemaining = 0f;
            lastHeatBand = 0;
            AddHeatProgressive(3f + pendingBlock.CellCount * 1.15f);
            Momentum += 1.5f;

            AppendLog("Selected " + pendingBlock.Label + ". Drag it into the inventory grid.");
            if (!CanPendingFitAnywhere())
            {
                AppendLog("No valid slot right now. Sell or free space.");
            }

            RaiseChanged();
            return true;
        }

        public bool RotatePendingClockwise()
        {
            return RotatePendingBy(1);
        }

        public bool RotatePendingCounterClockwise()
        {
            return RotatePendingBy(-1);
        }

        private bool RotatePendingBy(int quarterTurns)
        {
            if (pendingBlock == null || HasDrawChoice)
            {
                return false;
            }

            pendingRotationQuarterTurns = (pendingRotationQuarterTurns + quarterTurns) % 4;
            if (pendingRotationQuarterTurns < 0)
            {
                pendingRotationQuarterTurns += 4;
            }

            AppendLog("Rotated pending block to " + PendingRotationDegrees + " degrees.");
            RaiseChanged();
            return true;
        }
        public bool SellIngredient()
        {
            if (IsRunOver || EventPending)
            {
                return false;
            }

            if (HasDrawChoice)
            {
                AppendLog("Pick one of the 3 draw options first.");
                return false;
            }

            if (pendingBlock != null)
            {
                int refund = Mathf.Max(1, pendingBlock.DrawCost / 2);
                Supplies += refund;
                Heat = Mathf.Max(0f, Heat - 2.5f);
                AppendLog("Sold pending block for " + refund + " supplies.");
                pendingBlock = null;
                pendingRotationQuarterTurns = 0;
                lastPlacementFailReason = PlacementFailReason.None;
                RaiseChanged();
                return true;
            }

            if (placedBlocks.Count == 0)
            {
                return false;
            }

            int newestId = int.MinValue;
            foreach (KeyValuePair<int, PlacedBlockState> entry in placedBlocks)
            {
                if (entry.Key > newestId)
                {
                    newestId = entry.Key;
                }
            }

            RemovePlacedBlock(newestId);
            Supplies += Mathf.Max(1, GetDrawCost() / 2);
            Heat = Mathf.Max(0f, Heat - 4f);
            Momentum = Mathf.Max(0f, Momentum - 1f);
            AppendLog("Sold one placed block for partial refund.");
            RaiseChanged();
            return true;
        }

        public bool CanPlacePendingAtCell(int anchorCellIndex)
        {
            return TryCollectPendingCells(anchorCellIndex, true, out _, out _);
        }

        public bool CanAutoMergePendingAtCell(int anchorCellIndex)
        {
            return TryResolveAutoMergeTarget(anchorCellIndex, out _, out _);
        }

        public bool TryGetPendingFootprintCells(int anchorCellIndex, out int[] cells)
        {
            return TryCollectPendingCells(anchorCellIndex, false, out cells, out _);
        }

        public bool TryGetPendingFootprintCellsPreview(int anchorCellIndex, bool requireEmpty, out int[] cells)
        {
            return TryGetPendingFootprintCellsPreview(
                anchorCellIndex,
                requireEmpty,
                out cells,
                out _,
                out _);
        }

        public bool TryGetPendingFootprintCellsPreview(
            int anchorCellIndex,
            bool requireEmpty,
            out int[] cells,
            out PlacementFailReason failReason,
            out string reason)
        {
            PlacementFailReason savedReason = lastPlacementFailReason;
            bool ok = TryCollectPendingCells(anchorCellIndex, requireEmpty, out cells, out reason);
            failReason = ok ? PlacementFailReason.None : lastPlacementFailReason;
            lastPlacementFailReason = savedReason;
            return ok;
        }

        public bool TryPlacePendingAtCell(int anchorCellIndex)
        {
            placementAttemptCount += 1;

            if (pendingBlock == null)
            {
                lastPlacementFailReason = PlacementFailReason.NoPendingBlock;
                RegisterPlacementFailure(lastPlacementFailReason);
                AppendLog("No pending block to place.");
                EmitPresentationTrigger(PresentationTriggerType.PlacementBlocked, "No pending block.");
                return false;
            }

            if (!TryCollectPendingCells(anchorCellIndex, true, out int[] cells, out string reason))
            {
                if (lastPlacementFailReason == PlacementFailReason.Occupied &&
                    TryAutoMergePendingAtCell(anchorCellIndex, out string autoMergePayload))
                {
                    placementSuccessCount += 1;
                    autoMergeSuccessCount += 1;
                    lastPlacementFailReason = PlacementFailReason.None;
                    EmitPresentationTrigger(PresentationTriggerType.AutoMergeSuccess, autoMergePayload);
                    bool autoMergeRecipeBingoActivated = EvaluateRecipeBingo();
                    if (!autoMergeRecipeBingoActivated && random.NextDouble() < 0.45d)
                    {
                        TriggerRandomRecipe("Auto-merge bonus roll");
                    }
                    else
                    {
                        RaiseChanged();
                    }

                    return true;
                }

                RegisterPlacementFailure(lastPlacementFailReason);
                if (!string.IsNullOrEmpty(reason))
                {
                    AppendLog(reason);
                }

                EmitPresentationTrigger(PresentationTriggerType.PlacementBlocked, reason);
                return false;
            }

            string placedLabel = pendingBlock.Label;
            int blockId = nextBlockId++;
            PlacedBlockState placed = new PlacedBlockState(
                blockId,
                pendingBlock.IngredientName,
                pendingBlock.Grade,
                cells,
                pendingBlock.Damage,
                pendingBlock.CooldownSeconds,
                pendingBlock.ColorSeed,
                pendingBlock.TargetType,
                pendingBlock.ShapeKey);

            placedBlocks.Add(blockId, placed);
            for (int i = 0; i < cells.Length; i++)
            {
                blockByCell[cells[i]] = blockId;
            }

            RecalculateInventoryFill();
            AppendLog("Placed " + pendingBlock.Label + ".");
            pendingBlock = null;
            pendingRotationQuarterTurns = 0;
            lastPlacementFailReason = PlacementFailReason.None;
            placementSuccessCount += 1;
            Momentum += 4f;

            RegisterComboAction("Place");
            EmitPresentationTrigger(PresentationTriggerType.PlacementSuccess, placedLabel);
            bool recipeBingoActivated = EvaluateRecipeBingo();

            if (!recipeBingoActivated && random.NextDouble() < 0.30d)
            {
                TriggerRandomRecipe("Placement bonus roll");
            }
            else
            {
                RaiseChanged();
            }

            return true;
        }

        private bool TryResolveAutoMergeTarget(
            int anchorCellIndex,
            out PlacedBlockState target,
            out string failureMessage)
        {
            target = null;
            failureMessage = string.Empty;
            if (pendingBlock == null)
            {
                return false;
            }

            if (!TryGetPendingFootprintCellsPreview(anchorCellIndex, false, out int[] candidateCells))
            {
                return false;
            }

            if (candidateCells == null || candidateCells.Length == 0)
            {
                return false;
            }

            int targetBlockId = -1;
            int overlapCount = 0;
            for (int i = 0; i < candidateCells.Length; i++)
            {
                int cell = candidateCells[i];
                int blockId = blockByCell[cell];
                if (blockId < 0)
                {
                    continue;
                }

                overlapCount += 1;
                if (targetBlockId < 0)
                {
                    targetBlockId = blockId;
                    continue;
                }

                if (targetBlockId != blockId)
                {
                    failureMessage = "Auto-merge can overlap only one existing block.";
                    return false;
                }
            }

            if (overlapCount <= 0 || targetBlockId < 0)
            {
                return false;
            }

            if (!placedBlocks.TryGetValue(targetBlockId, out target))
            {
                return false;
            }

            if (target.IngredientName != pendingBlock.IngredientName ||
                target.Grade != pendingBlock.Grade)
            {
                failureMessage = "Auto-merge requires same ingredient and grade.";
                return false;
            }

            if (target.Grade >= 5)
            {
                failureMessage = "That block is already max grade.";
                return false;
            }

            return true;
        }

        private bool TryAutoMergePendingAtCell(int anchorCellIndex, out string payload)
        {
            payload = string.Empty;
            if (!TryResolveAutoMergeTarget(anchorCellIndex, out PlacedBlockState target, out string failureMessage))
            {
                if (!string.IsNullOrEmpty(failureMessage))
                {
                    AppendLog(failureMessage);
                }

                return false;
            }

            target.Grade += 1;
            target.Damage *= 1.45f;
            target.CooldownSeconds = Mathf.Max(0.65f, target.CooldownSeconds * 0.90f);
            target.CooldownRemaining = Mathf.Min(target.CooldownRemaining, target.CooldownSeconds);
            Momentum += 7f;
            Heat = Mathf.Max(0f, Heat - 3f);
            RegisterComboAction("Merge");

            string ingredientName = target.IngredientName;
            int mergedGrade = target.Grade;
            pendingBlock = null;
            pendingRotationQuarterTurns = 0;

            AppendLog("Auto-merged " + ingredientName + " to G" + mergedGrade + ".");
            payload = ingredientName + " G" + mergedGrade + " (Auto Merge)";

            return true;
        }

        public bool TryMergeBlocksFromCells(int firstCellIndex, int secondCellIndex)
        {
            int firstId = GetCellBlockId(firstCellIndex);
            int secondId = GetCellBlockId(secondCellIndex);

            if (firstId < 0 || secondId < 0)
            {
                AppendLog("Select two occupied cells to merge.");
                return false;
            }

            if (firstId == secondId)
            {
                AppendLog("Select two different blocks.");
                return false;
            }

            if (!placedBlocks.TryGetValue(firstId, out PlacedBlockState first) ||
                !placedBlocks.TryGetValue(secondId, out PlacedBlockState second))
            {
                return false;
            }

            if (first.IngredientName != second.IngredientName ||
                first.Grade != second.Grade)
            {
                AppendLog("Merge requires same ingredient and grade.");
                return false;
            }

            if (second.Grade >= 5)
            {
                AppendLog("That block is already max grade.");
                return false;
            }

            RemovePlacedBlock(firstId);
            second.Grade += 1;
            second.Damage *= 1.45f;
            second.CooldownSeconds = Mathf.Max(0.65f, second.CooldownSeconds * 0.90f);
            second.CooldownRemaining = Mathf.Min(second.CooldownRemaining, second.CooldownSeconds);
            Momentum += 7f;
            Heat = Mathf.Max(0f, Heat - 3f);
            RegisterComboAction("Merge");
            bool recipeBingoActivated = EvaluateRecipeBingo();

            AppendLog("Merged " + second.IngredientName + " to G" + second.Grade + ".");
            if (!recipeBingoActivated && random.NextDouble() < 0.45d)
            {
                TriggerRandomRecipe("Merge bonus roll");
            }
            else
            {
                RaiseChanged();
            }

            return true;
        }

        public void ForceNextWave()
        {
            if (IsRunOver || EventPending || HasDrawChoice || !IsRestPhase)
            {
                return;
            }

            restTimer = 0f;
            waveTimer = 0f;
            AppendLog("Rest skipped. Back to combat.");
            UpdateLanePressures();
            RaiseChanged();
        }

        public bool TriggerRandomRecipe()
        {
            return TriggerRandomRecipe("Random recipe roll");
        }

        private bool TriggerRandomRecipe(string cause)
        {
            if (IsRunOver)
            {
                return false;
            }

            RecipeTemplate template = recipePool[random.Next(0, recipePool.Length)];
            ActivateRecipe(template, cause);
            RaiseChanged();
            return true;
        }

        public bool ActivateComboBurst()
        {
            if (IsRunOver || EventPending || HasDrawChoice)
            {
                return false;
            }

            if (comboStreak < ComboBurstRequiredStreak)
            {
                AppendLog("Need combo x" + ComboBurstRequiredStreak + "+ to activate Burst.");
                return false;
            }

            if (laneEnemies.Count == 0)
            {
                AppendLog("No enemies in lanes for Burst.");
                return false;
            }

            float burstDamage = (ComboBurstBaseDamage + comboStreak * 4.2f + Wave * 1.1f + Momentum * 0.15f) * GetHeatAttackMultiplier();
            int laneHits = 0;
            for (int lane = 0; lane < 3; lane++)
            {
                int targetIndex = FindNearestEnemyInLane(lane);
                if (targetIndex < 0)
                {
                    continue;
                }

                LaneEnemyState target = laneEnemies[targetIndex];
                float effectiveDealt = Mathf.Min(burstDamage, Mathf.Max(0f, target.Hp));
                target.Hp -= burstDamage;
                waveDamageDealt += effectiveDealt;
                laneHits += 1;
            }

            int kills = 0;
            for (int i = laneEnemies.Count - 1; i >= 0; i--)
            {
                LaneEnemyState enemy = laneEnemies[i];
                if (enemy.Hp > 0f)
                {
                    continue;
                }

                kills += 1;
                waveEnemiesDefeated += 1;
                int baseGain = enemy.IsSpecial ? 4 : 2;
                Supplies += GetHeatAdjustedSupplyGain(baseGain);
                laneEnemies.RemoveAt(i);
            }

            comboStreak = 0;
            comboTimerRemaining = 0f;
            AddHeatProgressive(6f + laneHits * 1.2f);
            Momentum += 2f + kills;
            Threat = Mathf.Max(4f, Threat - kills * 0.35f);

            AppendLog("Burst fired! hits " + laneHits + ", kills " + kills + ", dmg " + burstDamage.ToString("0") + ".");
            EmitPresentationTrigger(
                PresentationTriggerType.ComboBurst,
                "hits " + laneHits + ", kills " + kills + ", dmg " + burstDamage.ToString("0"));
            RaiseChanged();
            return true;
        }

        public bool VentHeat()
        {
            if (!CanVentHeat)
            {
                if (!IsRunOver && !EventPending && !HasDrawChoice && Heat < VentMinHeatThreshold)
                {
                    AppendLog("Heat is too low to vent.");
                }
                else if (!IsRunOver && !EventPending && !HasDrawChoice && Supplies < VentSupplyCost)
                {
                    AppendLog("Need " + VentSupplyCost + " supplies to vent.");
                }
                else if (!IsRunOver && !EventPending && !HasDrawChoice && ventCooldownRemaining > 0f)
                {
                    AppendLog("Vent cooldown " + ventCooldownRemaining.ToString("0") + "s.");
                }

                return false;
            }

            Supplies -= VentSupplyCost;
            Momentum = Mathf.Max(0f, Momentum - VentMomentumCost);
            Heat = Mathf.Max(0f, Heat - VentHeatRecovery);
            ventCooldownRemaining = VentCooldownSeconds;

            if (Heat < OverheatHeatThreshold)
            {
                AppendLog("Vented heat. Systems stabilize.");
            }
            else
            {
                AppendLog("Vented heat, but the truck is still running hot.");
            }

            RaiseChanged();
            return true;
        }
        public bool ChooseEventOption(int optionIndex)
        {
            if (pendingEvent == null || optionIndex < 0 || optionIndex >= pendingEvent.Options.Length)
            {
                return false;
            }

            RunEventOption option = pendingEvent.Options[optionIndex];
            pendingEvent = null;
            ResolveEventOption(option.Type);
            AppendLog("Event choice: " + option.Label);
            EmitPresentationTrigger(PresentationTriggerType.EventSelected, option.Label);
            RaiseChanged();
            return true;
        }

        private void RegisterComboAction(string source)
        {
            int previous = comboStreak;
            comboStreak = Mathf.Clamp(comboStreak + 1, 1, ComboMaxStreak);
            comboTimerRemaining = ComboWindowSeconds;
            waveComboActions += 1;
            waveBestComboStreak = Mathf.Max(waveBestComboStreak, comboStreak);

            if (comboStreak >= 2 && comboStreak != previous)
            {
                AppendLog(source + " combo x" + comboStreak + " (DMG x" + ComboMultiplier.ToString("0.00") + ")");
            }

            if (comboStreak == ComboMaxStreak && previous != ComboMaxStreak)
            {
                AppendLog("Max combo reached!");
            }
        }

        private void TickCombo()
        {
            if (comboStreak <= 0)
            {
                return;
            }

            if (EventPending || HasDrawChoice)
            {
                return;
            }

            comboTimerRemaining -= 1f;
            if (comboTimerRemaining > 0f)
            {
                return;
            }

            comboStreak = 0;
            comboTimerRemaining = 0f;
            AppendLog("Combo ended.");
        }
        private void SimulateSecond()
        {
            TickRecipes();
            TickCombo();
            if (ventCooldownRemaining > 0f)
            {
                ventCooldownRemaining = Mathf.Max(0f, ventCooldownRemaining - 1f);
            }
            ApplyOverheatPressureDamage();

            if (TruckHp <= 0f)
            {
                TruckHp = 0f;
                AppendLog("Truck destroyed. Run ended.");
                RaiseChanged();
                return;
            }

            if (IsRestPhase)
            {
                restTimer -= 1f;
                Heat = Mathf.Max(0f, Heat - 4f);
                TruckHp = Mathf.Min(MaxTruckHp, TruckHp + 1.2f);
                if (restTimer <= 0f)
                {
                    AppendLog("Rest ended. Back to combat.");
                }

                UpdateLanePressures();
                RaiseChanged();
                return;
            }

            bool advancedWave = false;
            if (!EventPending)
            {
                SimulateCombat();
                waveTimer += 1f;
                if (waveTimer >= WaveDurationSeconds)
                {
                    AdvanceWave();
                    advancedWave = true;
                }
            }

            Momentum = Mathf.Max(0f, Momentum - 1.05f);
            Heat = Mathf.Clamp(Heat - 0.7f, 0f, 100f);
            UpdateLanePressures();
            if (advancedWave)
            {
                CaptureWaveOutcomeBaseline();
            }

            RaiseChanged();
        }

        private void SimulateCombat()
        {
            SpawnLaneEnemies();
            ResolveBlockAttacks();
            ResolveRecipeEffects();
            AdvanceLaneEnemies();
            ApplyAmbientPressureDamage();

            if (TruckHp <= 0f)
            {
                TruckHp = 0f;
                AppendLog("Truck destroyed. Run ended.");
            }
        }

        private void SpawnLaneEnemies()
        {
            float spawnIntensity = 0.25f + Threat * 0.06f + Wave * 0.045f;
            int spawnCount = Mathf.FloorToInt(spawnIntensity);
            if (random.NextDouble() < spawnIntensity - spawnCount)
            {
                spawnCount += 1;
            }

            spawnCount = Mathf.Clamp(spawnCount, 0, 6);
            for (int i = 0; i < spawnCount; i++)
            {
                int lane = random.Next(0, 3);
                bool special = random.NextDouble() < Mathf.Clamp01((Wave - 4) * 0.05f + Threat * 0.004f);

                float hp = 6f + Wave * 1.2f + Threat * 0.25f;
                if (special)
                {
                    hp *= 1.3f;
                }

                float speed = 0.045f + Wave * 0.0018f + GetWeatherSpeedBonus();
                if (special)
                {
                    speed += 0.012f;
                }

                laneEnemies.Add(new LaneEnemyState(nextEnemyId++, lane, special, hp, speed));
            }
        }

        private void ResolveBlockAttacks()
        {
            if (placedBlocks.Count == 0 || laneEnemies.Count == 0)
            {
                return;
            }

            float passivePower = GetPassivePower();
            float damageMult = (1f + passivePower * 0.12f + Momentum * 0.01f) * ComboMultiplier * GetHeatAttackMultiplier();

            List<PlacedBlockState> blockSnapshot = new List<PlacedBlockState>(placedBlocks.Values);
            for (int i = 0; i < blockSnapshot.Count; i++)
            {
                PlacedBlockState block = blockSnapshot[i];
                block.CooldownRemaining -= 1f;
                if (block.CooldownRemaining > 0f)
                {
                    continue;
                }

                int targetIndex = FindTargetEnemyIndex(block);
                if (targetIndex < 0)
                {
                    block.CooldownRemaining = 0f;
                    continue;
                }

                LaneEnemyState target = laneEnemies[targetIndex];
                float dealt = block.Damage * damageMult;
                float effectiveDealt = Mathf.Min(dealt, Mathf.Max(0f, target.Hp));
                target.Hp -= dealt;
                waveDamageDealt += effectiveDealt;
                block.CooldownRemaining += block.CooldownSeconds;

                if (target.Hp <= 0f)
                {
                    waveEnemiesDefeated += 1;
                    int gain = target.IsSpecial ? 4 : 2;
                    int comboSupplyBonus = Mathf.Clamp(comboStreak / 3, 0, 3);
                    Supplies += GetHeatAdjustedSupplyGain(gain + comboSupplyBonus);
                    Momentum += target.IsSpecial ? 2.5f : 1f;
                    Threat = Mathf.Max(4f, Threat - 0.12f);
                    laneEnemies.RemoveAt(targetIndex);
                }
            }
        }

        private void ResolveRecipeEffects()
        {
            if (activeRecipes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < activeRecipes.Count; i++)
            {
                RecipeState recipe = activeRecipes[i];
                if (recipe.IsPassive)
                {
                    float hpBefore = TruckHp;
                    float heatBefore = Heat;
                    TruckHp = Mathf.Min(MaxTruckHp, TruckHp + recipe.Potency * 0.3f);
                    Heat = Mathf.Max(0f, Heat - recipe.Potency * 0.25f);
                    recipe.HpRestored += Mathf.Max(0f, TruckHp - hpBefore);
                    recipe.HeatRelieved += Mathf.Max(0f, heatBefore - Heat);
                    continue;
                }

                if (laneEnemies.Count == 0)
                {
                    continue;
                }

                int burstTargets = Mathf.Min(2, laneEnemies.Count);
                for (int t = 0; t < burstTargets; t++)
                {
                    int idx = random.Next(0, laneEnemies.Count);
                    LaneEnemyState enemy = laneEnemies[idx];
                    float dealt = recipe.Potency * (1.6f + Wave * 0.08f) * ComboMultiplier * GetHeatAttackMultiplier();
                    float effectiveDealt = Mathf.Min(dealt, Mathf.Max(0f, enemy.Hp));
                    enemy.Hp -= dealt;
                    waveDamageDealt += effectiveDealt;
                    recipe.DamageDealt += effectiveDealt;
                    if (enemy.Hp <= 0f)
                    {
                        waveEnemiesDefeated += 1;
                        recipe.EnemiesDefeated += 1;
                        int baseGain = enemy.IsSpecial ? 3 : 1;
                        Supplies += GetHeatAdjustedSupplyGain(baseGain);
                        laneEnemies.RemoveAt(idx);
                    }
                }
            }
        }

        private void AdvanceLaneEnemies()
        {
            for (int i = laneEnemies.Count - 1; i >= 0; i--)
            {
                LaneEnemyState enemy = laneEnemies[i];
                enemy.DistanceToTruck01 -= enemy.SpeedPerSecond;
                if (enemy.DistanceToTruck01 > 0f)
                {
                    continue;
                }

                float hitDamage = ((enemy.IsSpecial ? 9f : 5f) + Wave * 0.35f) * GetHeatRiskMultiplier();
                TruckHp -= hitDamage;
                waveTruckHits += 1;
                AddHeatProgressive(enemy.IsSpecial ? 3f : 1.6f);
                Threat += (enemy.IsSpecial ? 0.8f : 0.3f) * GetHeatRiskMultiplier();
                laneEnemies.RemoveAt(i);
            }
        }

        private void ApplyAmbientPressureDamage()
        {
            float chip = (Mathf.Max(0f, (Threat - 18f) * 0.05f) + Mathf.Max(0f, (Heat - 70f) * 0.07f)) * GetHeatRiskMultiplier();
            if (chip > 0f)
            {
                TruckHp -= chip;
            }
        }
        private void ApplyOverheatPressureDamage()
        {
            if (!IsOverheated || IsRestPhase || EventPending || HasDrawChoice)
            {
                return;
            }

            float severity = OverheatSeverity01;
            float chip = (OverheatSelfDamageBase + severity * OverheatSelfDamageScale) * GetHeatRiskMultiplier();
            TruckHp -= chip;
        }

        private void AdvanceWave()
        {
            CaptureWaveOutcomeSummary();
            waveTimer = 0f;
            Wave += 1;
            lastWaveCadencePlan = BuildWaveCadencePlan(Wave);
            Threat += 2.2f + Wave * 0.33f;
            Supplies += 4 + Mathf.FloorToInt(Wave * 0.32f);
            AppendLog(lastWaveOutcomeSummary);
            AppendLog("Wave " + Wave + " started.");

            if (lastWaveCadencePlan.ProgressionUnlock && Wave == 4)
            {
                AppendLog("Progression unlock: advanced targeting (FAR/HP) + T shape enabled.");
                EmitPresentationTrigger(
                    PresentationTriggerType.ProgressionUnlock,
                    "Wave 4: FAR/HP targeting + T shape unlocked.");
            }
            else if (lastWaveCadencePlan.ProgressionUnlock && Wave == 7)
            {
                AppendLog("Progression unlock: RANDOM targeting + Square shape + harsher heat risk.");
                EmitPresentationTrigger(
                    PresentationTriggerType.ProgressionUnlock,
                    "Wave 7: RANDOM targeting + Square shape unlocked.");
            }

            if (lastWaveCadencePlan.WeatherRotation)
            {
                RotateWeather();
            }

            if (lastWaveCadencePlan.BossPressureSpike)
            {
                Threat += 3.2f;
                restTimer = RestDurationSeconds;
                TruckHp = Mathf.Min(MaxTruckHp, TruckHp + 12f);
                AppendLog("Boss pressure spike. Rest phase granted.");
            }

            if (lastWaveCadencePlan.RunEvent)
            {
                pendingEvent = BuildEvent();
                AppendLog("Run event triggered. Choose one option.");
            }
        }

        private static WaveCadencePlan BuildWaveCadencePlan(int wave)
        {
            bool progressionUnlock = wave == 4 || wave == 7;
            bool weatherRotation = wave % 4 == 0;
            bool bossPressureSpike = wave % 5 == 0;
            bool runEvent = wave % 3 == 0;
            bool restGranted = bossPressureSpike;

            var beats = new List<string>();
            if (progressionUnlock)
            {
                beats.Add("Unlock");
            }

            if (weatherRotation)
            {
                beats.Add("Weather");
            }

            if (bossPressureSpike)
            {
                beats.Add("Boss");
                beats.Add("Rest");
            }

            if (runEvent)
            {
                beats.Add("Event");
            }

            int scheduledBeatCount = beats.Count;
            bool plannedSpike = bossPressureSpike || scheduledBeatCount >= 2;
            string beatSummary = scheduledBeatCount > 0 ? string.Join(", ", beats.ToArray()) : "Steady combat";
            string summary = "Wave " + wave + ": " + beatSummary;
            if (plannedSpike)
            {
                summary += " (planned spike)";
            }

            return new WaveCadencePlan(
                wave,
                progressionUnlock,
                weatherRotation,
                bossPressureSpike,
                runEvent,
                restGranted,
                scheduledBeatCount,
                plannedSpike,
                summary);
        }

        private void CaptureWaveOutcomeBaseline()
        {
            waveStartSupplies = Supplies;
            waveStartTruckHp = TruckHp;
            waveStartHeat = Heat;
            waveDamageDealt = 0f;
            waveEnemiesDefeated = 0;
            waveTruckHits = 0;
            waveComboActions = 0;
            waveBestComboStreak = comboStreak;
            wavePeakHeat = Heat;
        }

        private void CaptureWaveOutcomeSummary()
        {
            int completedWave = Wave;
            int suppliesDelta = Supplies - waveStartSupplies;
            float hpDelta = TruckHp - waveStartTruckHp;
            float heatDelta = Heat - waveStartHeat;
            float peakHeatDelta = wavePeakHeat - waveStartHeat;

            string primaryImpact = waveEnemiesDefeated > 0
                ? waveEnemiesDefeated + " KO"
                : (waveDamageDealt >= 0.5f ? Mathf.RoundToInt(waveDamageDealt) + " dmg" : "held");
            string hpPart = "HP " + FormatSignedRounded(hpDelta);
            string heatPart = "Heat " + FormatSignedRounded(heatDelta);
            string comboPart = waveBestComboStreak >= 2
                ? ", Combo x" + waveBestComboStreak
                : (waveComboActions > 0 ? ", Actions " + waveComboActions : string.Empty);
            string leakPart = waveTruckHits > 0 ? ", Leak x" + waveTruckHits : string.Empty;

            lastWaveOutcomeCue = "Wave " + completedWave + ": " + primaryImpact + ", " + hpPart + ", " + heatPart + ".";
            lastWaveOutcomeSummary =
                lastWaveOutcomeCue.TrimEnd('.') +
                ", Sup " + FormatSignedRounded(suppliesDelta) +
                ", Dmg " + Mathf.RoundToInt(waveDamageDealt) +
                (peakHeatDelta > heatDelta + 0.5f ? ", PeakHeat " + FormatSignedRounded(peakHeatDelta) : string.Empty) +
                comboPart +
                leakPart +
                ".";
            lastWaveOutcomeNextHint = BuildWaveOutcomeNextHint(
                waveTruckHits,
                hpDelta,
                heatDelta,
                peakHeatDelta,
                waveEnemiesDefeated,
                waveDamageDealt,
                waveBestComboStreak);
        }

        public static string BuildWaveOutcomeNextHint(
            int truckHits,
            float hpDelta,
            float heatDelta,
            float peakHeatDelta,
            int enemiesDefeated,
            float damageDealt,
            int bestComboStreak)
        {
            if (truckHits >= 2 || hpDelta <= -18f)
            {
                return "Stabilize lanes";
            }

            if (peakHeatDelta >= 12f || heatDelta >= 12f)
            {
                return "Pick COOL/SAFE";
            }

            if (enemiesDefeated >= 3 || damageDealt >= 45f)
            {
                return "Push damage";
            }

            if (bestComboStreak >= 3)
            {
                return "Keep combo window";
            }

            if (hpDelta >= 8f)
            {
                return "Spend recovery";
            }

            return "Keep balanced draw";
        }

        public static RhythmBeatType ResolveRhythmBeat(
            bool isRestPhase,
            bool eventPending,
            bool hasDrawChoice,
            bool hasPendingBlock,
            bool hasPayoffHint,
            float waveProgress01)
        {
            if (isRestPhase)
            {
                return RhythmBeatType.Release;
            }

            if (eventPending || hasDrawChoice)
            {
                return RhythmBeatType.Read;
            }

            if (hasPendingBlock)
            {
                return RhythmBeatType.Commit;
            }

            if (hasPayoffHint && waveProgress01 <= 0.12f)
            {
                return RhythmBeatType.Payoff;
            }

            return RhythmBeatType.Pressure;
        }

        public static string BuildRhythmBeatLabel(RhythmBeatType beat)
        {
            switch (beat)
            {
                case RhythmBeatType.Read:
                    return "Read";
                case RhythmBeatType.Commit:
                    return "Commit";
                case RhythmBeatType.Payoff:
                    return "Payoff";
                case RhythmBeatType.Release:
                    return "Release";
                default:
                    return "Pressure";
            }
        }

        public static PressureRampPhase ResolvePressureRampPhase(float waveProgress01, bool isRestPhase, bool eventPending, bool hasDrawChoice)
        {
            if (isRestPhase || eventPending || hasDrawChoice)
            {
                return PressureRampPhase.Build;
            }

            float clampedProgress = Mathf.Clamp01(waveProgress01);
            if (clampedProgress < 0.35f)
            {
                return PressureRampPhase.Build;
            }

            return clampedProgress < 0.72f ? PressureRampPhase.Climb : PressureRampPhase.Peak;
        }

        public static string BuildPressureRampLabel(PressureRampPhase phase)
        {
            switch (phase)
            {
                case PressureRampPhase.Climb:
                    return "Climb";
                case PressureRampPhase.Peak:
                    return "Peak";
                default:
                    return "Build";
            }
        }

        public static float BuildPressureRampIntensity01(float waveProgress01, bool isRestPhase, bool eventPending, bool hasDrawChoice)
        {
            if (isRestPhase || eventPending || hasDrawChoice)
            {
                return 0f;
            }

            float clampedProgress = Mathf.Clamp01(waveProgress01);
            return clampedProgress * clampedProgress * (3f - 2f * clampedProgress);
        }

        private static string FormatSignedRounded(float value)
        {
            int rounded = Mathf.RoundToInt(value);
            return rounded >= 0 ? "+" + rounded : rounded.ToString();
        }

        private void TickRecipes()
        {
            for (int i = activeRecipes.Count - 1; i >= 0; i--)
            {
                RecipeState recipe = activeRecipes[i];
                recipe.RemainingSeconds -= 1f;
                if (recipe.RemainingSeconds <= 0f)
                {
                    string impactSummary = BuildRecipeImpactSummary(recipe);
                    lastRecipeResultSummary = recipe.Name + ": " + impactSummary;
                    lastRecipeResultCue = recipe.Name + " " + impactSummary;
                    AppendLog(recipe.Name + " expired: " + impactSummary + ".");
                    EmitPresentationTrigger(PresentationTriggerType.RecipeExpired, lastRecipeResultSummary);
                    activeRecipes.RemoveAt(i);
                }
            }
        }

        public static string BuildRecipeImpactSummary(RecipeState recipe)
        {
            if (recipe == null)
            {
                return "No payoff recorded";
            }

            List<string> parts = new List<string>();
            if (recipe.DamageDealt >= 0.5f)
            {
                parts.Add("Dmg " + Mathf.RoundToInt(recipe.DamageDealt));
            }

            if (recipe.EnemiesDefeated > 0)
            {
                parts.Add("KO " + recipe.EnemiesDefeated);
            }

            if (recipe.HpRestored >= 0.5f)
            {
                parts.Add("HP +" + Mathf.RoundToInt(recipe.HpRestored));
            }

            if (recipe.HeatRelieved >= 0.5f)
            {
                parts.Add("Heat -" + Mathf.RoundToInt(recipe.HeatRelieved));
            }

            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "No payoff recorded";
        }

        private float GetPassivePower()
        {
            float total = 0f;
            for (int i = 0; i < activeRecipes.Count; i++)
            {
                if (activeRecipes[i].IsPassive)
                {
                    total += activeRecipes[i].Potency;
                }
            }

            return total;
        }

        private int GetHeatBand()
        {
            if (Heat >= OverheatHeatThreshold)
            {
                return 2;
            }

            if (Heat >= HeatWarningThreshold)
            {
                return 1;
            }

            return 0;
        }

        private void UpdateHeatBandFeedback()
        {
            int band = GetHeatBand();
            if (band == lastHeatBand)
            {
                return;
            }

            if (band == 2)
            {
                AppendLog("OVERHEAT zone: attack/loot boosted, incoming damage amplified.");
                EmitPresentationTrigger(
                    PresentationTriggerType.OverheatSpike,
                    "Heat " + Heat.ToString("0.0") + ", risk x" + GetHeatRiskMultiplier().ToString("0.00"));
            }
            else if (band == 1 && lastHeatBand == 0)
            {
                AppendLog("Heat warning zone entered. Higher rewards, higher risk.");
                EmitPresentationTrigger(
                    PresentationTriggerType.HeatWarningEnter,
                    "Heat " + Heat.ToString("0.0") + ", rewards up and risk up.");
            }
            else if (band == 1 && lastHeatBand == 2)
            {
                AppendLog("Heat dropped to warning zone.");
                EmitPresentationTrigger(
                    PresentationTriggerType.HeatWarningEnter,
                    "Heat dropped from overheat to warning.");
            }
            else if (band == 0)
            {
                AppendLog("Heat stabilized.");
                EmitPresentationTrigger(
                    PresentationTriggerType.HeatStabilized,
                    "Heat " + Heat.ToString("0.0") + ", risk normalized.");
            }

            lastHeatBand = band;
        }

        private int GetHeatAdjustedSupplyGain(int baseGain)
        {
            if (baseGain <= 0)
            {
                return 0;
            }

            float bonus = baseGain * (GetHeatLootMultiplier() - 1f);
            if (bonus <= 0f)
            {
                return baseGain;
            }

            int adjusted = baseGain + Mathf.Max(1, Mathf.FloorToInt(bonus));
            if (IsOverheated)
            {
                adjusted += 1;
            }

            return adjusted;
        }

        private float GetHeatAttackMultiplier()
        {
            if (IsOverheated)
            {
                return 1f + 0.06f + OverheatSeverity01 * (OverheatAttackBonusMax - 0.06f);
            }

            if (IsHeatWarning)
            {
                float warning01 = Mathf.InverseLerp(HeatWarningThreshold, OverheatHeatThreshold, Heat);
                return 1f + Mathf.Lerp(0.03f, HeatWarningAttackBonusMax, warning01);
            }

            return 1f;
        }

        private float GetHeatRiskMultiplier()
        {
            float progressionScale = GetHeatRiskProgressionScale();
            if (IsOverheated)
            {
                return Mathf.Lerp(OverheatRiskMultiplier - 0.10f, OverheatRiskMultiplier, OverheatSeverity01) * progressionScale;
            }

            return IsHeatWarning ? HeatWarningRiskMultiplier * progressionScale : 1f;
        }

        private float GetHeatRiskProgressionScale()
        {
            int stage = GetProgressionStage();
            if (stage <= 1)
            {
                return 0.92f;
            }

            if (stage >= 3)
            {
                return 1.10f;
            }

            return 1f;
        }

        private float GetHeatGainProgressionScale()
        {
            int stage = GetProgressionStage();
            if (stage <= 1)
            {
                return 0.84f;
            }

            if (stage >= 3)
            {
                return 1.12f;
            }

            return 1f;
        }

        private void AddHeatProgressive(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Heat += amount * GetHeatGainProgressionScale();
            wavePeakHeat = Mathf.Max(wavePeakHeat, Heat);
        }

        private float GetHeatLootMultiplier()
        {
            if (IsOverheated)
            {
                return Mathf.Lerp(OverheatLootMultiplier - 0.15f, OverheatLootMultiplier, OverheatSeverity01);
            }

            return IsHeatWarning ? HeatWarningLootMultiplier : 1f;
        }

        private int FindTargetEnemyIndex(PlacedBlockState block)
        {
            switch (block.TargetType)
            {
                case BlockTargetType.Farthest:
                    return FindFarthestEnemyIndex();
                case BlockTargetType.HighestHp:
                    return FindHighestHpEnemyIndex();
                case BlockTargetType.RandomLane:
                    int lane = random.Next(0, 3);
                    int laneTarget = FindRandomEnemyInLane(lane);
                    return laneTarget >= 0 ? laneTarget : FindNearestEnemyIndex();
                default:
                    return FindNearestEnemyIndex();
            }
        }

        private int FindNearestEnemyIndex()
        {
            int target = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < laneEnemies.Count; i++)
            {
                float distance = laneEnemies[i].DistanceToTruck01;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = i;
                }
            }

            return target;
        }

        private int FindNearestEnemyInLane(int laneIndex)
        {
            int target = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < laneEnemies.Count; i++)
            {
                LaneEnemyState enemy = laneEnemies[i];
                if (enemy.LaneIndex != laneIndex)
                {
                    continue;
                }

                if (enemy.DistanceToTruck01 < bestDistance)
                {
                    bestDistance = enemy.DistanceToTruck01;
                    target = i;
                }
            }

            return target;
        }
        private int FindFarthestEnemyIndex()
        {
            int target = -1;
            float bestDistance = float.MinValue;
            for (int i = 0; i < laneEnemies.Count; i++)
            {
                float distance = laneEnemies[i].DistanceToTruck01;
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    target = i;
                }
            }

            return target;
        }

        private int FindHighestHpEnemyIndex()
        {
            int target = -1;
            float bestHp = float.MinValue;
            for (int i = 0; i < laneEnemies.Count; i++)
            {
                float hp = laneEnemies[i].Hp;
                if (hp > bestHp)
                {
                    bestHp = hp;
                    target = i;
                }
            }

            return target;
        }

        private int FindRandomEnemyInLane(int laneIndex)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < laneEnemies.Count; i++)
            {
                if (laneEnemies[i].LaneIndex == laneIndex)
                {
                    indices.Add(i);
                }
            }

            if (indices.Count == 0)
            {
                return -1;
            }

            int selected = random.Next(0, indices.Count);
            return indices[selected];
        }

        private void UpdateLanePressures()
        {
            for (int lane = 0; lane < lanePressures.Length; lane++)
            {
                lanePressures[lane] = Threat * 0.32f + Wave * 0.75f + GetWeatherLanePressureBonus();
            }

            for (int i = 0; i < laneEnemies.Count; i++)
            {
                LaneEnemyState enemy = laneEnemies[i];
                lanePressures[enemy.LaneIndex] += enemy.Hp * 0.16f + (1f - enemy.DistanceToTruck01) * 7f;
            }
        }

        private float GetWeatherSpeedBonus()
        {
            switch (Weather)
            {
                case WeatherType.Night:
                    return 0.012f;
                case WeatherType.Fog:
                    return 0.006f;
                default:
                    return 0f;
            }
        }

        private float GetWeatherLanePressureBonus()
        {
            switch (Weather)
            {
                case WeatherType.Night:
                    return 3.5f;
                case WeatherType.Rain:
                    return 2.2f;
                case WeatherType.Fog:
                    return 1.4f;
                default:
                    return 0f;
            }
        }

        private bool EvaluateRecipeBingo()
        {
            if (placedBlocks.Count < 3)
            {
                activeRecipeBingoKeys.Clear();
                return false;
            }

            Dictionary<string, int> ingredientCounts = new Dictionary<string, int>();
            Dictionary<string, int> ingredientMaxGrades = new Dictionary<string, int>();
            Dictionary<string, int> shapeCounts = new Dictionary<string, int>();
            Dictionary<string, int> shapeMaxGrades = new Dictionary<string, int>();

            foreach (KeyValuePair<int, PlacedBlockState> entry in placedBlocks)
            {
                PlacedBlockState block = entry.Value;
                if (block == null)
                {
                    continue;
                }

                PushBingoCount(ingredientCounts, ingredientMaxGrades, block.IngredientName, block.Grade);
                PushBingoCount(shapeCounts, shapeMaxGrades, block.ShapeKey, block.Grade);
            }

            HashSet<string> currentKeys = new HashSet<string>();
            bool anyActivated = false;
            anyActivated |= TryActivateBingoGroup("ING", ingredientCounts, ingredientMaxGrades, true, currentKeys);
            anyActivated |= TryActivateBingoGroup("SHAPE", shapeCounts, shapeMaxGrades, false, currentKeys);

            List<string> staleKeys = new List<string>();
            foreach (string key in activeRecipeBingoKeys)
            {
                if (!currentKeys.Contains(key))
                {
                    staleKeys.Add(key);
                }
            }

            for (int i = 0; i < staleKeys.Count; i++)
            {
                activeRecipeBingoKeys.Remove(staleKeys[i]);
            }

            return anyActivated;
        }

        private static void PushBingoCount(
            Dictionary<string, int> counts,
            Dictionary<string, int> maxGrades,
            string key,
            int grade)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
                maxGrades[key] = grade;
            }

            counts[key] += 1;
            maxGrades[key] = Mathf.Max(maxGrades[key], grade);
        }

        private bool TryActivateBingoGroup(
            string prefix,
            Dictionary<string, int> counts,
            Dictionary<string, int> maxGrades,
            bool passive,
            HashSet<string> currentKeys)
        {
            bool anyActivated = false;
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value < 3)
                {
                    continue;
                }

                string key = prefix + ":" + pair.Key;
                currentKeys.Add(key);
                if (activeRecipeBingoKeys.Contains(key))
                {
                    continue;
                }

                activeRecipeBingoKeys.Add(key);
                int maxGrade = maxGrades.TryGetValue(pair.Key, out int grade) ? grade : 1;
                ActivateRecipe(
                    BuildBingoRecipeTemplate(prefix, pair.Key, pair.Value, maxGrade, passive),
                    BuildRecipeActivationCause(prefix, pair.Key, pair.Value, maxGrade));
                anyActivated = true;
            }

            return anyActivated;
        }

        private static string BuildRecipeActivationCause(string prefix, string value, int count, int maxGrade)
        {
            string source = prefix == "ING" ? value + " ingredient" : value + " shape";
            return count + "x " + source + ", best G" + maxGrade;
        }

        private RecipeTemplate BuildBingoRecipeTemplate(string prefix, string value, int count, int maxGrade, bool passive)
        {
            int extraCount = Mathf.Max(0, count - 3);
            int gradeBonus = Mathf.Max(0, maxGrade - 1);
            RecipeTier tier = count >= 5 || maxGrade >= 4
                ? RecipeTier.Advanced
                : (count >= 4 || maxGrade >= 3 ? RecipeTier.Intermediate : RecipeTier.Basic);
            float potency = (passive ? 0.62f : 0.76f) + extraCount * 0.12f + gradeBonus * 0.08f;
            float duration = passive ? 22f + extraCount * 2f : 12f + extraCount * 1.5f;
            string label = prefix == "ING"
                ? "Bingo: " + value + " Line"
                : "Bingo: " + value + " Pattern";
            return new RecipeTemplate(label, tier, passive, potency, duration);
        }

        private void ActivateRecipe(RecipeTemplate template)
        {
            ActivateRecipe(template, string.Empty);
        }

        private void ActivateRecipe(RecipeTemplate template, string cause)
        {
            RecipeState existing = activeRecipes.Find(r => r.Name == template.Name);
            if (existing != null)
            {
                existing.RemainingSeconds = Mathf.Max(existing.RemainingSeconds, template.DurationSeconds);
                existing.Potency = Mathf.Min(existing.Potency + 0.15f, template.Potency * 1.6f);
            }
            else
            {
                activeRecipes.Add(new RecipeState(
                    template.Name,
                    template.Tier,
                    template.IsPassive,
                    template.Potency,
                    template.DurationSeconds));
            }

            if (template.IsPassive)
            {
                Momentum += 6f + template.Potency * 5f;
                if (template.Tier == RecipeTier.Advanced)
                {
                    MaxTruckHp += 3f;
                    TruckHp = Mathf.Min(MaxTruckHp, TruckHp + 3f);
                }
            }
            else
            {
                Threat = Mathf.Max(4f, Threat - template.Potency * (3f + Wave * 0.22f));
                Heat = Mathf.Max(0f, Heat - template.Potency * 2f);
                Supplies += Mathf.RoundToInt(2f + template.Potency * 2f);
            }

            string payload = BuildRecipeActivationPayload(template, cause);
            lastRecipeActivationName = template.Name;
            lastRecipeActivationSummary = payload;
            lastRecipeActivationCue = BuildRecipeActivationCue(template, cause);
            AppendLog("Recipe online: " + payload);
            EmitPresentationTrigger(PresentationTriggerType.RecipeActivated, payload);
        }

        private static string BuildRecipeActivationPayload(RecipeTemplate template, string cause)
        {
            string payload = template.Name + " (" + template.Tier + ")";
            return string.IsNullOrEmpty(cause) ? payload : payload + " from " + cause;
        }

        private static string BuildRecipeActivationCue(RecipeTemplate template, string cause)
        {
            if (string.IsNullOrEmpty(cause))
            {
                return template.Name + " (" + template.Tier + ")";
            }

            return template.Name + " from " + cause;
        }

        private void RotateWeather()
        {
            WeatherType previous = Weather;
            WeatherType next = previous;
            while (next == previous)
            {
                next = (WeatherType)random.Next(0, 4);
            }

            Weather = next;
            AppendLog("Weather changed: " + Weather);
        }

        private PendingBlockState BuildRandomPendingBlock(int drawCost, DrawProfile profile, float friction01)
        {
            ShapeTemplate shape = SelectShapeTemplate(profile, friction01);
            IngredientTemplate ingredient = ingredientTemplates[random.Next(0, ingredientTemplates.Length)];

            int grade = 1;
            if (profile == DrawProfile.Power && (Wave >= 3 || random.NextDouble() < 0.35d))
            {
                grade = 2;
            }
            else if (profile == DrawProfile.Safe && friction01 >= 0.72f && random.NextDouble() < 0.22d)
            {
                grade = 2;
            }

            float damage = (3.2f + shape.Cells.Length * 0.9f + Wave * 0.25f) * ingredient.DamageMultiplier;
            float cooldown = (2.6f + shape.Cells.Length * 0.18f) * ingredient.CooldownMultiplier;

            if (profile == DrawProfile.Safe)
            {
                damage *= Mathf.Lerp(0.94f, 1.06f, friction01);
                cooldown *= Mathf.Lerp(0.96f, 0.86f, friction01);
            }
            else if (profile == DrawProfile.Power)
            {
                damage *= Mathf.Lerp(1.22f, 1.08f, friction01);
                cooldown *= 1.12f;
            }

            damage *= 1f + (grade - 1) * 0.18f;
            cooldown *= 1f - (grade - 1) * 0.07f;
            cooldown = Mathf.Max(1.35f, cooldown);

            int colorSeed = ingredient.Name.GetHashCode() ^ shape.Name.GetHashCode();
            BlockTargetType targetType = SelectTargetTypeForIngredient(ingredient.Name);

            return new PendingBlockState(
                ingredient.Name,
                grade,
                drawCost,
                damage,
                cooldown,
                shape.Cells,
                colorSeed,
                targetType,
                shape.Name);
        }

        private float EstimatePlacementFriction01()
        {
            if (placementAttemptCount <= 0)
            {
                return 0.38f;
            }

            float attempts = placementAttemptCount;
            float failRate = 1f - PlacementSuccessRate;
            float outOfBoundsRate = placementBlockedOutOfBoundsCount / attempts;
            float occupiedRate = placementBlockedOccupiedCount / attempts;
            float invalidAnchorRate = placementBlockedInvalidAnchorCount / attempts;
            float noPendingRate = placementBlockedNoPendingCount / attempts;

            float friction =
                failRate * 0.62f +
                occupiedRate * 0.24f +
                outOfBoundsRate * 0.22f +
                invalidAnchorRate * 0.10f +
                noPendingRate * 0.06f;

            if (placementAttemptCount < 5)
            {
                float settle = Mathf.Clamp01((placementAttemptCount - 1) / 4f);
                friction = Mathf.Lerp(0.40f, friction, settle);
            }

            return Mathf.Clamp01(friction);
        }

        private string GetDrawAssistTag(float friction01)
        {
            if (friction01 >= 0.62f)
            {
                return "SAFE";
            }

            if (friction01 <= 0.24f && placementAttemptCount >= 6)
            {
                return "POWER";
            }

            return "BAL";
        }

        private DrawProfile GetDrawProfileForSlot(int slotIndex, float friction01)
        {
            if (friction01 >= 0.62f)
            {
                if (slotIndex == 0)
                {
                    return DrawProfile.Safe;
                }

                if (slotIndex == 1)
                {
                    return DrawProfile.Balanced;
                }

                return random.NextDouble() < 0.70d ? DrawProfile.Safe : DrawProfile.Power;
            }

            if (friction01 <= 0.24f && placementAttemptCount >= 6)
            {
                return slotIndex == 0 ? DrawProfile.Balanced : DrawProfile.Power;
            }

            if (slotIndex == 0)
            {
                return DrawProfile.Safe;
            }

            if (slotIndex == 1)
            {
                return DrawProfile.Balanced;
            }

            return DrawProfile.Power;
        }

        private ShapeTemplate SelectShapeTemplate(DrawProfile profile, float friction01)
        {
            int stage = GetProgressionStage();
            float totalWeight = 0f;
            float[] weights = new float[shapeTemplates.Length];

            for (int i = 0; i < shapeTemplates.Length; i++)
            {
                if (!IsShapeUnlockedForStage(shapeTemplates[i], stage))
                {
                    weights[i] = 0f;
                    continue;
                }

                float weight = GetShapeWeightForProfile(shapeTemplates[i], profile, friction01);
                weights[i] = weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.01f)
            {
                return SelectRandomUnlockedShape(stage);
            }

            float roll = (float)(random.NextDouble() * totalWeight);
            for (int i = 0; i < shapeTemplates.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    return shapeTemplates[i];
                }
            }

            return SelectRandomUnlockedShape(stage);
        }

        private ShapeTemplate SelectRandomUnlockedShape(int stage)
        {
            int unlockedCount = 0;
            for (int i = 0; i < shapeTemplates.Length; i++)
            {
                if (IsShapeUnlockedForStage(shapeTemplates[i], stage))
                {
                    unlockedCount += 1;
                }
            }

            if (unlockedCount <= 0)
            {
                return shapeTemplates[random.Next(0, shapeTemplates.Length)];
            }

            int pick = random.Next(0, unlockedCount);
            for (int i = 0; i < shapeTemplates.Length; i++)
            {
                if (!IsShapeUnlockedForStage(shapeTemplates[i], stage))
                {
                    continue;
                }

                if (pick == 0)
                {
                    return shapeTemplates[i];
                }

                pick -= 1;
            }

            return shapeTemplates[0];
        }

        private float GetShapeWeightForProfile(ShapeTemplate shape, DrawProfile profile, float friction01)
        {
            int cellCount = shape.Cells.Length;
            float weight;

            switch (profile)
            {
                case DrawProfile.Safe:
                    weight = cellCount <= 2 ? 2.4f : (cellCount == 3 ? 1.2f : 0.40f);
                    weight *= Mathf.Lerp(0.85f, 1.35f, friction01);
                    break;
                case DrawProfile.Power:
                    weight = cellCount >= 4 ? 2.2f : (cellCount == 3 ? 1.05f : 0.44f);
                    weight *= Mathf.Lerp(1.35f, 0.80f, friction01);
                    break;
                default:
                    weight = cellCount == 4 ? 1.18f : 1f;
                    break;
            }

            if (shape.Name == "Dot" && profile == DrawProfile.Safe)
            {
                weight += 0.35f;
            }
            else if (shape.Name == "Square4" && profile == DrawProfile.Power)
            {
                weight += 0.35f;
            }

            return Mathf.Max(0.02f, weight);
        }

        private PendingBlockState BuildUniquePendingBlockForProfile(int drawCost, DrawProfile profile, float friction01, int maxRerolls, int ignoreChoiceIndex = -1)
        {
            PendingBlockState candidate = BuildRandomPendingBlock(drawCost, profile, friction01);
            int rerolls = 0;
            while (rerolls < Mathf.Max(1, maxRerolls) && ContainsSimilarChoice(candidate, ignoreChoiceIndex))
            {
                candidate = BuildRandomPendingBlock(drawCost, profile, friction01);
                rerolls += 1;
            }

            return candidate;
        }

        private void EnsureRecoveryChoice(int drawCost, float friction01)
        {
            bool hasSafe = false;
            for (int i = 0; i < drawChoiceProfiles.Count; i++)
            {
                if (drawChoiceProfiles[i] == DrawProfile.Safe)
                {
                    hasSafe = true;
                    break;
                }
            }

            if (hasSafe || drawChoices.Count == 0)
            {
                return;
            }

            int replaceIndex = FindLowestValueDrawChoiceIndex();
            drawChoices[replaceIndex] = BuildUniquePendingBlockForProfile(drawCost, DrawProfile.Safe, friction01, 10, replaceIndex);

            if (replaceIndex < drawChoiceProfiles.Count)
            {
                drawChoiceProfiles[replaceIndex] = DrawProfile.Safe;
            }
        }

        private int FindLowestValueDrawChoiceIndex()
        {
            int selected = 0;
            float lowest = float.MaxValue;
            for (int i = 0; i < drawChoices.Count; i++)
            {
                float value = EstimateChoiceOfferValue(drawChoices[i]);
                if (value < lowest)
                {
                    lowest = value;
                    selected = i;
                }
            }

            return selected;
        }

        private static float EstimateChoiceOfferValue(PendingBlockState choice)
        {
            float dps = choice.Damage / Mathf.Max(0.45f, choice.CooldownSeconds);
            return dps + choice.Grade * 0.60f + choice.CellCount * 0.35f;
        }

        private bool ContainsSimilarChoice(PendingBlockState candidate, int ignoreChoiceIndex = -1)
        {
            for (int i = 0; i < drawChoices.Count; i++)
            {
                if (i == ignoreChoiceIndex)
                {
                    continue;
                }

                PendingBlockState existing = drawChoices[i];
                if (existing.IngredientName == candidate.IngredientName &&
                    existing.ShapeKey == candidate.ShapeKey &&
                    existing.TargetType == candidate.TargetType)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetProgressionStage()
        {
            if (Wave >= 7)
            {
                return 3;
            }

            if (Wave >= 4)
            {
                return 2;
            }

            return 1;
        }

        private static bool IsShapeUnlockedForStage(ShapeTemplate shape, int stage)
        {
            if (stage <= 1)
            {
                return shape.Cells.Length <= 3;
            }

            if (stage == 2)
            {
                return shape.Name != "Square4";
            }

            return true;
        }

        private static bool IsTargetUnlockedForStage(BlockTargetType targetType, int stage)
        {
            if (stage <= 1)
            {
                return targetType == BlockTargetType.Nearest;
            }

            if (stage == 2)
            {
                return targetType != BlockTargetType.RandomLane;
            }

            return true;
        }

        private BlockTargetType ResolveTargetTypeByProgression(BlockTargetType preferred)
        {
            int stage = GetProgressionStage();
            if (IsTargetUnlockedForStage(preferred, stage))
            {
                return preferred;
            }

            if (stage <= 1)
            {
                return BlockTargetType.Nearest;
            }

            BlockTargetType[] fallback = { BlockTargetType.Nearest, BlockTargetType.Farthest, BlockTargetType.HighestHp };
            return fallback[random.Next(0, fallback.Length)];
        }

        private BlockTargetType SelectTargetTypeForIngredient(string ingredientName)
        {
            BlockTargetType preferred;
            switch (ingredientName)
            {
                case "Onion":
                case "Pork":
                    preferred = BlockTargetType.Nearest;
                    break;
                case "Beef":
                case "Garlic":
                    preferred = BlockTargetType.HighestHp;
                    break;
                case "Shrimp":
                case "Seaweed":
                    preferred = BlockTargetType.RandomLane;
                    break;
                case "Chili":
                case "Rice":
                    preferred = BlockTargetType.Farthest;
                    break;
                default:
                    preferred = (BlockTargetType)random.Next(0, 4);
                    break;
            }

            return ResolveTargetTypeByProgression(preferred);
        }

        private static string TargetTypeToShortCode(BlockTargetType targetType)
        {
            switch (targetType)
            {
                case BlockTargetType.Farthest:
                    return "F";
                case BlockTargetType.HighestHp:
                    return "H";
                case BlockTargetType.RandomLane:
                    return "R";
                default:
                    return "N";
            }
        }

        private Vector2Int[] GetPendingRotatedOffsets()
        {
            if (pendingBlock == null)
            {
                return Array.Empty<Vector2Int>();
            }

            if (pendingRotationQuarterTurns == 0)
            {
                return pendingBlock.CellOffsets;
            }

            Vector2Int[] source = pendingBlock.CellOffsets;
            Vector2Int[] rotated = new Vector2Int[source.Length];
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            for (int i = 0; i < source.Length; i++)
            {
                int x = source[i].x;
                int y = source[i].y;

                for (int t = 0; t < pendingRotationQuarterTurns; t++)
                {
                    int nextX = y;
                    int nextY = -x;
                    x = nextX;
                    y = nextY;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                rotated[i] = new Vector2Int(x, y);
            }

            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = new Vector2Int(rotated[i].x - minX, rotated[i].y - minY);
            }

            return rotated;
        }
        private bool CanPendingFitAnywhere()
        {
            if (pendingBlock == null)
            {
                return false;
            }

            for (int anchor = 0; anchor < InventoryCellCount; anchor++)
            {
                if (TryCollectPendingCells(anchor, true, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }
        public int GetDrawChoicePickCount(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= drawChoicePickCounts.Length)
            {
                return 0;
            }

            return drawChoicePickCounts[slotIndex];
        }

        public string GetDrawChoiceAssistTag(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= drawChoices.Count || slotIndex >= drawChoiceProfiles.Count)
            {
                return string.Empty;
            }

            return DrawProfileToTag(drawChoiceProfiles[slotIndex]);
        }

        private static string DrawProfileToTag(DrawProfile profile)
        {
            switch (profile)
            {
                case DrawProfile.Safe:
                    return "SAFE";
                case DrawProfile.Power:
                    return "POWER";
                default:
                    return "BAL";
            }
        }

        private void RegisterPlacementFailure(PlacementFailReason reason)
        {
            switch (reason)
            {
                case PlacementFailReason.OutOfBounds:
                    placementBlockedOutOfBoundsCount += 1;
                    break;
                case PlacementFailReason.Occupied:
                    placementBlockedOccupiedCount += 1;
                    break;
                case PlacementFailReason.InvalidAnchor:
                    placementBlockedInvalidAnchorCount += 1;
                    break;
                case PlacementFailReason.NoPendingBlock:
                    placementBlockedNoPendingCount += 1;
                    break;
            }
        }

        private static string GetPlacementFailReasonText(PlacementFailReason reason)
        {
            switch (reason)
            {
                case PlacementFailReason.NoPendingBlock:
                    return "No pending block.";
                case PlacementFailReason.InvalidAnchor:
                    return "Invalid anchor cell.";
                case PlacementFailReason.OutOfBounds:
                    return "Block exceeds grid bounds.";
                case PlacementFailReason.Occupied:
                    return "That slot is already occupied.";
                default:
                    return string.Empty;
            }
        }

        private bool TryCollectPendingCells(int anchorCellIndex, bool requireEmpty, out int[] cells, out string reason)
        {
            cells = null;
            reason = string.Empty;

            if (pendingBlock == null)
            {
                reason = "No pending block.";
                lastPlacementFailReason = PlacementFailReason.NoPendingBlock;
                return false;
            }

            if (anchorCellIndex < 0 || anchorCellIndex >= InventoryCellCount)
            {
                reason = "Invalid anchor cell.";
                lastPlacementFailReason = PlacementFailReason.InvalidAnchor;
                return false;
            }

            int anchorX = anchorCellIndex % InventoryWidth;
            int anchorY = anchorCellIndex / InventoryWidth;
            Vector2Int[] offsets = GetPendingRotatedOffsets();
            int[] resolved = new int[offsets.Length];

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int offset = offsets[i];
                int x = anchorX + offset.x;
                int y = anchorY + offset.y;
                if (x < 0 || x >= InventoryWidth || y < 0 || y >= InventoryHeight)
                {
                    reason = "Block exceeds grid bounds.";
                    lastPlacementFailReason = PlacementFailReason.OutOfBounds;
                    return false;
                }

                int index = y * InventoryWidth + x;
                if (requireEmpty && blockByCell[index] >= 0)
                {
                    reason = "That slot is already occupied.";
                    lastPlacementFailReason = PlacementFailReason.Occupied;
                    return false;
                }

                resolved[i] = index;
            }

            cells = resolved;
            lastPlacementFailReason = PlacementFailReason.None;
            return true;
        }

        private void RemovePlacedBlock(int blockId)
        {
            if (!placedBlocks.TryGetValue(blockId, out PlacedBlockState block))
            {
                return;
            }

            for (int i = 0; i < block.OccupiedCellIndices.Length; i++)
            {
                int cell = block.OccupiedCellIndices[i];
                if (cell >= 0 && cell < blockByCell.Length && blockByCell[cell] == blockId)
                {
                    blockByCell[cell] = -1;
                }
            }

            placedBlocks.Remove(blockId);
            RecalculateInventoryFill();
        }

        private void RecalculateInventoryFill()
        {
            int filled = 0;
            for (int i = 0; i < blockByCell.Length; i++)
            {
                if (blockByCell[i] >= 0)
                {
                    filled++;
                }
            }

            InventoryCellsFilled = filled;
        }

        private RunEvent BuildEvent()
        {
            int eventId = random.Next(0, 3);
            if (eventId == 0)
            {
                return new RunEvent(
                    "Abandoned Street Market",
                    "You found a partially intact market. High reward, high kitchen stress.",
                    new[]
                    {
                        new RunEventOption(RunEventOptionType.ScavengeMarket, "Scavenge",
                            "+18 Supplies, +12 Heat, +2 Threat"),
                        new RunEventOption(RunEventOptionType.ReinforceTruck, "Reinforce Truck",
                            "-12 Supplies, +25 Truck HP"),
                        new RunEventOption(RunEventOptionType.CoolKitchen, "Cool Kitchen",
                            "-20 Heat, -6 Momentum")
                    });
            }

            if (eventId == 1)
            {
                return new RunEvent(
                    "Radio Distress Call",
                    "Nearby survivors offer labor if you spend resources now.",
                    new[]
                    {
                        new RunEventOption(RunEventOptionType.StreetShow, "Street Show",
                            "+14 Supplies, +10 Momentum, +2 Threat"),
                        new RunEventOption(RunEventOptionType.HireMechanic, "Hire Mechanic",
                            "-15 Supplies, +20 Max HP, +1 Fuel"),
                        new RunEventOption(RunEventOptionType.RiskyShortcut, "Risky Shortcut",
                            "50/50: +30 Supplies or -20 Truck HP")
                    });
            }

            return new RunEvent(
                "Fuel Depot Ruins",
                "Damaged tanks still contain resources if handled correctly.",
                new[]
                {
                    new RunEventOption(RunEventOptionType.SalvageFuel, "Salvage Fuel",
                        "+1 Fuel, +10 Supplies, +8 Heat"),
                    new RunEventOption(RunEventOptionType.SealLeak, "Seal Leak",
                        "-8 Supplies, -6 Threat, -5 Heat"),
                    new RunEventOption(RunEventOptionType.RecipeRush, "Recipe Rush",
                        "Trigger 2 random recipes, +5 Heat")
                });
        }

        private void ResolveEventOption(RunEventOptionType optionType)
        {
            switch (optionType)
            {
                case RunEventOptionType.ScavengeMarket:
                    Supplies += 18;
                    AddHeatProgressive(12f);
                    Threat += 2f;
                    break;
                case RunEventOptionType.ReinforceTruck:
                    Supplies = Mathf.Max(0, Supplies - 12);
                    TruckHp = Mathf.Min(MaxTruckHp, TruckHp + 25f);
                    break;
                case RunEventOptionType.CoolKitchen:
                    Heat = Mathf.Max(0f, Heat - 20f);
                    Momentum = Mathf.Max(0f, Momentum - 6f);
                    break;
                case RunEventOptionType.StreetShow:
                    Supplies += 14;
                    Momentum += 10f;
                    Threat += 2f;
                    break;
                case RunEventOptionType.HireMechanic:
                    Supplies = Mathf.Max(0, Supplies - 15);
                    MaxTruckHp += 20f;
                    TruckHp = Mathf.Min(MaxTruckHp, TruckHp + 20f);
                    Fuel += 1;
                    break;
                case RunEventOptionType.RiskyShortcut:
                    if (random.NextDouble() < 0.5d)
                    {
                        Supplies += 30;
                    }
                    else
                    {
                        TruckHp = Mathf.Max(1f, TruckHp - 20f);
                    }
                    break;
                case RunEventOptionType.SalvageFuel:
                    Fuel += 1;
                    Supplies += 10;
                    AddHeatProgressive(8f);
                    break;
                case RunEventOptionType.SealLeak:
                    Supplies = Mathf.Max(0, Supplies - 8);
                    Threat = Mathf.Max(4f, Threat - 6f);
                    Heat = Mathf.Max(0f, Heat - 5f);
                    break;
                case RunEventOptionType.RecipeRush:
                    TriggerRandomRecipe("Recipe Rush event");
                    TriggerRandomRecipe("Recipe Rush event");
                    AddHeatProgressive(5f);
                    break;
            }
        }

        private void AppendLog(string message)
        {
            CombatLogAppended?.Invoke(message);
        }

        private void EmitPresentationTrigger(PresentationTriggerType triggerType, string payload)
        {
            PresentationTriggered?.Invoke(triggerType, payload ?? string.Empty);
        }

        private void RaiseChanged()
        {
            UpdateHeatBandFeedback();
            StateChanged?.Invoke();
        }

        private readonly struct IngredientTemplate
        {
            public IngredientTemplate(string name, float damageMultiplier, float cooldownMultiplier)
            {
                Name = name;
                DamageMultiplier = damageMultiplier;
                CooldownMultiplier = cooldownMultiplier;
            }

            public string Name { get; }
            public float DamageMultiplier { get; }
            public float CooldownMultiplier { get; }
        }

        private readonly struct ShapeTemplate
        {
            public ShapeTemplate(string name, Vector2Int[] cells)
            {
                Name = name;
                Cells = cells;
            }

            public string Name { get; }
            public Vector2Int[] Cells { get; }
        }

        private readonly struct RecipeTemplate
        {
            public RecipeTemplate(
                string name,
                RecipeTier tier,
                bool isPassive,
                float potency,
                float durationSeconds)
            {
                Name = name;
                Tier = tier;
                IsPassive = isPassive;
                Potency = potency;
                DurationSeconds = durationSeconds;
            }

            public string Name { get; }
            public RecipeTier Tier { get; }
            public bool IsPassive { get; }
            public float Potency { get; }
            public float DurationSeconds { get; }
        }
    }
}













































