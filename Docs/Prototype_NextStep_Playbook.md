# 프로토타입 다음 단계 플레이북

## 현재 기준
- 최신 상세 인계: `Docs/Prototype_Session_Handoff.md`.
- 수동 Play Mode 검증표: `Docs/Prototype_PlayMode_Verification.md`.
- 향후 업데이트 방향성: `Docs/Prototype_Update_Roadmap.md`.
- 리듬 디자인 점검: `Docs/Prototype_RhythmDesign_Audit.md`.
- 2026-05-08 01:52 KST 기준 코드 레벨 가드(`gate`, `layout`, `HUD state contract`, `static`)는 PlayMode evidence preflight 경로를 추가한 뒤에도 통과한다.
- 2026-05-17 01:18 KST 기준 sub-agent review 결과, 디자인 방향성은 rhythm-first로 유지한다. 즉시는 Play Mode evidence closure, 그 다음 구현 후보는 `Wave Cadence Composer`와 `Payoff-to-Read Panel`이다.
- 남은 핵심 리스크는 Play Mode 수동 검증이다. 현재 `playmode_suite_status=manual_partial`, `playmode_screenshot_status=partial`, `playmode_record_status=not_recorded` 상태다.
- tracked 코드/문서는 이 업데이트 전 깨끗했지만, Unity/Asset Store import 흔적으로 보이는 대형 untracked 폴더들이 남아 있다. 명시적 에셋 결정 없이 스테이징하지 않는다.
- 기존 Wave Combat 스크린샷 2장은 PNG/세로 품질은 통과한다. 그중 1장은 suite manifest에 Wave Combat 수동 증거로 등록되어 `covered_state_count=1/4`가 되었고, 다른 1장은 Build Flow idle로 triage되어 `triaged_non_state_count=1`로 표시된다. Draw Choice/Pending Placement/Invalid Placement는 아직 missing이고 `manual_registration_candidate_count=0`이다.
- Unity MCP와 headless 검증은 환경에 따라 막힐 수 있으므로, 로컬 스크립트와 열린 Unity Editor의 Play Mode 메뉴를 우선 사용한다.
- PC 제한 상황에서도 코어 루프 개선은 진행 중이다. 최신 코드 단계는 Wave Combat 종료 후 KO/damage, HP/Heat 변화, supplies, peak Heat, combo/leak 정보를 짧은 payoff cue로 남긴다.
- Draw Choice 카드는 이제 `Fit`, `Heat`, `Role` 칩으로 세 카드의 즉시 차이를 더 빨리 비교하게 만드는 방향으로 보강됐다.
- Invalid Placement 피드백은 이제 사유와 함께 `Next` 행동 힌트를 표시해 같은 실패를 반복하지 않도록 보강됐다.
- Pending Placement 추천은 이제 점수보다 `cover L3 high`, `2-lane`, `center` 같은 이유를 먼저 보여준다.
- Recipe activation 피드백은 이제 단순히 레시피 이름만 말하지 않고, 보너스 롤인지 3x 빙고 조건인지까지 같이 노출하는 방향으로 보강 중이다.
- Synergy Bar는 최근 레시피 발동 원인을 별도 칩으로 유지해, 배너가 사라진 뒤에도 방금 만든 보상이 무엇 때문인지 확인할 수 있게 하는 방향으로 보강 중이다.
- Active recipe 칩은 이제 남은 시간뿐 아니라 `Regen/Cool` 또는 `Lane Hit` 역할을 함께 보여주는 방향으로 보강 중이다.
- Recipe expiry 피드백은 이제 레시피가 실제로 만든 피해, KO, 회복, Heat 완화 성과를 짧게 남기는 방향으로 보강 중이다.
- Active recipe 칩은 이제 만료 전에도 `Dmg`, `KO`, `HP`, `Heat` 진행 성과나 `Warming up` 상태를 보여주는 방향으로 보강 중이다.
- 최신 웨이브 payoff는 이제 Synergy Bar의 별도 칩으로도 남겨, 다음 드로우/배치 판단 중 다시 확인할 수 있게 하는 방향으로 보강 중이다.
- 전장 레이아웃은 이제 Draw/Pending Placement에서도 푸드트럭과 좀비가 화면의 절반 이상을 차지하도록 보강됐다. 트럭은 lane마다 반복하지 않고 한 대의 긴 마커로 보여주며, 피격 순간에는 공격 궤적과 충격 플래시가 더 크게 보이도록 보강됐다.
- 전투 결과 판독성은 이제 좀비 피격 `-damage`, 처치 `KO`, 트럭 도달 `LEAK`, 트럭 피해 `TRUCK -HP` 플로팅 텍스트로 보강됐다.
- Wave Combat suite 캡처는 이제 검증용 액션 showcase를 포함해 `-12`, `KO`, `LEAK`, `TRUCK -7` 표식과 lane flash가 찍히도록 보강됐다.
- Review pack은 이제 Wave Combat action showcase 준비 여부와 사유를 같이 보여주며, 네 상태의 시각 판정 체크리스트를 한 장에 포함한다.
- 게임 리듬감 기준은 이제 별도 audit로 승격됐다. 현재 판정은 "리듬 재료는 있으나, beat map과 tension/release 검증 기준이 부족하다"이며, 다음 기능은 `Read`, `Commit`, `Pressure`, `Payoff`, `Release` 중 어느 beat를 개선하는지 먼저 밝혀야 한다.
- `Tools\Show-PrototypeSessionStatus.ps1`도 이제 `wave_combat_action_showcase_ready/reason`을 첫 화면과 JSON에 함께 출력한다.
- `Tools\Show-PrototypeSessionStatus.ps1`은 이제 `review_pack_status`, `review_readiness`, `review_pack_visual_review_required`도 첫 화면과 JSON에 함께 출력한다.
- `Tools\Show-PrototypeSessionStatus.ps1`은 이제 `top_issue`, `next_evidence_action`, `next_code_target`까지 출력해 다음 세션의 첫 행동을 분명히 한다.
- `Tools\Register-PrototypePlayModeManualEvidence.ps1`은 standalone PNG를 상태별 suite 증거로 등록한다. 직접 Play Mode 조작이 불안정할 때 Capture Verification Suite의 보조 경로로 사용하며, screenshot verifier와 review pack preview가 상태별 command template을 생성한다.
- `Docs\Prototype_PlayMode_Screenshot_Triage.txt`는 시각 검토 후 필수 상태 증거가 아니라고 판단한 PNG를 기록한다. triage된 PNG는 더 이상 manual registration candidate로 추천되지 않는다.
- `Tools\Write-PrototypePlayModeRetakePlan.ps1`은 현재 partial evidence를 읽어 Draw Choice, Pending Placement, Invalid Placement의 focused retake 메뉴와 must-see 기준을 한 장짜리 `Docs\Prototype_PlayMode_RetakePlan.md`로 정리한다.
- `Tools\Verify-PrototypePlayModeRetakePlan.ps1`은 `Docs\Prototype_PlayMode_RetakePlan.md`가 현재 suite/screenshot/review preview 상태와 맞는지 확인하고, stale이면 gate/session status에서 바로 드러낸다.
- `Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1`은 session status, retake plan doc, suite, screenshot, review pack preview를 한 번에 모아 `ready_for_focused_retake` 또는 `ready_for_visual_review` 같은 다음 행동 상태를 출력한다.

