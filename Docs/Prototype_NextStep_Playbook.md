# 프로토타입 다음 단계 플레이북

## 현재 기준
- 최신 상세 인계: `Docs/Prototype_Session_Handoff.md`.
- 수동 Play Mode 검증표: `Docs/Prototype_PlayMode_Verification.md`.
- 향후 업데이트 방향성: `Docs/Prototype_Update_Roadmap.md`.
- 2026-05-04 20:24 KST 기준 코드 레벨 가드(`gate`, `layout`, `HUD state contract`, `static`)는 통과한다.
- 남은 핵심 리스크는 Play Mode 수동 검증이다. 현재 `playmode_suite_status=not_recorded`, `playmode_screenshot_status=partial`, `playmode_record_status=not_recorded` 상태다.
- 기존 Wave Combat 스크린샷 2장은 PNG/세로 품질은 통과하지만 suite 라벨 커버리지가 없어 최종 판정 증거로는 아직 `partial`이다.
- Unity MCP와 headless 검증은 환경에 따라 막힐 수 있으므로, 로컬 스크립트와 열린 Unity Editor의 Play Mode 메뉴를 우선 사용한다.

## 이번 스프린트 목표
- Play Mode 검증 닫기: `Capture Verification Suite` -> suite verifier -> screenshot verifier -> review pack -> result writer/record verifier 순서로 증거와 판정을 남긴다.
- PC 입력 불안정성 완화: 긴 직접 플레이보다 전체 suite 캡처를 먼저 실행하고, 실패한 상태만 focused retake로 다시 찍는다.
- UX 다음 작업 게이트 고정: PASS/FIX/BLOCKED 결과가 기록되기 전에는 새 메커니즘보다 레이아웃/피드백 수정에 집중한다.

## 우선순위
### P0 (즉시: Play Mode 증거와 결과 기록)
- 첫 상태 확인은 `Tools\Show-PrototypeSessionStatus.ps1`로 시작한다. suite/스크린샷/record 상태가 기대와 다르면 먼저 인계 문서를 확인한다.
- Unity Play Mode에서 `Tools > Food Truck Prototype > Capture Verification Suite`를 실행해 Draw Choice, Pending Placement, Invalid Placement, Wave Combat 네 상태를 한 번에 캡처한다.
- suite 캡처 직후 `Tools\Verify-PrototypePlayModeSuite.ps1`로 네 상태의 manifest와 스크린샷 파일 존재를 확인한다.
- 이어서 `Tools\Verify-PrototypePlayModeScreenshots.ps1`로 PNG 유효성, 세로 해상도, 파일 크기, suite 라벨 커버리지를 확인한다.
- `Tools\Write-PrototypePlayModeReviewPack.ps1`로 suite 상태, screenshot 상태, record 상태, contact sheet, 결과 명령 템플릿을 한 장에 모은다.
- review pack을 보고 네 상태가 모두 읽히고 조작 가능하면 `Tools\Write-PrototypePlayModeResultFromSuite.ps1 ... -Apply` 또는 Unity 메뉴 `Record PASS Manual Result`로 PASS를 기록한다.
- 하나라도 문제가 있으면 PASS 메뉴를 쓰지 말고 result writer로 `FIX_LAYOUT`, `FIX_ASSET`, `FIX_FEEDBACK`, `BLOCKED` 중 실제 상태를 기록한다.
- 기록 후 `Tools\Verify-PrototypePlayModeRecord.ps1`로 문서가 파싱 가능한지 확인하고, 마지막으로 `Tools\Gate-Verification.ps1 -RunTests -JsonOnly`를 실행해 코드 가드가 유지되는지 본다.

### P1 (다음: 결과 기반 UX 수정)
- `FIX_LAYOUT`이면 `FoodTruckPrototypeHud.CalculateGameplayFocusLayout`, `ApplyGameplayHudContext`, `ApplyPanelLayout` 쪽을 우선 본다. 수정 뒤 layout guard와 HUD state contract를 실행하고 해당 상태만 focused retake한다.
- `FIX_FEEDBACK`이면 Invalid Placement의 실패 사유가 보드 근처에서 즉시 이해되는지 먼저 고친다. 수정 뒤 HUD state contract와 Invalid Placement retake를 실행한다.
- `FIX_ASSET`이면 ingredient/truck/kitchen module Sprite import, 크기, 대비를 점검한다. 수정 뒤 asset verifier와 영향을 받은 상태 retake를 실행한다.
- 네 상태가 PASS이면 Draw 카드 시각 언어(EV/Risk/역할 비교), 배치 성공/실패 마이크로 애니메이션, 체크리스트/플레이 로그 문구 동기화를 다음 개선 대상으로 둔다.

