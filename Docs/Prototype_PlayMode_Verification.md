# Prototype Play Mode Verification

Last updated: 2026-05-04 16:20 KST

## Purpose
Unity Editor에서 직접 확인해야 하는 UI/UX 검증 기준이다. 현재 로컬 headless 검증은 `compile_status=inconclusive`, `tests_status=inconclusive`가 나올 수 있으므로, 이 문서를 수동 Play Mode 검증의 기준으로 사용한다.

## Preflight
1. Unity Editor로 `D:\uni\zombieFoodcenter` 프로젝트를 연다.
2. Console에 신규 compile error가 없는지 확인한다.
3. `Assets/Resources/FoodTruckPrototype` 아래 PNG들이 Sprite로 import되었는지 확인한다.
4. Play Mode 진입 전 아래 명령이 통과하는지 확인한다.

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Gate-Verification.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -RunTests -JsonOnly
```

## Editor Helpers
- Play Mode 중 `Tools > Food Truck Prototype > Capture Play Mode Snapshot`을 실행하면 `Docs/PlayModeScreenshots`에 스크린샷을 저장하고 `Docs/Prototype_PlayMode_Verification_Draft.txt`에 현재 Unity 버전/해상도/리소스 로드 상태를 기록한다.
- 네 상태가 모두 PASS임을 직접 확인한 뒤 `Tools > Food Truck Prototype > Record PASS Manual Result`를 실행하면 `Latest Manual Result`가 자동으로 PASS 기록으로 갱신된다.
- 문제가 보이면 PASS 기록 메뉴를 쓰지 말고 아래 Result Template 값을 직접 채운다.

## Required States
### Draw Choice
- 3개 선택 카드가 동시에 보인다.
- 각 카드의 음식 아이콘이 텍스트 없이도 구분 가능하다.
- 카드 선택 영역이 전투/게임 화면을 완전히 덮지 않는다.
- EV/Risk/역할 정보가 3초 안에 비교 가능하다.

### Pending Placement
- 선택된 블록 프리뷰가 보인다.
- 3x3 배치판이 손가락/마우스로 실수 없이 누를 수 있는 크기다.
- 배치판, 선택 블록, 주요 전투 레인이 동시에 보인다.
- 회전 안내와 배치 안내가 현재 행동 하나만 강조한다.

### Invalid Placement
- 실패 사유가 보드 근처에 표시된다.
- `occupied`, `out_of_bounds`, `invalid_anchor`, `no_pending` 중 어떤 실패인지 바로 구분된다.
- 실패 피드백이 로그를 읽지 않아도 행동 수정으로 이어진다.

### Wave Combat
- 푸드트럭 마커가 레인 왼쪽에서 보인다.
- 적 이동, 레인 압력, HP, Heat, Wave 상태가 동시에 읽힌다.
- 배치/카드 편집 UI가 전투 화면을 불필요하게 덮지 않는다.

## Acceptance Criteria
- `PASS`: Draw choice, Pending placement, Invalid placement, Wave combat 모두 읽을 수 있고 조작 가능한 상태다.
- `FIX_LAYOUT`: 보드/카드/전투 화면 중 하나라도 주요 상태에서 가려진다.
- `FIX_ASSET`: Sprite가 빠지거나 아이콘이 너무 작아 구분되지 않는다.
- `FIX_FEEDBACK`: 배치 실패 이유가 보드 근처에서 즉시 이해되지 않는다.
- `BLOCKED`: Unity compile error 또는 Play Mode 진입 실패.

## Latest Manual Result
Date: 2026-05-04 01:02 KST
Unity version: 6000.3.8f1
Aspect ratio / resolution: 1170x2532 (195:422)

Draw Choice: NOT_RECORDED
Pending Placement: NOT_RECORDED
Invalid Placement: NOT_RECORDED
Wave Combat: PASS

Screenshots captured: 2
1. Docs/PlayModeScreenshots/foodtruck-playmode-20260504-010153.png
2. Docs/PlayModeScreenshots/foodtruck-playmode-20260504-010245.png
Top issue: Current PC cannot reliably continue direct Play Mode interaction. Captured Wave Combat was readable, but the bottom build panel occupied too much portrait space when no block was pending.
Next code target: Completed 2026-05-04 16:20 KST; combat-only HUD now hides the 3x3 build grid, keeps the bottom as a compact command strip, and reapplies layout when combat/build context changes.
Verification command result: Partial Play Mode record remains `not_recorded` until Draw Choice, Pending Placement, and Invalid Placement can be captured; `Tools\Verify-PrototypeLayout.ps1` and `Tools\Verify-PrototypeStatic.ps1` pass after the combat-only HUD change.

## Result Template
```text
Date:
Unity version:
Aspect ratio / resolution:

Draw Choice: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED
Pending Placement: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED
Invalid Placement: PASS / FIX_FEEDBACK / FIX_LAYOUT / BLOCKED
Wave Combat: PASS / FIX_LAYOUT / FIX_ASSET / BLOCKED

Screenshots captured:
1.
2.
3.
4.

Top issue:
Next code target:
Verification command result:
```

## If Layout Fails
우선 수정 대상은 `FoodTruckPrototypeHud.CalculateGameplayFocusLayout`과 `ApplyPanelLayout`이다. 새 기능 추가보다 먼저 게임 화면, 배치판, 블록 선택 UI의 동시 가독성을 해결한다.