## 이번 스프린트 목표
- Play Mode 검증 닫기: `Capture Verification Suite` -> suite verifier -> screenshot verifier -> review pack -> result writer/record verifier 순서로 증거와 판정을 남긴다.
- PC 입력 불안정성 완화: 긴 직접 플레이보다 전체 suite 캡처를 먼저 실행하고, 실패한 상태만 focused retake로 다시 찍는다.
- UX 다음 작업 게이트 고정: PASS/FIX/BLOCKED 결과가 기록되기 전에는 새 메커니즘보다 레이아웃/피드백 수정에 집중한다.
- 리듬감 기준 고정: 새 시스템을 추가하기 전 현재 loop가 읽기, 선택, 배치, 압박, 보상, 회복의 박자로 느껴지는지 판정한다.

## 우선순위
### P0 (즉시: Play Mode 증거와 결과 기록)
- 첫 상태 확인은 `Tools\Show-PrototypeSessionStatus.ps1`로 시작한다. suite/스크린샷/review pack/record/showcase 상태가 기대와 다르면 먼저 인계 문서를 확인한다.
- 현재 `top_issue`가 suite 미촬영 또는 `manual_partial`이면 새 gameplay code보다 Capture Verification Suite, focused retake, 또는 standalone PNG 수동 등록을 우선한다.
- 현재 `manual_registration_candidate_count=0`이고 missing state가 남아 있으면 Unity를 열기 전에 `Tools\Write-PrototypePlayModeRetakePlan.ps1`을 실행해 retake checklist를 먼저 만든다.
- retake checklist 생성 뒤 `Tools\Verify-PrototypePlayModeRetakePlan.ps1`로 문서가 현재 증거 상태와 동기화되어 있는지 확인한다.
- Unity를 열기 전 `Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1`로 focused retake 대상과 캡처 후 실행할 검증 명령을 한 번 더 확인한다.
- Unity Play Mode에서 `Tools > Food Truck Prototype > Capture Verification Suite`를 실행해 Draw Choice, Pending Placement, Invalid Placement, Wave Combat 네 상태를 한 번에 캡처한다.
- 직접 suite 캡처가 어렵지만 PNG는 확보했다면 `Tools\Register-PrototypePlayModeManualEvidence.ps1`로 해당 PNG를 상태별 suite 증거에 등록한다.
- 등록할 상태가 애매하면 `Tools\Verify-PrototypePlayModeScreenshots.ps1 -JsonOnly` 또는 review pack preview의 `manual_registration_commands`를 먼저 보고, 시각적으로 맞는 상태에만 적용한다. 시각적으로 맞지 않으면 triage manifest에 남긴다.
- suite 캡처 직후 `Tools\Verify-PrototypePlayModeSuite.ps1`로 네 상태의 manifest와 스크린샷 파일 존재를 확인한다.
- 이어서 `Tools\Verify-PrototypePlayModeScreenshots.ps1`로 PNG 유효성, 세로 해상도, 파일 크기, suite 라벨 커버리지를 확인한다.
- `Tools\Write-PrototypePlayModeReviewPack.ps1`로 suite 상태, screenshot 상태, record 상태, contact sheet, 결과 명령 템플릿을 한 장에 모은다.
- `Show-PrototypeSessionStatus.ps1`에서 `review_readiness=partial_evidence`이면 아직 전체 review pack 판정 전에 suite-state 캡처 또는 focused retake가 필요하다.
- review pack에서 `wave_combat_action_showcase_ready=true`가 아니면 Wave Combat는 아직 액션 증거가 부족한 상태로 보고 retake 또는 `FIX_FEEDBACK` 판정을 우선 고려한다.
- review pack을 보고 네 상태가 모두 읽히고 조작 가능하면 `Tools\Write-PrototypePlayModeResultFromSuite.ps1 ... -Apply` 또는 Unity 메뉴 `Record PASS Manual Result`로 PASS를 기록한다.
- 하나라도 문제가 있으면 PASS 메뉴를 쓰지 말고 result writer로 `FIX_LAYOUT`, `FIX_ASSET`, `FIX_FEEDBACK`, `BLOCKED` 중 실제 상태를 기록한다.
- 기록 후 `Tools\Verify-PrototypePlayModeRecord.ps1`로 문서가 파싱 가능한지 확인하고, 마지막으로 `Tools\Gate-Verification.ps1 -RunTests -JsonOnly`를 실행해 코드 가드가 유지되는지 본다.