### P2 (중기: 자동화와 제품 확장)
- review pack 산출물을 세션별로 비교하기 쉽게 보관하고, suite 스크린샷의 의미적 차이는 아직 수동 판정으로 남긴다.
- 간단 텔레메트리 저장/리포트 자동화를 붙여 `place_attempt`, `blocked reason`, Draw pick rate를 회고에 연결한다.
- 3웨이브 단위 해금 구조(타겟팅/형태/열관리)를 도입하되, Play Mode 검증이 PASS 또는 명확한 FIX 기록으로 닫힌 뒤 진행한다.
- 연출 트리거 표준화(이벤트 선택, 배치, 콤보 버스트)는 결과 기록과 회귀 가드가 안정된 뒤 확장한다.

## 측정 지표
- Suite coverage: `captured_count / expected_state_count`가 `4/4`인지 확인
- Screenshot evidence: `machine_quality_pass_count / screenshot_count`, missing labeled coverage
- Review readiness: `ready_for_visual_review` 또는 `partial_evidence`
- Manual record: `passed`, `needs_fix`, `blocked`, `not_recorded`, `invalid_record`
- 배치 성공률: `placed_success / place_attempt`
- blocked reason 분포: `out_of_bounds`, `occupied`, `invalid_anchor`, `no_pending`
- Draw pick rate: 카드 슬롯별 선택 비중(1/2/3)
- Draw -> Place 전환 시간(초)
- 웨이브당 과열(Overheat) 발생 횟수

## 데일리 체크리스트
### 디자인
- 오늘 카드 선택에서 "왜 이 카드를 골랐는지"가 3초 내 설명 가능한가?
- 실패 피드백 문구가 행동 수정에 직접 도움 되는가?
- review pack contact sheet만 보고도 Draw/Pending/Invalid/Wave의 주요 UI가 가려졌는지 판단 가능한가?

### 개발
- 작업 전 `Tools\Show-PrototypeSessionStatus.ps1`로 suite/스크린샷/record 상태를 확인했는가?
- 단계 잠금(event/draw/pending)에서 누락된 입력 경로가 없는가?
- HUD 텍스트/배너/버튼 상태가 동일한 상태머신을 참조하는가?
- Draw/Pending/Invalid Placement HUD를 바꿨다면 `Tools\Verify-PrototypeHudStateContract.ps1`를 실행했는가?
- 레이아웃을 바꿨다면 `Tools\Verify-PrototypeLayout.ps1`와 focused retake를 실행했는가?

### 플레이테스트
- Play Mode 진입 후 긴 직접 플레이보다 `Capture Verification Suite`를 먼저 실행했는가?
- suite verifier -> screenshot verifier -> review pack -> result writer 순서로 증거를 닫았는가?
- focused retake는 전체 suite에서 실패하거나 빠진 상태에만 사용했는가?
- 실패 로그 상위 2개 원인을 `FIX_LAYOUT`, `FIX_ASSET`, `FIX_FEEDBACK`, `BLOCKED` 중 하나와 다음 코드 타겟으로 연결했는가?

## 실행 순서
### 1. 상태 확인
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

### 2. Play Mode suite 캡처
- Unity Editor에서 Play Mode 진입
- `Tools > Food Truck Prototype > Capture Verification Suite`
- 일부 상태만 다시 찍을 때: `Tools > Food Truck Prototype > Prepare and Capture State > Draw Choice/Pending Placement/Invalid Placement/Wave Combat`

### 3. suite와 스크린샷 기계 검증
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

### 4. 시각 리뷰 pack 생성
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

### 5. 결과 기록
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS -Apply
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

### 6. 코드 변경 후 회귀 가드
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeHudStateContract.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -Strict -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Gate-Verification.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -RunTests -JsonOnly
```

## Latest Handoff
- Current detailed handoff: `Docs/Prototype_Session_Handoff.md`.
- Manual Play Mode verification sheet: `Docs/Prototype_PlayMode_Verification.md`.
- First status command: `powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- MCP unavailable fallback: local scripts/file inspection first; MCP 연결 문제로 completion-critical UX 검증을 멈추지 않는다.
- Immediate next validation: Play Mode에서 `Capture Verification Suite`를 실행한 뒤 suite verifier, screenshot verifier, review pack, result writer 순서로 닫는다.
- Current fallback status: Wave Combat 코드 보완과 helper-state setup, suite capture/evidence verifier, screenshot quality verifier, review pack writer, suite-backed result writer는 준비되어 있다.
- Manual record remains open until Draw Choice, Pending Placement, Invalid Placement, Wave Combat suite evidence is captured, visually reviewed, and recorded.
