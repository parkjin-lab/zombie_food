# Prototype Update Roadmap

Last updated: 2026-05-04 KST

## Purpose
이 문서는 다음 세션에서 바로 읽고 실행할 수 있는 향후 업데이트 방향성이다. 기준 코어 루프는 `Draw 3 -> Choose 1 -> Rotate/Place -> Survive Wave -> Next Decision`이며, 새 기능보다 먼저 검증 가능한 재미, 안정적인 반복, 수동 Play Mode 증거 추적을 우선한다.

## Current Status
- 코드 레벨 가드 상태: `gate_status=ok`, `layout_status=ok`, `hud_contract_status=ok`, `static_status=ok`.
- 환경 한계: Unity compile/tests는 현재 headless 환경에서 `inconclusive`일 수 있다.
- Play Mode suite: `playmode_suite_status=not_recorded`, `captured_count=0/4`.
- Screenshot evidence: `playmode_screenshot_status=partial`; 기존 PNG 2개는 유효한 portrait evidence지만 suite 라벨 커버리지는 없다.
- Manual record: `playmode_record_status=not_recorded`; Wave Combat만 `PASS`, Draw Choice/Pending Placement/Invalid Placement는 `NOT_RECORDED`.
- Review pack: `Write-PrototypePlayModeReviewPack.ps1 -PreviewOnly -JsonOnly`가 `partial_evidence`를 보고한다.

## Start Here
1. 현재 상태를 출력한다.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
2. Unity Play Mode에서 `Tools > Food Truck Prototype > Capture Verification Suite`를 실행한다.
3. suite와 스크린샷 증거를 확인한다.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
4. 시각 검토 직전에 review pack을 만든다.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
   ```
5. Draw Choice, Pending Placement, Invalid Placement, Wave Combat을 직접 보고 `PASS/FIX/BLOCKED`를 결정한다.
6. 결과를 기록한다.
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS -Apply
   powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
   ```

## 1. Short-Term Verification Stability
### Goal
- Play Mode 수동 판정을 코드/문서/스크린샷 증거와 분리하지 않는다.
- 자동 검증은 누락과 파손을 잡고, 사람은 실제 가독성/조작감/재미를 판단한다.

### Next Actions
- `Show-PrototypeSessionStatus.ps1`를 세션 시작 명령으로 고정한다.
- suite verifier가 네 상태를 모두 기록했는지 확인한다.
- screenshot verifier로 PNG 유효성, portrait 해상도, 상태 라벨 커버리지를 확인한다.
- review pack을 만든 뒤 시각 판정을 진행한다.
- `Write-PrototypePlayModeResultFromSuite.ps1` 또는 Unity 메뉴 `Record PASS Manual Result`로 결과를 닫는다.

### Acceptance
- `playmode_suite_status=captured` 또는 실패 사유가 명확하다.
- `playmode_screenshot_status=suite_ready` 또는 부족한 상태 라벨이 명확하다.
- `Prototype_PlayMode_Verification.md`의 `Latest Manual Result`가 최신 증거와 연결된다.
- HUD 변경 후 `Verify-PrototypeHudStateContract.ps1`가 통과한다.
- 레이아웃 변경 후 `Verify-PrototypeLayout.ps1`가 통과한다.

## 2. Core Loop Fun Reinforcement
### Goal
- 선택은 단순 메뉴가 아니라 `안전/욕심/유틸리티` 사이의 즉시 비교가 되게 한다.
- 배치는 숨은 유효성 검사가 아니라 손맛 있는 공간 퍼즐이 되게 한다.
- 웨이브 결과는 이전 선택이 왜 좋았거나 나빴는지 되돌려준다.

### Next Actions
- Draw 카드 3장에 `EV`, `Risk`, `Target/Role` 정보를 표시한다.
- 카드 조합은 임시라도 safe, greedy, utility가 섞이게 한다.
- Pending Placement에서 회전, 앵커, 막힌 셀, 영향 범위가 보이도록 preview를 강화한다.
- Invalid Placement에서 `occupied`, `out_of_bounds`, `invalid_anchor`, `no_pending` 사유를 보드 근처에 노출한다.
- Wave Combat 끝에는 마지막 배치가 막은 레인, 만든 딜, 누적 Heat, 콤보 기여 중 하나를 짧게 보여준다.

### Acceptance
- 플레이어가 3초 안에 카드 3장의 차이를 말할 수 있다.
- 같은 실패 사유로 3회 이상 반복 실패하는 빈도가 줄어든다.
- Wave Combat 화면에서 HP, Heat, Wave, lane pressure, truck position이 동시에 읽힌다.
- 새 기능 추가보다 Draw/Pending/Invalid/Wave suite readability PASS 또는 명확한 FIX 기록이 먼저다.

## 3. Content And Wave Expansion
### Goal
- 검증된 루프 위에 3-wave 단위 학습/긴장/보상 구조를 얹는다.
- 콘텐츠 추가는 기존 카드 태그와 실패 사유 언어를 재사용한다.