### P1 (다음: 결과 기반 UX 수정)
- 다음 코드 작업을 고르기 전에 `Docs\Prototype_RhythmDesign_Audit.md`의 beat map을 기준으로 어떤 beat가 약한지 정한다.
- `FIX_FEEDBACK`이면 먼저 실패한 beat를 분류한다: `Read`, `Commit`, `Pressure`, `Payoff`, `Release`.
- `Wave Cadence Composer` 1차 소스 가드는 완료됐다. 모델은 이벤트/날씨/보스/휴식/언락 beat와 `planned spike` 여부를 `LastWaveCadencePlan`으로 노출하고, HUD state contract와 EditMode 테스트가 이를 고정한다.
- `Payoff-to-Read Panel` 1차 소스 가드도 완료됐다. 기존 wave payoff cue 옆에 `Next` hint chip을 붙여 누수, Heat spike, damage payoff, combo, recovery를 다음 선택 성향으로 번역한다.
- `Rhythm Beat HUD/Telemetry` 1차 소스 가드도 완료됐다. 모델은 `Read`, `Commit`, `Pressure`, `Payoff`, `Release`를 계산하고, HUD/UX telemetry가 같은 beat label을 표시한다.
- Play Mode evidence가 PASS 또는 명확한 FIX로 닫히면, Composer/Payoff-to-Read/Rhythm Beat label이 실제 화면에서 리듬감 있게 느껴지는지 먼저 확인한다. 다음 자율 구현 후보는 beat duration telemetry 또는 pressure ramp tuning이다.
- payoff 가독성이 주된 문제라면 새 시스템을 늘리기보다 `Next` hint 문구/표시 조건을 먼저 조정한다. 다음 Draw/Pending 중 읽히는지 검증한다.
- Rhythm label/telemetry, pressure ramp, rest-phase reward는 위 두 후보 이후로 둔다.
- `FIX_LAYOUT`이면 `FoodTruckPrototypeHud.CalculateGameplayFocusLayout`, `ApplyGameplayHudContext`, `ApplyPanelLayout` 쪽을 우선 본다. 수정 뒤 layout guard와 HUD state contract를 실행하고 해당 상태만 focused retake한다.
- `FIX_FEEDBACK`이면 Invalid Placement의 실패 사유가 보드 근처에서 즉시 이해되는지 먼저 고친다. 수정 뒤 HUD state contract와 Invalid Placement retake를 실행한다.
- `FIX_ASSET`이면 ingredient/truck/kitchen module Sprite import, 크기, 대비를 점검한다. 수정 뒤 asset verifier와 영향을 받은 상태 retake를 실행한다.
- 네 상태가 PASS이면 Draw 카드 시각 언어(EV/Risk/역할 비교), 배치 성공/실패 마이크로 애니메이션, 체크리스트/플레이 로그 문구 동기화를 다음 개선 대상으로 둔다.
- Wave Combat retake에서는 새 payoff cue가 HP/Heat/lane pressure를 가리지 않고, 이전 선택의 결과를 1초 안에 이해시킬 수 있는지 확인한다.
- Draw Choice retake에서는 세 카드의 `Fit`, `Heat`, `Role` 문구가 작은 카드 안에서도 겹치지 않고 읽히는지 확인한다.
- Invalid Placement retake에서는 실패 사유와 `Next` 행동 힌트가 보드 근처/cue에서 동시에 읽히는지 확인한다.
- Pending Placement retake에서는 R1/R2 추천 이유가 보드 조작을 방해하지 않는 길이로 읽히는지 확인한다.
- Wave Combat/Draw/Pending retake에서는 푸드트럭과 좀비 전장이 실제 화면의 50% 이상으로 느껴지는지, 트럭이 한 대만 보이는지, 공격 궤적과 피격 플래시만 보고도 어떤 좀비가 맞았는지 이해되는지 확인한다.
- Wave Combat retake에서는 suite 캡처 직후 액션 showcase 표식(`-12`, `KO`, `LEAK`, `TRUCK -7`)이 겹치지 않고 읽히는지 확인한다.
- Wave Combat retake에서는 압박이 평평하게 흘러가는지, 아니면 상승-피크-해소가 느껴지는지 같이 판정한다.

