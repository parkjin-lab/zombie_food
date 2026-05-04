
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieFoodcenter.Prototype
{
    [DefaultExecutionOrder(-500)]
    public sealed partial class FoodTruckPrototypeHud : MonoBehaviour
    {
        private sealed class MeterWidget
        {
            public RectTransform RowRect;
            public LayoutElement RowLayout;
            public RectTransform FillRect;
            public Image FillImage;
            public Text ValueText;
        }

        private sealed class EnemyVisualWidget
        {
            public RectTransform Rect;
            public Image Icon;
            public Text HpText;
            public int LaneIndex;
            public int EnemyId;
            public bool IsSpecial;
            public float LastHp;
            public float LastDistance01;
            public float HitTimer;
            public float AnimationOffset;
        }



        private sealed class EnemyAfterVisualWidget
        {
            public RectTransform Rect;
            public Image Icon;
            public float Remaining;
            public float FadeDuration;
        }

        private sealed class EnemyHitEffectWidget
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Remaining;
            public float Duration;
        }
        private sealed class UndeadSpriteSet
        {
            public Sprite[] RunFrames;
            public Sprite HitFrame;
            public Sprite DeadFrame;
        }
        [Serializable]
        private struct IngredientSpriteEntry
        {
            public string IngredientName;
            public Sprite Sprite;
        }

        private enum PlacementFeedbackPreset
        {
            Soft,
            Balanced,
            Punchy
        }

        private enum RiskIconGlyph
        {
            Circle,
            Diamond,
            Triangle
        }

        public readonly struct GameplayFocusLayoutMetrics
        {
            public GameplayFocusLayoutMetrics(float topStart01, float bottomTop01, bool minimalCombatStrip)
            {
                TopStart01 = topStart01;
                BottomTop01 = bottomTop01;
                MinimalCombatStrip = minimalCombatStrip;
            }

            public float TopStart01 { get; }
            public float BottomTop01 { get; }
            public bool MinimalCombatStrip { get; }
            public float CenterViewport01 => Mathf.Clamp01(TopStart01 - BottomTop01);
        }

        public readonly struct GameplayActionVisibility
        {
            public GameplayActionVisibility(bool showPrimaryActionRow, bool showSecondaryActionRow, bool showInventoryGrid)
            {
                ShowPrimaryActionRow = showPrimaryActionRow;
                ShowSecondaryActionRow = showSecondaryActionRow;
                ShowInventoryGrid = showInventoryGrid;
            }

            public bool ShowPrimaryActionRow { get; }
            public bool ShowSecondaryActionRow { get; }
            public bool ShowInventoryGrid { get; }
            public bool ShowsAnyActionRow => ShowPrimaryActionRow || ShowSecondaryActionRow;
        }

        public static GameplayFocusLayoutMetrics CalculateGameplayFocusLayout(
            float viewportWidth,
            float viewportHeight,
            bool hasPlacementContext,
            bool dragFocus,
            bool minimalCombatStripRequested)
        {
            float width = Mathf.Max(1f, viewportWidth);
            float height = Mathf.Max(1f, viewportHeight);
            float portrait01 = Mathf.Clamp01((height / width - 1.4f) / 1.2f);
            bool combatOnlyFocus = !dragFocus && !hasPlacementContext;
            bool minimalCombatStrip = combatOnlyFocus && minimalCombatStripRequested;

            float topStartFocus;
            if (dragFocus)
            {
                topStartFocus = Mathf.Lerp(0.91f, 0.94f, portrait01);
            }
            else if (minimalCombatStrip)
            {
                topStartFocus = Mathf.Lerp(0.94f, 0.965f, portrait01);
            }
            else if (combatOnlyFocus)
            {
                topStartFocus = Mathf.Lerp(0.38f, 0.42f, portrait01);
            }
            else
            {
                topStartFocus = Mathf.Lerp(0.62f, 0.66f, portrait01);
            }

            float bottomTopFocus;
            if (dragFocus)
            {
                bottomTopFocus = Mathf.Lerp(0.16f, 0.20f, portrait01);
            }
            else if (hasPlacementContext)
            {
                bottomTopFocus = Mathf.Lerp(0.54f, 0.58f, portrait01);
            }
            else
            {
                bottomTopFocus = minimalCombatStrip
                    ? Mathf.Lerp(0.10f, 0.14f, portrait01)
                    : Mathf.Lerp(0.13f, 0.16f, portrait01);
            }

            float focusGap = dragFocus ? 0.24f : (minimalCombatStrip ? 0.10f : 0.04f);
            if (bottomTopFocus > topStartFocus - focusGap)
            {
                bottomTopFocus = topStartFocus - focusGap;
            }

            return new GameplayFocusLayoutMetrics(topStartFocus, bottomTopFocus, minimalCombatStrip);
        }

        public static GameplayActionVisibility CalculateGameplayActionVisibility(
            bool gameplayFocusHud,
            bool combatOnlyFocus,
            bool hasPlacementContext,
            bool hasDrawChoice,
            bool eventPending,
            bool canVentHeat,
            bool canActivateComboBurst,
            bool overheated)
        {
            bool flowLocked = hasDrawChoice || eventPending;
            bool urgentCombatAction = canVentHeat || canActivateComboBurst || overheated;
            bool showPrimary = !gameplayFocusHud || (!flowLocked && (!combatOnlyFocus || !urgentCombatAction));
            bool showSecondary = !gameplayFocusHud || canVentHeat || canActivateComboBurst || overheated;
            bool showInventoryGrid = !gameplayFocusHud || hasPlacementContext;
            return new GameplayActionVisibility(showPrimary, showSecondary, showInventoryGrid);
        }

        private FoodTruckRunModel model;
        private Font uiFont;

        private RectTransform canvasRect;
        private RectTransform topPanelRect;
        private RectTransform midPanelRect;
        private RectTransform bottomPanelRect;
        private RectTransform laneContainerRect;
        private RectTransform meterContainerRect;
        private RectTransform mapControlRowRect;
        private RectTransform flowChecklistPanelRect;
        private RectTransform telemetryControlRowRect;
        private RectTransform pendingRowRect;
        private RectTransform actionsRow1Rect;
        private RectTransform actionsRow2Rect;

        private Text titleText;
        private Text topLineText;
        private Text secondLineText;
        private Text combatLineText;
        private Text logText;
        private Text inventoryTitleText;
        private RectTransform cueBannerRect;
        private Text cueBannerText;
        private Image cueBannerBackground;
        private Image placementImpactImage;
        private RectTransform prototypeVfxRect;
        private Image prototypeVfxImage;
        private readonly Text[] flowChecklistTexts = new Text[5];
        private LayoutElement laneContainerLayoutElement;
        private LayoutElement meterContainerLayoutElement;
        private LayoutElement pendingRowLayoutElement;
        private LayoutElement actionsRow1LayoutElement;
        private LayoutElement actionsRow2LayoutElement;

        private readonly MeterWidget hpMeter = new MeterWidget();
        private readonly MeterWidget waveMeter = new MeterWidget();
        private readonly MeterWidget heatMeter = new MeterWidget();
        private readonly MeterWidget momentumMeter = new MeterWidget();

        private readonly RectTransform[] laneTrackRoots = new RectTransform[3];
        private readonly Text[] laneTexts = new Text[3];
        private readonly Dictionary<int, EnemyVisualWidget> enemyVisuals = new Dictionary<int, EnemyVisualWidget>();
        private readonly List<EnemyAfterVisualWidget> enemyAfterVisuals = new List<EnemyAfterVisualWidget>();
        private readonly List<EnemyHitEffectWidget> enemyHitEffects = new List<EnemyHitEffectWidget>();
        private readonly Image[] laneTrackImages = new Image[3];
        private readonly Image[] laneTruckImages = new Image[3];
        private readonly Text[] laneTruckTexts = new Text[3];
        private readonly LayoutElement[] laneRowLayoutElements = new LayoutElement[3];
        private readonly float[] laneHitFlashTimers = new float[3];
        private float previousTruckHp = -1f;
        private int previousWaveDisplay = -1;
        private bool previousRestPhase;
        private bool previousHasDrawChoiceState;
        private bool previousHasPendingBlockState;
        private bool previousEventPendingState;
        private bool previousCanVentHeatState;
        private bool previousCanActivateBurstState;
        private bool previousNextWaveReadyState;

        private readonly List<Image> inventoryCells = new List<Image>();
        private readonly List<Text> inventoryCellLabels = new List<Text>();
        private readonly List<RectTransform> inventoryCellRects = new List<RectTransform>();
        private readonly List<Image> inventoryGhostCells = new List<Image>();
        private readonly List<Image> inventoryRecommendationRings = new List<Image>();
        private RectTransform inventoryGridShakeRoot;
        private RectTransform inventoryGridShakeLayer;
        private RectTransform inventoryGridHolder;
        private GridLayoutGroup inventoryGridLayout;
        private LayoutElement inventoryGridLayoutElement;

        private RectTransform pendingSlotRect;
        private RectTransform pendingHelpRect;
        private RectTransform pendingTokenRect;
        private Text pendingTokenText;
        private Text pendingHintText;
        private Text pendingHelpText;
        private Text pendingRecommendationAssistText;
        private Image[] pendingTokenPreviewCells = Array.Empty<Image>();
        private RectTransform pendingDragGhostRect;
        private Image[] pendingDragGhostCells = Array.Empty<Image>();
        private Image[] pendingDragGhostHighlights = Array.Empty<Image>();
        private Image[] pendingDragGhostOutlines = Array.Empty<Image>();
        private Image pendingTokenIngredientImage;
        private Text pendingTokenIngredientBadge;
        private Image pendingTokenBackgroundImage;
        private RectTransform pendingRotateLeftHotspot;
        private RectTransform pendingRotateRightHotspot;

        private bool isDraggingPending;
        private int selectedMergeCell = -1;
        private Vector2 pendingDragOffset;
        private bool dragPointerIsTouch;
        private int dragTouchId = -1;
        private Vector2 lastDragPointerScreenPos;
        private int pendingHoverAnchorCell = -1;
        private bool pendingHoverValid;
        private string lastPlacementBlockedHint = string.Empty;
        private PlacementFailReason lastPlacementBlockedFailReason = PlacementFailReason.None;
        private readonly int[] recommendedAnchorCells = { -1, -1 };
        private readonly float[] recommendedAnchorScores = { float.MinValue, float.MinValue };
        private int recommendedAnchorCount;
        private float recommendationPulseTimer;
        private int recommendationPulseAnchorCell = -1;
        private int lastRecommendedPrimaryAnchor = -1;
        private bool recommendationAssistWasLocked;
        private bool recommendationAssistLocked;
        private int recommendationAssistLockAnchorCell = -1;
        private float recommendationAssistFlashTimer;
        private float recommendationHapticCooldownTimer;
        private float pendingPlacementFeedbackTimer;
        private int[] pendingPlacementFeedbackCells = Array.Empty<int>();
        private bool pendingPlacementFeedbackSuccess;
        private float placementImpactTimer;
        private bool placementImpactSuccess;
        private float pendingTokenPulseTimer;
        private bool pendingTokenPulseSuccess;
        private Vector3 pendingTokenBaseScale = Vector3.one;
        private readonly Vector3[] inventoryCellBaseScales = new Vector3[FoodTruckRunModel.InventoryCellCount];
        private readonly Queue<string> recentLogs = new Queue<string>();
        private readonly string[] lastFlowChecklistLogLines = new string[5];
        private bool flowChecklistLogPrimed;
        private string lastFlowChecklistLogMessage = string.Empty;
        private float flowChecklistLogCooldownTimer;
        private bool telemetryRunHasMeaningfulData;
        private float telemetryDrawToPlaceTimer = -1f;
        private float telemetryDrawToPlaceTotalSeconds;
        private int telemetryDrawToPlaceSamples;
        private int telemetryOverheatEntryCountRun;
        private readonly Dictionary<int, int> telemetryOverheatEntriesByWave = new Dictionary<int, int>();
        private readonly int[] telemetryDrawPickValueBucketCounts = new int[3];
        private readonly int[] telemetryDrawPickRiskTagCounts = new int[3];
        private readonly int[] telemetryWaveBaseDrawPickValueBucketCounts = new int[3];
        private readonly int[] telemetryWaveBaseDrawPickRiskTagCounts = new int[3];
        private bool telemetryPrevHasDrawChoice;
        private bool telemetryPrevIsOverheated;
        private int telemetryPrevPlacementSuccessCount;
        private int telemetryPrevWave;
        private int telemetryRunSessionId = 1;
        private int telemetryExportSequence = 1;
        private int telemetryManualMergeSuccessCount;
        private RectTransform synergyContainer;

        private GameObject eventPanel;
        private Text eventTitleText;
        private Text eventDescText;
        private readonly List<Button> eventOptionButtons = new List<Button>();
        private readonly List<Text> eventOptionTexts = new List<Text>();
        private Button speedCycleButton;
        private Text speedCycleButtonText;
        private Button hudDensityButton;
        private Text hudDensityButtonText;
        private Button telemetryToggleButton;
        private Text telemetryToggleButtonText;
        private Button telemetryScopeButton;
        private Text telemetryScopeButtonText;
        private Button drawButton;
        private Text drawButtonText;
        private Button sellButton;
        private Text sellButtonText;
        private Button rotateLeftButton;
        private Button rotateRightButton;
        private Button recipeButton;
        private Text recipeButtonText;
        private Button nextWaveButton;
        private Text nextWaveButtonText;
        private Button comboBurstButton;
        private Text comboBurstButtonText;
        private Button ventButton;
        private Text ventButtonText;
        private Button exportTelemetryButton;
        private Button resetRunButton;
        private RectTransform drawChoiceRow;
        private LayoutElement drawChoiceRowLayoutElement;
        private readonly List<Button> drawChoiceButtons = new List<Button>();
        private readonly List<Text> drawChoiceTexts = new List<Text>();
        private readonly List<Image> drawChoiceGuideRings = new List<Image>();
        private readonly List<RectTransform> drawChoiceGuideBadgeRoots = new List<RectTransform>();
        private readonly List<Text> drawChoiceGuidePointers = new List<Text>();
        private readonly List<RectTransform> drawChoiceGuideSweeps = new List<RectTransform>();
        private readonly List<Image[]> drawChoicePreviewCells = new List<Image[]>();
        private readonly List<Image[]> drawChoicePreviewHighlights = new List<Image[]>();
        private readonly List<Image[]> drawChoicePreviewOutlines = new List<Image[]>();
        private readonly List<Image> drawChoiceIngredientImages = new List<Image>();
        private readonly List<Text> drawChoiceIngredientLabels = new List<Text>();
        private readonly List<Image> drawChoiceValueBarFills = new List<Image>();
        private readonly List<Text> drawChoiceValueBarValues = new List<Text>();
        private readonly List<Image> drawChoiceRiskBarFills = new List<Image>();
        private readonly List<Text> drawChoiceRiskBarValues = new List<Text>();
        private readonly List<Image> drawChoiceRiskBarIcons = new List<Image>();
        private readonly Dictionary<string, Sprite> ingredientSpriteCache = new Dictionary<string, Sprite>();
        private readonly List<UndeadSpriteSet> undeadEnemySpriteSets = new List<UndeadSpriteSet>();
        private readonly List<UndeadSpriteSet> undeadSpecialSpriteSets = new List<UndeadSpriteSet>();
        private bool undeadSpriteSetsLoaded;
        private Sprite riskIconHighSprite;
        private Sprite riskIconMidSprite;
        private Sprite riskIconLowSprite;
        private Texture2D riskIconHighTexture;
        private Texture2D riskIconMidTexture;
        private Texture2D riskIconLowTexture;
        private RectTransform telemetryPanelRect;
        private LayoutElement telemetryPanelLayoutElement;
        private Text telemetryPanelText;

        [SerializeField]
        private float enemyRunAnimationFps = 10f;

        [SerializeField]
        private float enemyHitPoseDuration = 0.14f;

        [SerializeField]
        private float enemyIconSize = 34f;

        [SerializeField]
        private float enemyDeathHoldSeconds = 0.34f;

        [SerializeField]
        private float enemyDeathFadeSeconds = 0.24f;

        [SerializeField]
        private float enemyHitEffectDuration = 0.20f;

        [SerializeField]
        private float enemyHitEffectSize = 16f;

        [SerializeField]
        private float enemyHitEffectTravelSpeed = 56f;

        [SerializeField]
        private float enemyHitShakeDistance = 8f;

        [SerializeField]
        private float enemyHitScalePulse = 0.16f;

        [SerializeField]
        private float laneHitFlashDuration = 0.22f;

        [SerializeField]
        private Color laneBaseColor = new Color(0.11f, 0.14f, 0.18f, 1f);

        [SerializeField]
        private Color laneHitFlashColor = new Color(0.72f, 0.18f, 0.14f, 0.98f);

        [SerializeField]
        private float eventResolvePanelPulseDuration = 0.32f;

        [SerializeField]
        private float comboBurstButtonPulseDuration = 0.34f;

        [SerializeField]
        private float nextWaveButtonPulseDuration = 0.40f;

        [SerializeField]
        private float progressionUnlockPanelPulseDuration = 0.70f;

        [SerializeField]
        private float synergyChipPulseDuration = 0.55f;

        [SerializeField]
        private IngredientSpriteEntry[] ingredientSpriteEntries = Array.Empty<IngredientSpriteEntry>();

        [Header("Prototype Art")]
        [SerializeField]
        private bool autoLoadPrototypeArt = true;

        [SerializeField]
        private Sprite foodTruckSprite;

        [SerializeField]
        private Sprite kitchenModuleSprite;

        [Header("Prototype VFX")]
        [SerializeField]
        private bool autoLoadPrototypeVfx = true;

        [SerializeField]
        private Sprite vfxPlaceSuccessSprite;

        [SerializeField]
        private Sprite vfxPlaceFailSprite;

        [SerializeField]
        private Sprite vfxComboBurstSprite;

        [SerializeField]
        private Sprite vfxOverheatSpikeSprite;

        [SerializeField]
        private Sprite vfxHeatWarningSprite;

        [SerializeField]
        private Sprite placementFailNoPendingSprite;

        [SerializeField]
        private Sprite placementFailInvalidAnchorSprite;

        [SerializeField]
        private Sprite placementFailOutOfBoundsSprite;

        [SerializeField]
        private Sprite placementFailOccupiedSprite;

        [SerializeField]
        private float prototypeVfxDuration = 0.44f;

        [SerializeField]
        private float prototypeVfxSize = 180f;

        [Header("Audio")]
        [SerializeField]
        private bool autoLoadPrototypeSfx = true;

        [SerializeField]
        private float sfxVolume = 0.85f;

        [SerializeField]
        private AudioSource sfxAudioSource;

        [SerializeField]
        private AudioClip sfxSelectClip;

        [SerializeField]
        private AudioClip sfxPlaceSuccessClip;

        [SerializeField]
        private AudioClip sfxPlaceBlockedClip;

        [SerializeField]
        private AudioClip sfxComboBurstClip;

        [SerializeField]
        private AudioClip sfxOverheatClip;

        [SerializeField]
        private AudioClip sfxProgressionUnlockClip;

        [SerializeField]
        private AudioClip sfxHeatWarningClip;

        [SerializeField]
        private AudioClip sfxHeatStabilizedClip;

        [SerializeField]
        private float pendingPlacementFeedbackDuration = 0.26f;

        [SerializeField]
        private float placementImpactDuration = 0.18f;

        [SerializeField]
        private float placementImpactSuccessAlpha = 0.18f;

        [SerializeField]
        private float placementImpactFailAlpha = 0.28f;

        [SerializeField]
        private Color placementImpactSuccessColor = new Color(0.22f, 0.90f, 0.42f, 1f);

        [SerializeField]
        private Color placementImpactFailColor = new Color(0.95f, 0.34f, 0.28f, 1f);

        [SerializeField]
        private PlacementFeedbackPreset placementFeedbackPreset = PlacementFeedbackPreset.Balanced;

        [SerializeField]
        private bool adaptivePlacementFeedbackPreset = true;

        [SerializeField]
        private float placementFailShakeDuration = 0.20f;

        [SerializeField]
        private float placementFailShakeDistance = 12f;

        [SerializeField]
        private float placementFailShakeFrequency = 28f;

        [SerializeField]
        private float pendingTokenPulseDuration = 0.18f;

        [SerializeField]
        private float cueBannerDuration = 1.65f;

        [SerializeField]
        private float cueBannerMinimalDurationScale = 0.72f;

        [SerializeField]
        private float cueBannerMinimalAlphaScale = 0.56f;

        [SerializeField]
        private float overheatSpikePulseDuration = 0.55f;

        [SerializeField]
        private float overheatSpikePulseStrength = 0.68f;

        [SerializeField]
        private float heatStatePulseDuration = 0.42f;

        [SerializeField]
        private float heatStatePulseStrength = 0.52f;

        [SerializeField]
        private float ventReadyPulseDuration = 0.46f;

        [SerializeField]
        private float ventReadyPulseStrength = 0.58f;

        [SerializeField]
        private float drawChoicePickImpactDuration = 0.26f;

        [SerializeField]
        private float drawChoiceRiskIconSize = 12f;

        [SerializeField]
        private float drawChoiceRiskIconSizeTallPortrait = 15f;

        [SerializeField]
        private float drawChoiceRiskValueWidth = 34f;

        [SerializeField]
        private float drawChoiceRiskValueWidthTallPortrait = 40f;

        [SerializeField]
        private int drawChoiceRiskValueFontSize = 10;

        [SerializeField]
        private int drawChoiceRiskValueFontSizeTallPortrait = 11;

        [SerializeField]
        private float recommendationSnapRadiusPixels = 84f;

        [SerializeField]
        private float recommendationPulseDuration = 0.34f;

        [SerializeField]
        private float recommendationAssistLockDistancePixels = 26f;

        [SerializeField]
        private float recommendationAssistFlashDuration = 0.20f;

        [SerializeField]
        private bool recommendationLockHapticOnMobile = true;

        [SerializeField]
        private float recommendationHapticCooldownSeconds = 0.35f;

        [SerializeField]
        private float recommendationScoreWeightLaneAverage = 1f;

        [SerializeField]
        private float recommendationScoreWeightLaneMax = 0.55f;

        [SerializeField]
        private float recommendationScoreWeightCoverage = 0.22f;

        [SerializeField]
        private float recommendationCenterBaseBonus = 0.34f;

        [SerializeField]
        private float recommendationCenterDistancePenalty = 0.12f;

        [SerializeField]
        private bool syncChecklistToPlayLog = true;

        [SerializeField]
        private float checklistLogMinInterval = 0.50f;

        [SerializeField]
        private bool autoExportTelemetryOnRunReset = true;

        [SerializeField]
        private bool autoExportTelemetryOnQuit = true;

        [SerializeField]
        private bool telemetryConsoleSummaryOnExport = true;

        [SerializeField]
        private string telemetryCsvFileName = "foodtruck_ux_telemetry.csv";

        [SerializeField]
        private bool inventoryLayoutCorrectionDiagnostics = false;

        [SerializeField]
        private float inventoryLayoutCorrectionLogCooldown = 0.75f;

        [SerializeField]
        private int inventoryLayoutCorrectionBurstThreshold = 4;

        [SerializeField]
        private float inventoryLayoutCorrectionBurstWindow = 1.5f;

        [SerializeField]
        private float inventoryLayoutTransformEpsilon = 0.0005f;

        [SerializeField]
        private float inventoryLayoutRotationEpsilonDeg = 0.05f;

        [SerializeField]
        private float inventoryLayoutAnchorEpsilon = 0.0005f;

        [SerializeField]
        private float inventoryLayoutPositionEpsilon = 0.05f;

        [Header("HUD Focus")]
        [SerializeField]
        private bool autoGameplayFocusHud = true;

        [SerializeField]
        private float topPanelFocusAlpha = 0.26f;

        [SerializeField]
        private float bottomPanelFocusAlpha = 0.22f;

        [SerializeField]
        private float topPanelExpandedAlpha = 0.92f;

        [SerializeField]
        private float bottomPanelExpandedAlpha = 0.92f;

        [SerializeField]
        private float ultraFocusReleaseHoldSeconds = 0.14f;

        [SerializeField]
        private float pendingTokenUltraFocusScale = 0.82f;

        [SerializeField]
        private float pendingTokenUltraFocusAlpha = 0.68f;

        [SerializeField]
        private float minimalCombatStripEnterDelaySeconds = 0.40f;

        [SerializeField]
        private float minimalCombatStripExitHoldSeconds = 0.35f;

        [SerializeField]
        private float minimalCombatStripLanePressureEnterThreshold = 12f;

        [SerializeField]
        private float minimalCombatStripLanePressureExitThreshold = 15f;

        [SerializeField]
        private bool minimalCombatStripImmediateExitOnThreat = true;

        [SerializeField]
        private bool minimalCombatStripEnabled = false;

        [SerializeField]
        private float inventoryCellMinSizeExpanded = 92f;

        [SerializeField]
        private float inventoryCellMinSizeFocusPlacement = 84f;

        [SerializeField]
        private float inventoryCellMinSizeFocusCombat = 74f;

        [SerializeField]
        private float inventoryCellMinSizeUltraFocus = 66f;

        [SerializeField]
        private float inventoryCellMaxSizeExpanded = 176f;

        [SerializeField]
        private float inventoryCellMaxSizeFocusPlacement = 156f;

        [SerializeField]
        private float inventoryCellMaxSizeFocusCombat = 136f;

        [SerializeField]
        private float inventoryCellMaxSizeUltraFocus = 118f;

        [SerializeField]
        private int inventoryCellLabelFontSizeExpanded = 16;

        [SerializeField]
        private int inventoryCellLabelFontSizeFocusPlacement = 15;

        [SerializeField]
        private int inventoryCellLabelFontSizeFocusCombat = 13;

        [SerializeField]
        private int inventoryCellLabelFontSizeUltraFocus = 11;

        private float cueBannerTimer;
        private float overheatSpikePulseTimer;
        private float heatStatePulseTimer;
        private float ventReadyPulseTimer;
        private float eventResolvePanelPulseTimer;
        private float comboBurstButtonPulseTimer;
        private float nextWaveButtonPulseTimer;
        private float progressionUnlockPanelPulseTimer;
        private float synergyChipPulseTimer;
        private string lastActivatedRecipeName = string.Empty;
        private Color heatStatePulseColor = Color.white;
        private bool suppressNextEventResolvedCue;
        private bool suppressNextPlacementResolvedCue;
        private float safeChoicePulseTimer;
        private float safeChoiceSweepTimer;
        private float pendingPlacementFeedbackAmplitude = 0.12f;
        private float pendingPlacementFeedbackDurationRuntime = -1f;
        private float pendingTokenPulseAmplitude = 0.11f;
        private float pendingTokenPulseDurationRuntime = -1f;
        private float placementImpactDurationRuntime = -1f;
        private float placementImpactAlphaScale = 1f;
        private float placementFailShakeDurationRuntime = -1f;
        private float placementFailShakeDistanceRuntime = -1f;
        private float placementFailShakeTimer;
        private float placementFailShakeDirection = 1f;
        private float prototypeVfxTimer;
        private float prototypeVfxDurationRuntime = -1f;
        private float prototypeVfxSizeRuntime = -1f;
        private Color prototypeVfxTint = Color.white;
        private Vector2 inventoryGridHolderBaseAnchoredPosition;
        private bool inventoryGridHolderBaseAnchoredPositionCached;
        private bool inventoryLayoutInitialized;
        private float inventoryLayoutCorrectionLogTimer;
        private int inventoryLayoutCorrectionBurstCount;
        private float inventoryLayoutCorrectionBurstTimer;
        private readonly float[] drawChoicePickImpactTimers = new float[3];
        private readonly bool[] drawChoicePickImpactSafeFlags = new bool[3];
        private bool compactHudMode = true;
        private bool gameplayHudFocusApplied;
        private bool gameplayHudFocusInitialized;
        private bool gameplayHudLayoutContextInitialized;
        private bool gameplayHudPlacementContextApplied;
        private bool gameplayHudDragFocusApplied;
        private float ultraFocusHoldTimer;
        private bool minimalCombatStripActive;
        private bool minimalCombatStripLayoutApplied;
        private float minimalCombatStripEnterTimer;
        private float minimalCombatStripExitHoldTimer;
        private bool telemetryPanelExpanded;
        private bool telemetryWaveScope;
        private float inventoryGridCellSizeMinRuntime;
        private float inventoryGridCellSizeMaxRuntime;
        private int inventoryCellLabelFontSizeRuntime = -1;
        private int telemetryWaveBaseWave = -1;
        private int telemetryWaveBaseAttempts;
        private int telemetryWaveBaseSuccess;
        private int telemetryWaveBaseAutoMergeSuccess;
        private int telemetryWaveBaseBlockedOutOfBounds;
        private int telemetryWaveBaseBlockedOccupied;
        private int telemetryWaveBaseBlockedInvalidAnchor;
        private int telemetryWaveBaseBlockedNoPending;
        private int telemetryWaveBasePick1;
        private int telemetryWaveBasePick2;
        private int telemetryWaveBasePick3;
        private int telemetryWaveBaseManualMergeSuccess;

        private static readonly Color PanelDark = new Color(0.08f, 0.10f, 0.13f, 0.95f);
        private static readonly Color PanelMid = new Color(0.11f, 0.15f, 0.19f, 0.95f);
        private static readonly Color AccentRed = new Color(0.92f, 0.26f, 0.22f, 0.95f);
        private static readonly Color AccentOrange = new Color(0.95f, 0.54f, 0.20f, 0.95f);
        private static readonly Color AccentGreen = new Color(0.25f, 0.75f, 0.40f, 0.95f);
        private static readonly Color AccentBlue = new Color(0.24f, 0.56f, 0.88f, 0.95f);
        private static readonly Color HeatSafeColor = new Color(0.32f, 0.76f, 0.44f, 0.95f);
        private static readonly Color HeatWarningColor = new Color(0.96f, 0.63f, 0.22f, 0.95f);
        private static readonly Color HeatDangerColor = new Color(0.94f, 0.28f, 0.20f, 0.95f);
        private static readonly Color PendingTokenNeutralColor = new Color(0.15f, 0.19f, 0.26f, 0.98f);
        private static readonly Color PendingTokenValidColor = new Color(0.22f, 0.46f, 0.30f, 0.98f);
        private static readonly Color PendingTokenInvalidColor = new Color(0.52f, 0.20f, 0.20f, 0.98f);
        private static readonly Color PendingDragGhostOffColor = new Color(0.12f, 0.14f, 0.19f, 0.28f);
        private static readonly Color PendingGhostValidColor = new Color(0.33f, 0.92f, 0.48f, 0.88f);
        private static readonly Color PendingGhostInvalidColor = new Color(0.94f, 0.32f, 0.28f, 0.84f);

        private void Awake()
        {
            FoodTruckPrototypeHud[] existing = FindObjectsByType<FoodTruckPrototypeHud>(FindObjectsSortMode.None);
            if (existing.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            model = new FoodTruckRunModel();
            model.StateChanged += RefreshAll;
            model.CombatLogAppended += AppendLog;
            model.PresentationTriggered += HandlePresentationTrigger;
        }

        private void Start()
        {
            uiFont = TryLoadBuiltinFont();
            if (uiFont == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Malgun Gothic", "Arial", "Noto Sans CJK KR", "Helvetica Neue" }, 16);
            }

            EnsureEventSystem();
            inventoryGridCellSizeMinRuntime = Mathf.Clamp(inventoryCellMinSizeExpanded, 32f, 220f);
            inventoryGridCellSizeMaxRuntime = Mathf.Clamp(inventoryCellMaxSizeExpanded, inventoryGridCellSizeMinRuntime, 240f);
            inventoryCellLabelFontSizeRuntime = -1;
            BuildUi();
            ApplyInventoryCellLabelFontSize(inventoryCellLabelFontSizeExpanded);
            inventoryLayoutInitialized = false;
            ResetInventoryGridShakeState();
            StabilizeInventoryLayout();
            StartCoroutine(DeferredStabilizeInventoryLayout());
            EnsureRiskIconSprites();
            TryAutoLoadPrototypeArt();
            TryAutoLoadPrototypeVfx();
            TryAutoLoadPrototypeSfx();
            EnsureSfxAudioSource();
            model.ResetRun();
            ResetTelemetryRunTracking(true);
            CaptureTelemetryWaveBaseline();
            previousTruckHp = model.TruckHp;
            previousCanVentHeatState = model.CanVentHeat;
            previousCanActivateBurstState = model.CanActivateComboBurst;
            previousNextWaveReadyState = CanTriggerNextWaveNow();
        }

        private void Update()
        {
            if (model == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            flowChecklistLogCooldownTimer = Mathf.Max(0f, flowChecklistLogCooldownTimer - dt);
            inventoryLayoutCorrectionLogTimer = Mathf.Max(0f, inventoryLayoutCorrectionLogTimer - dt);
            if (inventoryLayoutCorrectionBurstTimer > 0f)
            {
                inventoryLayoutCorrectionBurstTimer = Mathf.Max(0f, inventoryLayoutCorrectionBurstTimer - dt);
                if (inventoryLayoutCorrectionBurstTimer <= 0f)
                {
                    inventoryLayoutCorrectionBurstCount = 0;
                }
            }
            model.Tick(dt);
            UpdateTelemetryAutomation(dt);
            UpdateEnemyAfterVisuals(dt);
            UpdateEnemyHitEffects(dt);
            UpdateLaneHitFlashVisuals(dt);

            HandleKeyboardShortcuts();
            HandlePendingDrag();
            UpdateUltraFocusHold(dt);
            HandleGridMergeClick();
            RefreshGameplayHudContextImmediate();
            UpdatePendingPlacementFeedback(dt);
            UpdatePlacementImpact(dt);
            UpdatePrototypeVfx(dt);
            UpdatePlacementFailShake(dt);
            UpdateRecommendationPulse(dt);
            UpdateFlowChecklist();
            UpdateDrawChoiceTutorialPulse(dt);
            UpdateDrawChoicePickImpact(dt);
            UpdateCueBanner(dt);
            UpdateHeatMeterPulse(dt);
            UpdateVentButtonPulse(dt);
            UpdateEventPanelPulse(dt);
            UpdateComboBurstPulse(dt);
            UpdateNextWaveButtonPulse(dt);
            UpdateProgressionUnlockPulse(dt);
            UpdateSynergyChipPulse(dt);
        }

        private void LateUpdate()
        {
            NormalizeInventoryGridTransformIfNeeded();
            EnsureInventoryGridIdleAlignment();
        }

        private void NormalizeInventoryGridTransformIfNeeded()
        {
            if (!inventoryLayoutInitialized)
            {
                return;
            }

            bool transformChanged = false;
            float transformEpsilonSqr = Mathf.Max(1e-8f, inventoryLayoutTransformEpsilon * inventoryLayoutTransformEpsilon);
            float rotationEpsilonDeg = Mathf.Max(0.001f, inventoryLayoutRotationEpsilonDeg);
            float anchorEpsilonSqr = GetInventoryLayoutAnchorEpsilonSqr();
            float positionEpsilonSqr = GetInventoryLayoutPositionEpsilonSqr();

            if (inventoryGridShakeRoot != null)
            {
                Vector2 desiredRootAnchor = new Vector2(0f, 1f);
                if ((inventoryGridShakeRoot.anchorMin - desiredRootAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridShakeRoot.anchorMin = desiredRootAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridShakeRoot.anchorMax - desiredRootAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridShakeRoot.anchorMax = desiredRootAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridShakeRoot.pivot - desiredRootAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridShakeRoot.pivot = desiredRootAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridShakeRoot.localScale - Vector3.one).sqrMagnitude > transformEpsilonSqr)
                {
                    inventoryGridShakeRoot.localScale = Vector3.one;
                    transformChanged = true;
                }

                if (Quaternion.Angle(inventoryGridShakeRoot.localRotation, Quaternion.identity) > rotationEpsilonDeg)
                {
                    inventoryGridShakeRoot.localRotation = Quaternion.identity;
                    transformChanged = true;
                }
            }

            if (inventoryGridHolder != null)
            {
                Vector2 desiredAnchor = new Vector2(0f, 1f);
                if ((inventoryGridHolder.anchorMin - desiredAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridHolder.anchorMin = desiredAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridHolder.anchorMax - desiredAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridHolder.anchorMax = desiredAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridHolder.pivot - desiredAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridHolder.pivot = desiredAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridHolder.localScale - Vector3.one).sqrMagnitude > transformEpsilonSqr)
                {
                    inventoryGridHolder.localScale = Vector3.one;
                    transformChanged = true;
                }

                if (Quaternion.Angle(inventoryGridHolder.localRotation, Quaternion.identity) > rotationEpsilonDeg)
                {
                    inventoryGridHolder.localRotation = Quaternion.identity;
                    transformChanged = true;
                }

                Vector2 holderRest = ResolveInventoryGridRestAnchoredPosition(inventoryGridHolder);
                if ((inventoryGridHolder.anchoredPosition - holderRest).sqrMagnitude > positionEpsilonSqr)
                {
                    inventoryGridHolder.anchoredPosition = holderRest;
                    transformChanged = true;
                }
            }

            if (inventoryGridShakeLayer != null)
            {
                Vector2 desiredShakeAnchor = new Vector2(0f, 1f);
                if ((inventoryGridShakeLayer.anchorMin - desiredShakeAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridShakeLayer.anchorMin = desiredShakeAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridShakeLayer.anchorMax - desiredShakeAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridShakeLayer.anchorMax = desiredShakeAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridShakeLayer.pivot - desiredShakeAnchor).sqrMagnitude > anchorEpsilonSqr)
                {
                    inventoryGridShakeLayer.pivot = desiredShakeAnchor;
                    transformChanged = true;
                }

                if ((inventoryGridShakeLayer.localScale - Vector3.one).sqrMagnitude > transformEpsilonSqr)
                {
                    inventoryGridShakeLayer.localScale = Vector3.one;
                    transformChanged = true;
                }

                if (Quaternion.Angle(inventoryGridShakeLayer.localRotation, Quaternion.identity) > rotationEpsilonDeg)
                {
                    inventoryGridShakeLayer.localRotation = Quaternion.identity;
                    transformChanged = true;
                }

                if (placementFailShakeTimer <= 0f)
                {
                    Vector2 rest = ResolveInventoryGridRestAnchoredPosition(inventoryGridShakeLayer);
                    if ((inventoryGridShakeLayer.anchoredPosition - rest).sqrMagnitude > positionEpsilonSqr)
                    {
                        inventoryGridShakeLayer.anchoredPosition = rest;
                        transformChanged = true;
                    }
                }
            }

            if (transformChanged)
            {
                CacheInventoryGridBaseAnchoredPosition();
                RegisterInventoryLayoutCorrectionBurst();

                if (inventoryLayoutCorrectionDiagnostics && inventoryLayoutCorrectionLogTimer <= 0f)
                {
                    string holderState = inventoryGridHolder == null
                        ? "holder=null"
                        : "holder ap=" + inventoryGridHolder.anchoredPosition +
                          " anchorMin=" + inventoryGridHolder.anchorMin +
                          " anchorMax=" + inventoryGridHolder.anchorMax +
                          " pivot=" + inventoryGridHolder.pivot;
                    string rootState = inventoryGridShakeRoot == null
                        ? "root=null"
                        : "root scale=" + inventoryGridShakeRoot.localScale +
                          " rot=" + inventoryGridShakeRoot.localRotation.eulerAngles;
                    string shakeState = inventoryGridShakeLayer == null
                        ? "shake=null"
                        : "shake ap=" + inventoryGridShakeLayer.anchoredPosition +
                          " anchorMin=" + inventoryGridShakeLayer.anchorMin +
                          " anchorMax=" + inventoryGridShakeLayer.anchorMax +
                          " pivot=" + inventoryGridShakeLayer.pivot;
                    Debug.Log("[FoodTruckHUD] Inventory layout normalized: " + holderState + " | " + shakeState + " | " + rootState);
                    inventoryLayoutCorrectionLogTimer = Mathf.Max(0.1f, inventoryLayoutCorrectionLogCooldown);
                }
            }
        }

        private void RegisterInventoryLayoutCorrectionBurst()
        {
            float burstWindow = Mathf.Max(0.2f, inventoryLayoutCorrectionBurstWindow);
            int burstThreshold = Mathf.Max(2, inventoryLayoutCorrectionBurstThreshold);

            if (inventoryLayoutCorrectionBurstTimer <= 0f)
            {
                inventoryLayoutCorrectionBurstCount = 0;
            }

            inventoryLayoutCorrectionBurstCount++;
            inventoryLayoutCorrectionBurstTimer = burstWindow;

            if (inventoryLayoutCorrectionBurstCount < burstThreshold)
            {
                return;
            }

            inventoryLayoutCorrectionBurstCount = 0;
            inventoryLayoutCorrectionBurstTimer = 0f;
            StabilizeInventoryLayout();

            if (inventoryLayoutCorrectionDiagnostics)
            {
                Debug.LogWarning("[FoodTruckHUD] Inventory layout correction burst detected. Forced stabilization pass.");
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!inventoryLayoutInitialized)
            {
                return;
            }

            ApplyPanelLayout();
            UpdateInventoryGridSizing();
            CacheInventoryGridBaseAnchoredPosition();
        }

        private void OnDestroy()
        {
            if (model == null)
            {
                ReleaseRiskIconSprites();
                return;
            }

            model.StateChanged -= RefreshAll;
            model.CombatLogAppended -= AppendLog;
            model.PresentationTriggered -= HandlePresentationTrigger;
            ReleaseRiskIconSprites();
        }

        private void OnApplicationQuit()
        {
            if (autoExportTelemetryOnQuit)
            {
                ExportTelemetryReportFromHud("application_quit", false);
            }
        }

        private IEnumerator DeferredStabilizeInventoryLayout()
        {
            // Startup layout can settle over multiple frames (CanvasScaler, dynamic font metrics, etc).
            // Re-run stabilization a few times to prevent late frame drift in the inventory grid origin.
            const int startupStabilizePasses = 3;
            for (int i = 0; i < startupStabilizePasses; i++)
            {
                yield return null;
                StabilizeInventoryLayout();
            }
        }

        private RectTransform GetInventoryGridShakeTarget()
        {
            if (inventoryGridShakeLayer != null)
            {
                return inventoryGridShakeLayer;
            }

            return inventoryGridHolder != null ? inventoryGridHolder : inventoryGridShakeRoot;
        }

        private Vector2 ResolveInventoryGridRestAnchoredPosition(RectTransform shakeTarget)
        {
            if (shakeTarget == null)
            {
                return Vector2.zero;
            }

            // GridContent is anchored top-left in GridHolder, so its stable idle position is always zero.
            if (inventoryGridHolder != null && shakeTarget == inventoryGridHolder)
            {
                return Vector2.zero;
            }

            // The shake layer is the square board inside a potentially wide holder; keep it centered while idle.
            if (inventoryGridShakeLayer != null && shakeTarget == inventoryGridShakeLayer)
            {
                RectTransform parent = inventoryGridShakeRoot != null
                    ? inventoryGridShakeRoot
                    : inventoryGridShakeLayer.parent as RectTransform;
                if (parent != null)
                {
                    float availableWidth = parent.rect.width;
                    float boardWidth = inventoryGridShakeLayer.rect.width;
                    if (availableWidth > 1f && boardWidth > 1f)
                    {
                        return new Vector2(Mathf.Max(0f, (availableWidth - boardWidth) * 0.5f), 0f);
                    }
                }

                return Vector2.zero;
            }

            return shakeTarget.anchoredPosition;
        }

        private float GetInventoryLayoutAnchorEpsilonSqr()
        {
            float epsilon = Mathf.Max(0.00001f, inventoryLayoutAnchorEpsilon);
            return epsilon * epsilon;
        }

        private float GetInventoryLayoutPositionEpsilonSqr()
        {
            float epsilon = Mathf.Max(0.001f, inventoryLayoutPositionEpsilon);
            return epsilon * epsilon;
        }

        private void CacheInventoryGridBaseAnchoredPosition()
        {
            if (placementFailShakeTimer > 0f)
            {
                return;
            }

            RectTransform shakeTarget = GetInventoryGridShakeTarget();
            if (shakeTarget == null)
            {
                return;
            }

            inventoryGridHolderBaseAnchoredPosition = ResolveInventoryGridRestAnchoredPosition(shakeTarget);
            inventoryGridHolderBaseAnchoredPositionCached = true;
        }

        private void ResetInventoryGridShakeState()
        {
            placementFailShakeTimer = 0f;

            RectTransform shakeTarget = GetInventoryGridShakeTarget();
            if (shakeTarget == null)
            {
                inventoryGridHolderBaseAnchoredPosition = Vector2.zero;
                inventoryGridHolderBaseAnchoredPositionCached = false;
                return;
            }

            inventoryGridHolderBaseAnchoredPosition = ResolveInventoryGridRestAnchoredPosition(shakeTarget);
            shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
            inventoryGridHolderBaseAnchoredPositionCached = true;
        }

        private void EnsureInventoryGridIdleAlignment()
        {
            if (!inventoryLayoutInitialized)
            {
                return;
            }

            if (placementFailShakeTimer > 0f)
            {
                return;
            }

            RectTransform shakeTarget = GetInventoryGridShakeTarget();
            if (shakeTarget == null)
            {
                return;
            }

            if (!inventoryGridHolderBaseAnchoredPositionCached)
            {
                inventoryGridHolderBaseAnchoredPosition = ResolveInventoryGridRestAnchoredPosition(shakeTarget);
                inventoryGridHolderBaseAnchoredPositionCached = true;
                if ((shakeTarget.anchoredPosition - inventoryGridHolderBaseAnchoredPosition).sqrMagnitude > GetInventoryLayoutPositionEpsilonSqr())
                {
                    shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
                }
                return;
            }

            Vector2 delta = shakeTarget.anchoredPosition - inventoryGridHolderBaseAnchoredPosition;
            if (delta.sqrMagnitude <= GetInventoryLayoutPositionEpsilonSqr())
            {
                return;
            }

            shakeTarget.anchoredPosition = inventoryGridHolderBaseAnchoredPosition;
        }

        private bool AttemptChooseDrawOptionFromHud(int slotIndex)
        {
            if (model == null || !model.HasDrawChoice)
            {
                return false;
            }

            IReadOnlyList<PendingBlockState> choices = model.DrawChoices;
            if (slotIndex < 0 || slotIndex >= choices.Count)
            {
                return false;
            }

            PendingBlockState selectedChoice = choices[slotIndex];
            string valueBucket = GetDrawChoiceValueBucket(selectedChoice);
            string riskTag = GetDrawChoiceRiskTag(selectedChoice);
            bool safePick = string.Equals(model.GetDrawChoiceAssistTag(slotIndex), "SAFE", StringComparison.Ordinal);
            bool chosen = model.ChooseDrawOption(slotIndex);
            if (!chosen)
            {
                return false;
            }

            RegisterTelemetryDrawPick(valueBucket, riskTag);
            TriggerDrawChoicePickImpact(slotIndex, safePick);
            ShowCueBanner(
                safePick ? "SAFE choice locked. Place it quickly for stability." : "Power pick locked. Manage heat and timing.",
                safePick ? new Color(0.32f, 0.80f, 0.44f, 1f) : new Color(0.95f, 0.56f, 0.24f, 1f));
            PlaySfx(sfxSelectClip, safePick ? 0.95f : 1f);
            return true;
        }

        private void TriggerDrawChoicePickImpact(int slotIndex, bool safePick)
        {
            if (slotIndex < 0 || slotIndex >= drawChoicePickImpactTimers.Length)
            {
                return;
            }

            drawChoicePickImpactTimers[slotIndex] = Mathf.Max(0.08f, drawChoicePickImpactDuration);
            drawChoicePickImpactSafeFlags[slotIndex] = safePick;
        }

        private void AppendLog(string message)
        {
            recentLogs.Enqueue(message);
            while (recentLogs.Count > 6)
            {
                recentLogs.Dequeue();
            }

            if (logText != null)
            {
                logText.text = "Log\n" + string.Join("\n", recentLogs.ToArray());
            }
        }

        private void RefreshAll()
        {
            if (model == null || titleText == null)
            {
                return;
            }

            bool truckDamagedThisFrame = previousTruckHp >= 0f && model.TruckHp + 0.01f < previousTruckHp;
            bool focusPlacementMode = model.HasPendingBlock && (isDraggingPending || pendingHoverAnchorCell >= 0);
            bool gameplayFocusHud = IsGameplayFocusHudActive();
            ApplyGameplayHudContext(focusPlacementMode, gameplayFocusHud);

            string heatTag = model.IsOverheated ? "HOT" : (model.IsHeatWarning ? "WARM" : string.Empty);
            topLineText.text =
                "Wave " + model.Wave + "  |  HP " + Mathf.CeilToInt(model.TruckHp) + "/" + Mathf.CeilToInt(model.MaxTruckHp) +
                "  |  Supplies " + model.Supplies + "  |  Fuel " + model.Fuel +
                (string.IsNullOrEmpty(heatTag) ? string.Empty : "  |  " + heatTag);

            string phase = model.IsRestPhase ? "REST" : "COMBAT";
            string heatState = model.IsOverheated
                ? "OVERHEAT " + Mathf.CeilToInt(model.OverheatSeverity01 * 100f) + "%"
                : (model.IsHeatWarning ? "warning" : "stable");
            string ventState = BuildVentStatusText(false);
            if (gameplayFocusHud)
            {
                secondLineText.text =
                    phase +
                    "  |  Threat " + model.Threat.ToString("0.0") +
                    "  |  Heat " + model.Heat.ToString("0.0") + " (" + heatState + ")" +
                    "  |  Combo x" + model.ComboMultiplier.ToString("0.00") +
                    "  |  " + ventState;
            }
            else
            {
                secondLineText.text =
                    "Weather " + model.Weather + "  |  Threat " + model.Threat.ToString("0.0") +
                    "  |  Heat " + model.Heat.ToString("0.0") + " (" + heatState + ")" +
                    "  |  " + ventState +
                    "  |  Momentum " + model.Momentum.ToString("0.0") +
                    "  |  Combo " + model.ComboStreak + " (x" + model.ComboMultiplier.ToString("0.00") + ")" +
                    "  |  Heat A/L/R " + model.HeatAttackMultiplier.ToString("0.00") + "/" + model.HeatLootMultiplier.ToString("0.00") + "/" + model.HeatRiskMultiplier.ToString("0.00") +
                    "  |  " + phase;
            }

            string pendingStatus = model.HasDrawChoice
                ? "choose 1/3"
                : (model.HasPendingBlock ? model.PendingBlock.Label + " @" + model.PendingRotationDegrees + "deg" : "none");
            if (inventoryTitleText != null)
            {
                if (model.HasDrawChoice)
                {
                    inventoryTitleText.text = "Build Flow: Pick 1 block, then place it on the 3x3 board";
                }
                else if (model.HasPendingBlock)
                {
                    inventoryTitleText.text = "Build Flow: Place the pending block on the 3x3 board";
                }
                else
                {
                    inventoryTitleText.text = "Build Flow: Draw a block to start placement";
                }
            }

            string keyboardHint = Application.isMobilePlatform
                ? (model.HasDrawChoice ? "Tap 1/2/3" : "Tap/Drag/Buttons")
                : (model.HasDrawChoice ? "1/2/3 pick | K export | R reset" : "D/S/Q/E/F/T/C/Space/M/N/K/R");
            string ventMiniState = BuildVentStatusText(true);
            string burstMiniState = BuildBurstStatusText(true);
            string drawAssistTag = model.DrawAssistTag;
            if (focusPlacementMode)
            {
                UpdatePlacementRecommendations();
                string recHint = BuildPlacementRecommendationHint();
                string hoverState;
                if (pendingHoverAnchorCell >= 0)
                {
                    string blockedReason = !pendingHoverValid ? BuildPlacementBlockedHint() : string.Empty;
                    hoverState = "Slot " + (pendingHoverAnchorCell + 1) + (pendingHoverValid ? " READY" : " BLOCKED");
                    if (!string.IsNullOrEmpty(blockedReason))
                    {
                        hoverState += " (" + blockedReason + ")";
                    }
                }
                else
                {
                    hoverState = "Drag to a valid slot";
                }

                string rotateHint = Application.isMobilePlatform ? "ROT L/R" : "Q/E";
                combatLineText.text =
                    "FOCUS PLACE  |  " + model.PendingBlock.Label + " @" + model.PendingRotationDegrees + "deg" +
                    "  |  " + hoverState +
                    "  |  " + recHint +
                    "  |  Rotate " + rotateHint +
                    "  |  Vent " + ventMiniState +
                    "  |  Burst " + burstMiniState;
            }
            else
            {
                string comboTimer = model.ComboStreak > 0 ? "   ComboTimer: " + model.ComboTimerRemaining.ToString("0") + "s" : string.Empty;
                if (gameplayFocusHud)
                {
                    if (minimalCombatStripActive)
                    {
                        string comboMini = model.ComboStreak > 0
                            ? ("  C" + model.ComboStreak + "/" + model.ComboBurstRequiredStreakValue + " " + model.ComboTimerRemaining.ToString("0") + "s")
                            : string.Empty;
                        combatLineText.text =
                            "W" + model.Wave +
                            "  |  HP " + Mathf.CeilToInt(model.TruckHp) +
                            "  |  Heat " + model.Heat.ToString("0.0") +
                            "  |  V:" + ventMiniState +
                            "  |  B:" + burstMiniState +
                            comboMini;
                    }
                    else
                    {
                        combatLineText.text =
                            "Draw " + model.GetDrawCost() +
                            "  |  Assist " + drawAssistTag +
                            "  |  Pending " + pendingStatus +
                            "  |  Vent " + ventMiniState +
                            "  |  Burst " + burstMiniState +
                            comboTimer;
                    }
                }
                else
                {
                    string telemetryMini = BuildTelemetryMiniLine();
                    string recommendationMini = BuildRecommendationMiniLine();
                    combatLineText.text =
                        "Draw Cost: " + model.GetDrawCost() +
                        "   Assist: " + drawAssistTag +
                        "   Pending: " + pendingStatus +
                        "   Keyboard: " + keyboardHint +
                        "   Vent: " + ventMiniState +
                        "   Burst: " + burstMiniState +
                        comboTimer +
                        telemetryMini +
                        recommendationMini;
                }
            }

            topLineText.color = model.IsOverheated
                ? new Color(1f, 0.86f, 0.52f, 1f)
                : (model.IsHeatWarning ? new Color(0.99f, 0.84f, 0.56f, 1f) : Color.white);
            Color lineColor = model.IsOverheated
                ? new Color(1f, 0.66f, 0.34f, 1f)
                : (model.IsHeatWarning ? new Color(0.98f, 0.79f, 0.45f, 1f) : new Color(0.95f, 0.96f, 0.99f, 1f));
            secondLineText.color = lineColor;
            if (focusPlacementMode)
            {
                combatLineText.color = pendingHoverValid
                    ? new Color(0.82f, 0.98f, 0.86f, 1f)
                    : new Color(1f, 0.84f, 0.70f, 1f);
            }
            else
            {
                combatLineText.color = model.IsOverheated
                    ? new Color(1f, 0.82f, 0.46f, 1f)
                    : (model.IsHeatWarning ? new Color(1f, 0.88f, 0.58f, 1f) : new Color(0.85f, 0.94f, 1f, 1f));
            }

            SetMeterValue(hpMeter, model.TruckHp / model.MaxTruckHp,
                Mathf.CeilToInt(model.TruckHp) + " / " + Mathf.CeilToInt(model.MaxTruckHp));
            SetMeterValue(waveMeter, model.GetWaveProgress01(), (model.GetWaveProgress01() * 100f).ToString("0") + "%");
            RefreshHeatMeter();
            SetMeterValue(momentumMeter, model.Momentum / 100f, model.Momentum.ToString("0.0"));

            if (!isDraggingPending)
            {
                PaintInventoryCells(-1, false);
            }

            for (int lane = 0; lane < laneTexts.Length; lane++)
            {
                laneTexts[lane].text =
                    "Lane " + (lane + 1) +
                    "  z:" + model.GetLaneEnemyCount(lane) +
                    "  p:" + model.GetLanePressure(lane).ToString("0.0");
            }

            RefreshEnemyVisuals(truckDamagedThisFrame);
            logText.text = "Log\n" + string.Join("\n", recentLogs.ToArray());
            RefreshSynergyChips();
            RefreshEventPanel();
            RefreshDrawChoiceRow();
            RefreshSpeedButtons();
            RefreshTelemetryPanel();

            if (model.HasPendingBlock && pendingHoverAnchorCell >= 0)
            {
                RefreshPendingHintText(pendingHoverAnchorCell, pendingHoverValid);
            }
            else
            {
                RefreshPendingHintText();
            }

            string cueMessage = null;
            Color cueColor = Color.white;

            if (model.Wave != previousWaveDisplay && previousWaveDisplay > 0)
            {
                cueMessage = !string.IsNullOrEmpty(model.LastWaveOutcomeCue)
                    ? model.LastWaveOutcomeCue
                    : "Wave " + model.Wave + " started. Keep your lanes stable.";
                cueColor = new Color(0.30f, 0.70f, 0.95f, 1f);
            }
            else if (model.EventPending && !previousEventPendingState)
            {
                cueMessage = "Event triggered. Choose Option 1/2/3.";
                cueColor = new Color(0.94f, 0.72f, 0.28f, 1f);
            }
            else if (!model.EventPending && previousEventPendingState && !suppressNextEventResolvedCue)
            {
                cueMessage = "Event resolved. Return to your build flow.";
                cueColor = new Color(0.42f, 0.76f, 0.96f, 1f);
            }
            else if (model.HasDrawChoice && !previousHasDrawChoiceState)
            {
                int safeIndex = FindSafeDrawChoiceIndex();
                if (safeIndex >= 0 && model.Wave <= 3)
                {
                    cueMessage = "Draw ready: SAFE choice is slot " + (safeIndex + 1) + ".";
                    cueColor = new Color(0.32f, 0.80f, 0.50f, 1f);
                }
                else
                {
                    cueMessage = "Draw ready: pick 1 of 3 blocks.";
                    cueColor = new Color(0.28f, 0.70f, 0.95f, 1f);
                }
            }
            else if (model.HasPendingBlock && !previousHasPendingBlockState)
            {
                UpdatePlacementRecommendations();
                cueMessage = "Block selected. " + BuildPlacementRecommendationHint() + ".";
                cueColor = new Color(0.33f, 0.78f, 0.45f, 1f);
            }
            else if (!model.HasPendingBlock && previousHasPendingBlockState && !model.HasDrawChoice && !model.EventPending && !suppressNextPlacementResolvedCue)
            {
                cueMessage = "Placement resolved. Draw again or optimize your board.";
                cueColor = new Color(0.36f, 0.78f, 0.54f, 1f);
            }
            else if (model.IsRestPhase && !previousRestPhase)
            {
                cueMessage = "Rest phase: arrange blocks before the next wave.";
                cueColor = new Color(0.35f, 0.70f, 0.95f, 1f);
            }
            else if (!model.IsRestPhase && previousRestPhase)
            {
                cueMessage = "Combat phase started. Manage heat and combos.";
                cueColor = new Color(0.95f, 0.44f, 0.30f, 1f);
            }

            if (!string.IsNullOrEmpty(cueMessage))
            {
                ShowCueBanner(cueMessage, cueColor);
            }

            suppressNextEventResolvedCue = false;
            suppressNextPlacementResolvedCue = false;

            UpdateFlowChecklist();

            previousWaveDisplay = model.Wave;
            previousRestPhase = model.IsRestPhase;
            previousHasDrawChoiceState = model.HasDrawChoice;
            previousHasPendingBlockState = model.HasPendingBlock;
            previousEventPendingState = model.EventPending;
            previousTruckHp = model.TruckHp;
        }

        private bool IsGameplayFocusHudActive()
        {
            if (!autoGameplayFocusHud || model == null)
            {
                return false;
            }

            if (telemetryPanelExpanded)
            {
                return false;
            }

            return !model.EventPending && !model.HasDrawChoice;
        }

        private void UpdateUltraFocusHold(float dt)
        {
            bool requestUltraFocus = isDraggingPending || pendingHoverAnchorCell >= 0;
            if (requestUltraFocus)
            {
                ultraFocusHoldTimer = Mathf.Max(ultraFocusHoldTimer, Mathf.Max(0f, ultraFocusReleaseHoldSeconds));
                return;
            }

            if (ultraFocusHoldTimer > 0f)
            {
                ultraFocusHoldTimer = Mathf.Max(0f, ultraFocusHoldTimer - Mathf.Max(0f, dt));
            }
        }

        private bool IsUltraFocusActive()
        {
            return isDraggingPending || pendingHoverAnchorCell >= 0 || ultraFocusHoldTimer > 0f;
        }

        private bool UpdateMinimalCombatStripState(bool combatOnlyFocus, int totalLaneEnemies, float maxLanePressure)
        {
            if (!minimalCombatStripEnabled || !combatOnlyFocus || model == null)
            {
                minimalCombatStripEnterTimer = 0f;
                minimalCombatStripExitHoldTimer = 0f;
                minimalCombatStripActive = false;
                return false;
            }

            float pressureEnterThreshold = Mathf.Max(0f, minimalCombatStripLanePressureEnterThreshold);
            float pressureExitThreshold = Mathf.Max(pressureEnterThreshold, minimalCombatStripLanePressureExitThreshold);
            bool pressureEligible = minimalCombatStripActive
                ? maxLanePressure <= pressureExitThreshold
                : maxLanePressure <= pressureEnterThreshold;

            bool eligible =
                totalLaneEnemies <= 0 &&
                pressureEligible &&
                !model.IsHeatWarning &&
                !model.IsOverheated &&
                !model.CanActivateComboBurst &&
                !model.CanVentHeat;

            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (eligible)
            {
                minimalCombatStripEnterTimer += dt;
                if (!minimalCombatStripActive)
                {
                    minimalCombatStripActive = minimalCombatStripEnterTimer >= Mathf.Max(0f, minimalCombatStripEnterDelaySeconds);
                }

                if (minimalCombatStripActive)
                {
                    minimalCombatStripExitHoldTimer = Mathf.Max(
                        minimalCombatStripExitHoldTimer,
                        Mathf.Max(0f, minimalCombatStripExitHoldSeconds));
                }

                return minimalCombatStripActive;
            }

            minimalCombatStripEnterTimer = 0f;
            if (!minimalCombatStripActive)
            {
                return false;
            }

            bool immediateExitRequested =
                minimalCombatStripImmediateExitOnThreat &&
                (totalLaneEnemies > 0
                 || model.IsHeatWarning
                 || model.IsOverheated
                 || model.CanActivateComboBurst
                 || model.CanVentHeat
                 || maxLanePressure > pressureExitThreshold);
            if (immediateExitRequested)
            {
                minimalCombatStripActive = false;
                minimalCombatStripExitHoldTimer = 0f;
                return false;
            }

            if (minimalCombatStripExitHoldTimer > 0f)
            {
                minimalCombatStripExitHoldTimer = Mathf.Max(0f, minimalCombatStripExitHoldTimer - dt);
                if (minimalCombatStripExitHoldTimer > 0f)
                {
                    return true;
                }
            }

            minimalCombatStripActive = false;
            minimalCombatStripExitHoldTimer = 0f;
            return false;
        }

        private void ApplyGameplayHudContext(bool focusPlacementMode, bool gameplayFocusHud)
        {
            bool hasPendingBlock = model != null && model.HasPendingBlock;
            bool hasDrawChoice = model != null && model.HasDrawChoice;
            bool hasPlacedBlock = model != null && HasPlacedBlockInGrid();
            bool hasPlacementContext = model != null && (hasPendingBlock || hasDrawChoice || model.IsRestPhase);
            bool combatOnlyFocus = gameplayFocusHud && !hasPlacementContext;
            bool dragLayout = gameplayFocusHud && hasPendingBlock && IsUltraFocusActive();
            int totalLaneEnemies = 0;
            float maxLanePressure = 0f;
            if (model != null)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    totalLaneEnemies += Mathf.Max(0, model.GetLaneEnemyCount(lane));
                    maxLanePressure = Mathf.Max(maxLanePressure, model.GetLanePressure(lane));
                }
            }

            bool minimalCombatStrip = UpdateMinimalCombatStripState(combatOnlyFocus, totalLaneEnemies, maxLanePressure);
            bool minimalLayoutChanged = minimalCombatStripLayoutApplied != minimalCombatStrip;
            if (minimalLayoutChanged)
            {
                minimalCombatStripLayoutApplied = minimalCombatStrip;
            }

            bool focusModeChanged = !gameplayHudFocusInitialized || gameplayHudFocusApplied != gameplayFocusHud;
            bool layoutContextChanged = !gameplayHudLayoutContextInitialized
                || gameplayHudPlacementContextApplied != hasPlacementContext
                || gameplayHudDragFocusApplied != dragLayout;
            if (layoutContextChanged)
            {
                gameplayHudLayoutContextInitialized = true;
                gameplayHudPlacementContextApplied = hasPlacementContext;
                gameplayHudDragFocusApplied = dragLayout;
            }

            if (focusModeChanged || minimalLayoutChanged || layoutContextChanged)
            {
                gameplayHudFocusApplied = gameplayFocusHud;
                gameplayHudFocusInitialized = true;
                ApplyPanelLayout();
                UpdateInventoryGridSizing();
                if (focusModeChanged)
                {
                    ResetInventoryGridShakeState();
                }
            }

            bool drawChoiceFocus = gameplayFocusHud && hasDrawChoice;
            float laneHeight = gameplayFocusHud
                ? (focusPlacementMode ? 220f : (drawChoiceFocus ? 238f : (hasPlacementContext ? 300f : 480f)))
                : 196f;
            float meterHeight = gameplayFocusHud
                ? (drawChoiceFocus ? 0f : (hasPlacementContext ? 86f : 58f))
                : 110f;
            float inventoryPreferredHeight = gameplayFocusHud ? 236f : 470f;
            float inventoryMinHeight = gameplayFocusHud ? 190f : 350f;
            if (gameplayFocusHud && hasDrawChoice)
            {
                inventoryPreferredHeight = hasPlacedBlock ? 160f : 164f;
                inventoryMinHeight = hasPlacedBlock ? 132f : 136f;
            }
            else if (gameplayFocusHud && focusPlacementMode)
            {
                inventoryPreferredHeight = 326f;
                inventoryMinHeight = 250f;
            }
            else if (gameplayFocusHud && hasPendingBlock)
            {
                inventoryPreferredHeight = 286f;
                inventoryMinHeight = 226f;
            }
            bool draggingPlacementFocus = gameplayFocusHud && IsUltraFocusActive();
            if (draggingPlacementFocus)
            {
                laneHeight = 0f;
                meterHeight = 0f;
                inventoryPreferredHeight = 310f;
                inventoryMinHeight = 236f;
            }
            float pendingRowHeight = gameplayFocusHud ? (focusPlacementMode ? 64f : (hasDrawChoice ? 34f : (hasPendingBlock ? 54f : 44f))) : 58f;
            if (draggingPlacementFocus)
            {
                pendingRowHeight = 38f;
            }
            float actionsRow1Height = draggingPlacementFocus ? 0f : (gameplayFocusHud ? (combatOnlyFocus ? 44f : 52f) : 48f);
            float actionsRow2Height = draggingPlacementFocus ? 0f : (gameplayFocusHud ? 50f : 48f);
            GameplayActionVisibility actionVisibility = CalculateGameplayActionVisibility(
                gameplayFocusHud,
                combatOnlyFocus,
                hasPlacementContext,
                hasDrawChoice,
                model != null && model.EventPending,
                model != null && model.CanVentHeat,
                model != null && model.CanActivateComboBurst,
                model != null && model.IsOverheated);
            bool showPrimaryActions = actionVisibility.ShowPrimaryActionRow;
            bool showSecondaryCombatActions = actionVisibility.ShowSecondaryActionRow;
            bool showPendingRow = hasPendingBlock || hasDrawChoice;
            bool showInventoryGrid = actionVisibility.ShowInventoryGrid;
            bool showBottomPanel = showPendingRow || showInventoryGrid || actionVisibility.ShowsAnyActionRow;
            float laneRowHeight = gameplayFocusHud ? (focusPlacementMode ? 64f : (hasPlacementContext ? 86f : 136f)) : 60f;
            float laneRowMinHeight = gameplayFocusHud ? (focusPlacementMode ? 54f : (hasPlacementContext ? 72f : 112f)) : 56f;

            VerticalLayoutGroup topLayout = topPanelRect != null ? topPanelRect.GetComponent<VerticalLayoutGroup>() : null;
            if (topLayout != null)
            {
                topLayout.padding = drawChoiceFocus
                    ? new RectOffset(10, 10, 8, 6)
                    : new RectOffset(14, 14, 14, 14);
                topLayout.spacing = drawChoiceFocus ? 3f : 5f;
            }

            VerticalLayoutGroup bottomLayout = bottomPanelRect != null ? bottomPanelRect.GetComponent<VerticalLayoutGroup>() : null;
            if (bottomLayout != null)
            {
                bottomLayout.padding = gameplayFocusHud
                    ? new RectOffset(10, 10, 7, 8)
                    : new RectOffset(14, 14, 12, 12);
                bottomLayout.spacing = gameplayFocusHud ? 4f : 6f;
            }

            if (inventoryTitleText != null)
            {
                inventoryTitleText.fontSize = gameplayFocusHud ? 11 : 20;
                LayoutElement titleLayout = inventoryTitleText.GetComponent<LayoutElement>();
                if (titleLayout != null)
                {
                    titleLayout.preferredHeight = gameplayFocusHud ? 18f : 30f;
                }
            }

            if (laneContainerLayoutElement != null)
            {
                laneContainerLayoutElement.preferredHeight = laneHeight;
            }

            for (int i = 0; i < laneRowLayoutElements.Length; i++)
            {
                LayoutElement laneRowLayout = laneRowLayoutElements[i];
                if (laneRowLayout == null)
                {
                    continue;
                }

                laneRowLayout.preferredHeight = laneRowHeight;
                laneRowLayout.minHeight = laneRowMinHeight;
            }

            int laneFontSize = gameplayFocusHud
                ? (focusPlacementMode ? 12 : (hasPlacementContext ? 13 : 15))
                : 14;
            for (int i = 0; i < laneTexts.Length; i++)
            {
                if (laneTexts[i] != null)
                {
                    laneTexts[i].fontSize = laneFontSize;
                }
            }

            if (meterContainerLayoutElement != null)
            {
                meterContainerLayoutElement.preferredHeight = meterHeight;
            }

            bool showMeterRows = !draggingPlacementFocus && !drawChoiceFocus;
            bool showAllMeters = true;
            SetNodeVisible(hpMeter.RowRect, showMeterRows);
            SetNodeVisible(heatMeter.RowRect, showMeterRows);
            SetNodeVisible(waveMeter.RowRect, showMeterRows && showAllMeters);
            SetNodeVisible(momentumMeter.RowRect, showMeterRows && showAllMeters);

            int meterFontSize = gameplayFocusHud
                ? (draggingPlacementFocus ? 9 : (hasPlacementContext ? 10 : 9))
                : 12;
            if (hpMeter.ValueText != null) hpMeter.ValueText.fontSize = meterFontSize;
            if (waveMeter.ValueText != null) waveMeter.ValueText.fontSize = meterFontSize;
            if (heatMeter.ValueText != null) heatMeter.ValueText.fontSize = meterFontSize;
            if (momentumMeter.ValueText != null) momentumMeter.ValueText.fontSize = meterFontSize;

            if (inventoryGridLayoutElement != null)
            {
                inventoryGridLayoutElement.preferredHeight = inventoryPreferredHeight;
                inventoryGridLayoutElement.minHeight = inventoryMinHeight;
            }

            if (pendingRowLayoutElement != null)
            {
                pendingRowLayoutElement.preferredHeight = pendingRowHeight;
            }

            if (actionsRow1LayoutElement != null)
            {
                actionsRow1LayoutElement.preferredHeight = actionsRow1Height;
            }

            if (actionsRow2LayoutElement != null)
            {
                actionsRow2LayoutElement.preferredHeight = actionsRow2Height;
            }

            SetNodeVisible(topPanelRect, !draggingPlacementFocus);
            SetNodeVisible(bottomPanelRect, showBottomPanel);
            SetNodeVisible(midPanelRect, !gameplayFocusHud);
            SetNodeVisible(titleText, !gameplayFocusHud);
            SetNodeVisible(logText, !gameplayFocusHud);
            SetNodeVisible(mapControlRowRect, !gameplayFocusHud);
            SetNodeVisible(meterContainerRect, !draggingPlacementFocus);
            SetNodeVisible(laneContainerRect, !draggingPlacementFocus);
            SetNodeVisible(topLineText, !draggingPlacementFocus && !combatOnlyFocus && !drawChoiceFocus);
            SetNodeVisible(secondLineText, !draggingPlacementFocus && !combatOnlyFocus && !drawChoiceFocus);
            SetNodeVisible(combatLineText, !draggingPlacementFocus);
            SetNodeVisible(pendingHelpRect, showPendingRow && !draggingPlacementFocus && !hasDrawChoice);
            SetNodeVisible(flowChecklistPanelRect, !gameplayFocusHud);
            SetNodeVisible(telemetryControlRowRect, !gameplayFocusHud);
            SetNodeVisible(inventoryTitleText, showInventoryGrid || showPendingRow);
            SetNodeVisible(inventoryGridShakeRoot, showInventoryGrid);
            SetNodeVisible(pendingRowRect, showPendingRow);
            SetNodeVisible(actionsRow1Rect, !draggingPlacementFocus && showPrimaryActions);
            SetNodeVisible(actionsRow2Rect, !draggingPlacementFocus && showSecondaryCombatActions);
            SetNodeVisible(exportTelemetryButton, !gameplayFocusHud);
            SetNodeVisible(resetRunButton, !gameplayFocusHud);

            if (pendingHelpText != null)
            {
                pendingHelpText.fontSize = draggingPlacementFocus ? 11 : 14;
            }

            if (combatLineText != null)
            {
                combatLineText.fontSize = draggingPlacementFocus ? 12 : (minimalCombatStrip ? 11 : (combatOnlyFocus ? 12 : 14));
            }

            if (pendingHintText != null)
            {
                pendingHintText.fontSize = draggingPlacementFocus ? 13 : (gameplayFocusHud ? 15 : 17);
            }

            float tokenScale = draggingPlacementFocus
                ? Mathf.Clamp(pendingTokenUltraFocusScale, 0.60f, 1f)
                : 1f;
            pendingTokenBaseScale = Vector3.one * tokenScale;
            if (pendingTokenRect != null && pendingTokenPulseTimer <= 0f)
            {
                pendingTokenRect.localScale = pendingTokenBaseScale;
            }

            if (pendingTokenText != null)
            {
                pendingTokenText.fontSize = draggingPlacementFocus ? 13 : 15;
            }

            if (pendingTokenBackgroundImage != null)
            {
                Color tokenBg = pendingTokenBackgroundImage.color;
                float targetAlpha;
                if (draggingPlacementFocus)
                {
                    targetAlpha = Mathf.Clamp01(pendingTokenUltraFocusAlpha);
                }
                else if (pendingHoverAnchorCell >= 0)
                {
                    targetAlpha = pendingHoverValid ? PendingTokenValidColor.a : PendingTokenInvalidColor.a;
                }
                else
                {
                    targetAlpha = PendingTokenNeutralColor.a;
                }

                tokenBg.a = targetAlpha;
                pendingTokenBackgroundImage.color = tokenBg;
            }

            if (cueBannerRect != null)
            {
                if (draggingPlacementFocus)
                {
                    cueBannerRect.anchorMin = new Vector2(0.36f, 0.968f);
                    cueBannerRect.anchorMax = new Vector2(0.64f, 0.996f);
                }
                else if (minimalCombatStrip)
                {
                    cueBannerRect.anchorMin = new Vector2(0.69f, 0.958f);
                    cueBannerRect.anchorMax = new Vector2(0.97f, 0.990f);
                }
                else if (combatOnlyFocus)
                {
                    cueBannerRect.anchorMin = new Vector2(0.34f, 0.95f);
                    cueBannerRect.anchorMax = new Vector2(0.66f, 0.988f);
                }
                else if (gameplayFocusHud)
                {
                    cueBannerRect.anchorMin = new Vector2(0.24f, 0.92f);
                    cueBannerRect.anchorMax = new Vector2(0.76f, 0.975f);
                }
                else
                {
                    cueBannerRect.anchorMin = new Vector2(0.18f, 0.90f);
                    cueBannerRect.anchorMax = new Vector2(0.82f, 0.97f);
                }

                cueBannerRect.offsetMin = Vector2.zero;
                cueBannerRect.offsetMax = Vector2.zero;
            }

            if (cueBannerText != null)
            {
                cueBannerText.fontSize = draggingPlacementFocus ? 13 : (minimalCombatStrip ? 12 : (combatOnlyFocus ? 14 : (gameplayFocusHud ? 16 : 20)));
            }

            float inventoryCellMinSize = inventoryCellMinSizeExpanded;
            float inventoryCellMaxSize = inventoryCellMaxSizeExpanded;
            int inventoryCellLabelSize = inventoryCellLabelFontSizeExpanded;
            if (gameplayFocusHud)
            {
                if (draggingPlacementFocus)
                {
                    inventoryCellMinSize = inventoryCellMinSizeUltraFocus;
                    inventoryCellMaxSize = inventoryCellMaxSizeUltraFocus;
                    inventoryCellLabelSize = inventoryCellLabelFontSizeUltraFocus;
                }
                else if (hasPlacementContext)
                {
                    inventoryCellMinSize = inventoryCellMinSizeFocusPlacement;
                    inventoryCellMaxSize = inventoryCellMaxSizeFocusPlacement;
                    inventoryCellLabelSize = inventoryCellLabelFontSizeFocusPlacement;
                }
                else
                {
                    inventoryCellMinSize = inventoryCellMinSizeFocusCombat;
                    inventoryCellMaxSize = inventoryCellMaxSizeFocusCombat;
                    inventoryCellLabelSize = inventoryCellLabelFontSizeFocusCombat;
                }
            }

            bool inventoryClampChanged = UpdateInventoryCellSizeClampRuntime(inventoryCellMinSize, inventoryCellMaxSize);
            bool inventoryLabelChanged = ApplyInventoryCellLabelFontSize(inventoryCellLabelSize);
            if (inventoryClampChanged || inventoryLabelChanged)
            {
                UpdateInventoryGridSizing();
                CacheInventoryGridBaseAnchoredPosition();
            }

            if (gameplayFocusHud && telemetryPanelRect != null && telemetryPanelRect.gameObject.activeSelf)
            {
                telemetryPanelRect.gameObject.SetActive(false);
            }

            if (topPanelRect != null)
            {
                Image image = topPanelRect.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, gameplayFocusHud ? topPanelFocusAlpha : topPanelExpandedAlpha);
                    if (draggingPlacementFocus)
                    {
                        image.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, topPanelFocusAlpha * 0.55f);
                    }
                    else if (combatOnlyFocus)
                    {
                        image.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, topPanelFocusAlpha * (minimalCombatStrip ? 0.58f : 0.72f));
                    }
                }
            }

            if (bottomPanelRect != null)
            {
                Image image = bottomPanelRect.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, gameplayFocusHud ? bottomPanelFocusAlpha : bottomPanelExpandedAlpha);
                    if (draggingPlacementFocus)
                    {
                        image.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, bottomPanelFocusAlpha * 0.70f);
                    }
                    else if (combatOnlyFocus)
                    {
                        image.color = new Color(PanelDark.r, PanelDark.g, PanelDark.b, bottomPanelFocusAlpha * 0.48f);
                    }
                }
            }
        }

        private bool UpdateInventoryCellSizeClampRuntime(float minSize, float maxSize)
        {
            float normalizedMin = Mathf.Clamp(minSize, 32f, 220f);
            float normalizedMax = Mathf.Clamp(maxSize, normalizedMin, 240f);
            if (Mathf.Abs(inventoryGridCellSizeMinRuntime - normalizedMin) <= 0.01f
                && Mathf.Abs(inventoryGridCellSizeMaxRuntime - normalizedMax) <= 0.01f)
            {
                return false;
            }

            inventoryGridCellSizeMinRuntime = normalizedMin;
            inventoryGridCellSizeMaxRuntime = normalizedMax;
            return true;
        }

        private bool ApplyInventoryCellLabelFontSize(int fontSize)
        {
            int target = Mathf.Clamp(fontSize, 8, 24);
            if (inventoryCellLabelFontSizeRuntime == target)
            {
                return false;
            }

            inventoryCellLabelFontSizeRuntime = target;
            for (int i = 0; i < inventoryCellLabels.Count; i++)
            {
                Text label = inventoryCellLabels[i];
                if (label != null)
                {
                    label.fontSize = target;
                }
            }

            return true;
        }

        private void RefreshGameplayHudContextImmediate()
        {
            if (model == null)
            {
                return;
            }

            bool focusPlacementMode = model.HasPendingBlock && (isDraggingPending || pendingHoverAnchorCell >= 0);
            ApplyGameplayHudContext(focusPlacementMode, IsGameplayFocusHudActive());
        }

        private static void SetNodeVisible(Component component, bool visible)
        {
            if (component == null)
            {
                return;
            }

            GameObject node = component.gameObject;
            if (node.activeSelf != visible)
            {
                node.SetActive(visible);
            }
        }

        private void SetMeterValue(MeterWidget widget, float ratio, string label)
        {
            if (widget.FillRect == null || widget.ValueText == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            widget.FillRect.anchorMax = new Vector2(ratio, 1f);
            widget.ValueText.text = label;
        }

        private void RefreshHeatMeter()
        {
            string heatState = model.IsOverheated ? "OVERHEAT" : (model.IsHeatWarning ? "WARNING" : "STABLE");
            string cooldownSuffix = model.VentCooldownRemaining > 0f
                ? " | CD " + model.VentCooldownRemaining.ToString("0") + "s"
                : string.Empty;
            string heatLabel;
            if (minimalCombatStripActive)
            {
                heatLabel = model.Heat.ToString("0.0") + "  " + heatState + cooldownSuffix;
            }
            else
            {
                heatLabel = model.Heat.ToString("0.0") + "  " + heatState +
                    "  A" + model.HeatAttackMultiplier.ToString("0.00") +
                    " L" + model.HeatLootMultiplier.ToString("0.00") +
                    " R" + model.HeatRiskMultiplier.ToString("0.00") +
                    cooldownSuffix;
            }

            SetMeterValue(heatMeter, model.Heat / 100f, heatLabel);

            if (heatMeter.FillImage == null)
            {
                return;
            }

            if (model.IsOverheated)
            {
                heatMeter.FillImage.color = HeatDangerColor;
            }
            else if (model.IsHeatWarning)
            {
                heatMeter.FillImage.color = HeatWarningColor;
            }
            else
            {
                heatMeter.FillImage.color = HeatSafeColor;
            }
        }

        private void RefreshSynergyChips()
        {
            for (int i = synergyContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(synergyContainer.GetChild(i).gameObject);
            }

            if (model.ActiveRecipes.Count == 0)
            {
                CreateChip("No active recipe", new Color(0.35f, 0.38f, 0.43f, 1f));
                return;
            }

            for (int i = 0; i < model.ActiveRecipes.Count; i++)
            {
                RecipeState recipe = model.ActiveRecipes[i];
                Color color = recipe.Tier == RecipeTier.Advanced
                    ? new Color(0.90f, 0.62f, 0.23f, 1f)
                    : recipe.Tier == RecipeTier.Intermediate
                        ? new Color(0.32f, 0.72f, 0.86f, 1f)
                        : new Color(0.35f, 0.79f, 0.47f, 1f);

                string chipText = recipe.Name + "  [" + recipe.RemainingSeconds.ToString("0") + "s]";
                RectTransform chip = CreateChip(chipText, color);
                if (chip != null
                    && synergyChipPulseTimer > 0f
                    && string.Equals(recipe.Name, lastActivatedRecipeName, StringComparison.Ordinal))
                {
                    float duration = Mathf.Max(0.08f, synergyChipPulseDuration);
                    float t = 1f - Mathf.Clamp01(synergyChipPulseTimer / duration);
                    float pulse = Mathf.Clamp01(0.45f + Mathf.Sin(t * Mathf.PI * 3f) * 0.55f);
                    chip.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.08f, 1.08f, 1f), pulse);
                    Image chipImage = chip.GetComponent<Image>();
                    if (chipImage != null)
                    {
                        chipImage.color = Color.Lerp(color, new Color(1.0f, 0.84f, 0.30f, 1f), pulse * 0.65f);
                    }
                }
            }
        }

        private RectTransform CreateChip(string text, Color color)
        {
            RectTransform chip = new GameObject("Chip", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            chip.transform.SetParent(synergyContainer, false);
            chip.GetComponent<Image>().color = color;
            LayoutElement layout = chip.GetComponent<LayoutElement>();
            layout.minWidth = 190f;
            layout.preferredWidth = 220f;

            Text label = CreateText(chip, "Label", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            label.text = text;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            return chip;
        }

    }
}