### Roadmap
- Wave 1-3: 기본 배치, lane pressure, 카드 역할 이해.
- Wave 4-6: Heat risk, greedy pick, vent/relief 선택지 도입.
- Wave 7+: shape modifier, targeting profile, combo trigger, limited reposition 도입.

### Next Actions
- 웨이브별 목표 압력표를 만든다: 레인 분산, 적 밀도, 예상 clear margin.
- 카드/블록 태그를 정리한다: safe, greedy, utility, heat, lane, burst, reroll.
- wave 1 또는 wave 3 이후 placeholder reward 선택을 1회만 추가해 보상 루프를 검증한다.
- 새 카드나 보상은 한 번에 하나의 변수만 바꿔 Play Mode capture 병목을 줄인다.

### Acceptance
- 새 콘텐츠는 기존 suite 상태 중 어느 화면을 바꾸는지 명시한다.
- 새 웨이브는 최소 1개 KPI를 가진다: pick rate, success rate, overheat count, clear margin.
- 보상은 단순 수치 상승만 반복하지 않고 다음 선택을 바꾸는 효과가 있다.

## 4. Art And Feedback Policy
### Goal
- placeholder 상태에서도 실제 플레이 판단에 필요한 실루엣과 피드백 우선순위를 지킨다.
- 최종 아트 교체가 UX 검증 기준을 깨지 않게 한다.

### Policy
- 카드 아이콘은 텍스트 없이 음식/역할이 구분되어야 한다.
- Food truck, kitchen module, enemy, heat/overheat, placement cell은 서로 다른 실루엣을 가져야 한다.
- 핵심 행동에는 하나의 시각 피드백과 하나의 텍스트 피드백을 연결한다.
- 실패 피드백은 전체 로그보다 상호작용 위치 근처에 우선 노출한다.
- cue banner 문장은 한 행동만 지시한다.

### Next Actions
- 배치 성공 pop, 실패 shake/red flash, overheat spike, reward claim feedback preset을 정의한다.
- placeholder PNG는 같은 파일명으로 교체 가능하게 유지한다.
- `Verify-PrototypeAssets.ps1`로 필수 리소스와 `.meta` 누락을 확인한다.
- 최종 아트가 들어오면 suite screenshot을 다시 찍고 review pack에서 가독성을 비교한다.

### Acceptance
- portrait 화면에서 카드, 보드, 전투 레인이 서로를 가리지 않는다.
- 색상만으로 상태를 구분하지 않고 실루엣, 위치, 짧은 텍스트를 함께 쓴다.
- 아트 교체 후에도 `FIX_ASSET` 없이 manual result를 기록할 수 있다.

## 5. Data And Playtest Operation
### Goal
- "재미있다/답답하다"를 다음 코드 작업으로 바꿀 수 있는 최소 지표를 기록한다.
- 수동 Play Mode 병목이 있어도 매 세션 같은 방식으로 증거를 추적한다.

### Metrics
- `placed_success / place_attempt`
- `blocked_place_reason_count` by reason
- `draw_choice_pick_rate` by card slot/tag
- `draw_choice_decision_ms`
- `draw_to_place_time_sec`
- `rotations_per_pending`
- `overheat_events_per_wave`
- `wave_clear_margin`
- `reward_pick_rate`

### Operating Loop
1. 세션 시작: show status로 readiness와 unresolved issue를 확인한다.
2. 변경 전 관련 verifier를 먼저 실행해 기준선을 잡는다.
3. 구현: 한 세션에는 하나의 재미 가설만 바꾼다.
4. 캡처: Capture Verification Suite로 네 상태를 다시 기록한다.
5. 검증: suite verifier, screenshot verifier, review pack을 순서대로 실행한다.
6. 판정: result writer로 `PASS/FIX/BLOCKED`를 기록한다.
7. 회고: 가장 큰 실패 사유 1개와 다음 코드 목표 1개만 남긴다.

### Acceptance
- 모든 playtest note는 사용한 빌드/날짜/해상도/결과 상태를 포함한다.
- FIX 결과는 `FIX_LAYOUT`, `FIX_ASSET`, `FIX_FEEDBACK`, `BLOCKED` 중 하나로 먼저 분류한다.
- 다음 세션은 새 아이디어보다 마지막 `Top issue`와 `Next code target`을 먼저 처리한다.

## Command Reference
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeHudStateContract.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -PreviewOnly -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Gate-Verification.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -RunTests -JsonOnly
```

## Do Not Skip
- 다른 에이전트가 작업 중일 수 있으므로 이 문서를 제외한 파일은 로드맵 정리 목적으로 수정하지 않는다.
- 새 기능 추가 전 Draw Choice, Pending Placement, Invalid Placement, Wave Combat 증거를 먼저 확인한다.
- 자동 verifier가 `ok`여도 최종 Play Mode 시각 판정은 review pack을 보고 수동으로 결정한다.