### P2 (중기: 자동화와 제품 확장)
- review pack 산출물을 세션별로 비교하기 쉽게 보관하고, suite 스크린샷의 의미적 차이는 아직 수동 판정으로 남긴다.
- 간단 텔레메트리 저장/리포트 자동화를 붙여 `place_attempt`, `blocked reason`, Draw pick rate를 회고에 연결한다.
- 3웨이브 단위 해금 구조(타겟팅/형태/열관리)를 도입하되, Play Mode 검증이 PASS 또는 명확한 FIX 기록으로 닫힌 뒤 진행한다.
- 연출 트리거 표준화(이벤트 선택, 배치, 콤보 버스트)는 결과 기록과 회귀 가드가 안정된 뒤 확장한다.

## 측정 지표
- Suite coverage: `captured_count / expected_state_count`가 `4/4`인지 확인
- Screenshot evidence: `machine_quality_pass_count / screenshot_count`, missing labeled coverage
- Review readiness: `ready_for_visual_review` 또는 `partial_evidence`
- Review pack status: `review_pack_status=ok`, `review_pack_visual_review_required=True/False`
- Work focus: `top_issue`, `next_evidence_action`, `next_code_target`
- Retake plan: `retake_plan_status=ok`, `retake_plan_focused_retake_count`, `retake_plan_next_action`
- Retake plan doc: `retake_plan_doc_status=ok`, `documented_focused_retake_count / expected_focused_retake_count`
- PlayMode evidence preflight: `playmode_evidence_preflight_status=ready_for_focused_retake` 또는 `ready_for_visual_review`
- Manual registration candidates: `manual_registration_candidate_count`, `manual_registration_commands`
- Screenshot triage: `triaged_non_state_count`, `triaged_non_state_screenshots`
- Wave Combat action showcase: `wave_combat_action_showcase_ready=true`와 reason 확인
- Rhythm beat: 현재 작업이 개선하는 beat(`Read`, `Commit`, `Pressure`, `Payoff`, `Release`)
- Tension/release: spike overlap count, payoff visible time, release window 확인
- Planned spike: event/weather/boss/unlock/overheat/recipe overlap이 의도된 것인지 기록
- Manual record: `passed`, `needs_fix`, `blocked`, `not_recorded`, `invalid_record`
- 배치 성공률: `placed_success / place_attempt`
- blocked reason 분포: `out_of_bounds`, `occupied`, `invalid_anchor`, `no_pending`
- Draw pick rate: 카드 슬롯별 선택 비중(1/2/3)
- Draw -> Place 전환 시간(초)
- 웨이브당 과열(Overheat) 발생 횟수
- 웨이브 결과 요약 가독성: KO/damage, HP/Heat 변화, combo/leak 중 최소 1개 이상이 즉시 읽히는지
- 전장 비중: Draw/Pending/Wave 상태에서 푸드트럭과 좀비가 있는 영역이 화면의 50% 이상으로 체감되는지
- 공격 판독성: 공격 궤적, 피격 플래시, `-damage`/`KO`/`LEAK`/`TRUCK -HP` 표식만 보고도 맞은 대상과 HP 감소 원인을 이해할 수 있는지
- 드로우 카드 비교성: `Fit`, `Heat`, `Role`, `Value/Risk` 중 최소 3개가 3초 안에 비교되는지
- 배치 실패 회복성: 실패 후 다음 행동을 읽고 다시 시도할 수 있는지
- 배치 추천 설명성: 추천 슬롯이 어느 lane/coverage/center 이유로 좋은지 바로 이해되는지

## 데일리 체크리스트
### 디자인
- 오늘 카드 선택에서 "왜 이 카드를 골랐는지"가 3초 내 설명 가능한가?
- 실패 피드백 문구가 행동 수정에 직접 도움 되는가?
- review pack contact sheet만 보고도 Draw/Pending/Invalid/Wave의 주요 UI가 가려졌는지 판단 가능한가?
- 오늘 변경이 어떤 beat를 개선하는가: 읽기, 선택/배치, 압박, 보상, 회복 중 하나로 말할 수 있는가?
- wave/event/overheat/recipe가 동시에 울릴 때 의도된 spike인지, 우연히 겹친 소음인지 구분했는가?

### 개발
- 작업 전 `Tools\Show-PrototypeSessionStatus.ps1`로 suite/스크린샷/record 상태를 확인했는가?
- Unity를 열기 전 `Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1`로 focused retake readiness를 확인했는가?
- 단계 잠금(event/draw/pending)에서 누락된 입력 경로가 없는가?
- HUD 텍스트/배너/버튼 상태가 동일한 상태머신을 참조하는가?
- Draw/Pending/Invalid Placement HUD를 바꿨다면 `Tools\Verify-PrototypeHudStateContract.ps1`를 실행했는가?
- 레이아웃을 바꿨다면 `Tools\Verify-PrototypeLayout.ps1`와 focused retake를 실행했는가?
- Wave Combat를 바꿨다면 payoff cue가 로그뿐 아니라 화면 cue로도 보이는지 확인했는가?
- Draw Choice를 바꿨다면 tactical chip 문구가 compact portrait 카드에서 잘리지 않는지 확인했는가?
- Invalid Placement를 바꿨다면 `reason + next action` 문구가 cue/banner/pending hint에 같이 남는지 확인했는가?
- Pending Placement 추천을 바꿨다면 추천 이유가 raw score보다 먼저 보이는지 확인했는가?

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

### 2. Focused retake 계획 생성
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -PreviewOnly -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRetakePlan.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
```

### 3. Play Mode suite 캡처
- Unity Editor에서 Play Mode 진입
- `Tools > Food Truck Prototype > Capture Verification Suite`
- 일부 상태만 다시 찍을 때: `Tools > Food Truck Prototype > Prepare and Capture State > Draw Choice/Pending Placement/Invalid Placement/Wave Combat`
- standalone PNG만 있을 때: 아래 수동 등록 명령으로 해당 상태를 suite manifest에 연결

```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Register-PrototypePlayModeManualEvidence.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -State "Wave Combat" -ScreenshotPath "Docs\PlayModeScreenshots\foodtruck-playmode-20260504-010153.png" -PreviewOnly -JsonOnly
```

### 4. suite와 스크린샷 기계 검증
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeScreenshots.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
```

### 5. 시각 리뷰 pack 생성
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeReviewPack.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

### 6. 결과 기록
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS
powershell -ExecutionPolicy Bypass -File "Tools\Write-PrototypePlayModeResultFromSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -DrawChoice PASS -PendingPlacement PASS -InvalidPlacement PASS -WaveCombat PASS -Apply
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly
```

### 7. 코드 변경 후 회귀 가드
```powershell
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeHudStateContract.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"
powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -Strict -JsonOnly
powershell -ExecutionPolicy Bypass -File "Tools\Gate-Verification.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -RunTests -JsonOnly
```

## Latest Handoff
- Current detailed handoff: `Docs/Prototype_Session_Handoff.md`.
- Manual Play Mode verification sheet: `Docs/Prototype_PlayMode_Verification.md`.
- Rhythm design audit: `Docs/Prototype_RhythmDesign_Audit.md`.
- First status command: `powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- MCP unavailable fallback: local scripts/file inspection first; MCP 연결 문제로 completion-critical UX 검증을 멈추지 않는다.
- Immediate next validation: `Tools\Invoke-PrototypePlayModeEvidencePreflight.ps1`가 `ready_for_focused_retake`를 보고하면 Play Mode에서 focused retake 또는 `Capture Verification Suite`를 실행한 뒤 suite verifier, screenshot verifier, review pack, result writer 순서로 닫는다.
- Immediate next implementation after evidence closure: `Wave Cadence Composer`, unless review says payoff readability is the main blocker, in which case start with `Payoff-to-Read Panel`.
- Current fallback status: Wave Combat 코드 보완과 helper-state setup, suite capture/evidence verifier, screenshot quality verifier, manual PNG evidence registration, focused retake plan writer, action-showcase/review-readiness/next-focus-aware session status, review pack writer, suite-backed result writer는 준비되어 있다.
- Manual record remains open until Draw Choice, Pending Placement, Invalid Placement, Wave Combat suite evidence is captured, visually reviewed, and recorded.
