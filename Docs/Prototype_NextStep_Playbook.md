# 프로토타입 다음 단계 플레이북

## 이번 스프린트 목표
- UX 절차 안정화: Draw -> Choose -> Rotate -> Place 흐름을 끊김 없이 유지
- 선택 재미 강화: Draw 3카드에서 "즉시 비교"가 가능하도록 EV/Risk 정보 제공
- 연출 보강: 배치 성공/실패에 즉각적인 시각 피드백과 큐 배너 연결

## 우선순위
### P0 (이번 주 필수)
- 배치 실패 사유를 HUD에 직접 노출 (경계 초과/점유/잘못된 앵커)
- 이벤트/드로우 단계에서 비유효 입력 잠금 유지
- Draw 카드 텍스트에 EV/Risk 더미 수치 표기

### P1 (다음 주)
- 배치 성공/실패 마이크로 애니메이션 프리셋 확장
- Draw 카드 시각 언어 통일 (위험도에 따른 컬러/아이콘)
- 체크리스트 문구를 플레이 로그와 동기화

### P2 (중기)
- 3웨이브 단위 해금 구조(타겟팅/형태/열관리) 도입
- 연출 트리거 표준화(이벤트 선택, 배치, 콤보 버스트)
- 간단 텔레메트리 저장/리포트 자동화

## 측정 지표
- 배치 성공률: `placed_success / place_attempt`
- blocked reason 분포: `out_of_bounds`, `occupied`, `invalid_anchor`, `no_pending`
- Draw pick rate: 카드 슬롯별 선택 비중(1/2/3)
- Draw -> Place 전환 시간(초)
- 웨이브당 과열(Overheat) 발생 횟수

## 데일리 체크리스트
### 디자인
- 오늘 카드 선택에서 "왜 이 카드를 골랐는지"가 3초 내 설명 가능한가?
- 실패 피드백 문구가 행동 수정에 직접 도움 되는가?

### 개발
- 단계 잠금(event/draw/pending)에서 누락된 입력 경로가 없는가?
- HUD 텍스트/배너/버튼 상태가 동일한 상태머신을 참조하는가?
- Play Mode 입력이 막힌 환경에서는 `Tools\Verify-PrototypeHudStateContract.ps1`로 Draw/Pending/Invalid Placement 상태 계약을 먼저 고정했는가?

### 플레이테스트
- 세로 폰 해상도에서 배치 가능한 영역이 충분한가?
- 5분 플레이 중 막힘 구간(반복 실패/의미 없는 대기)이 기록되는가?
- 실패 로그 상위 2개 원인을 다음 작업으로 연결했는가?

## Latest Handoff
- Current detailed handoff: `Docs/Prototype_Session_Handoff.md`.
- Manual Play Mode verification sheet: `Docs/Prototype_PlayMode_Verification.md`.
- First status command: `powershell -ExecutionPolicy Bypass -File "Tools\Show-PrototypeSessionStatus.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- MCP unavailable fallback: continue with local scripts/file inspection first; do not block completion-critical UX work on MCP connectivity.
- HUD state contract command: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeHudStateContract.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- Layout guard command: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeLayout.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- Play Mode suite evidence command: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeSuite.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- Play Mode record command: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypePlayModeRecord.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`.
- Low-interaction capture path: in Play Mode use `Tools > Food Truck Prototype > Capture Verification Suite` to batch Draw Choice, Pending Placement, Invalid Placement, and Wave Combat evidence.
- Focused fallback path: use `Tools > Food Truck Prototype > Prepare and Capture State` when only one state needs a retake.
- Immediate next validation when Play Mode input is reliable again: run the suite verifier, review the captures visually, and record the manual result.
- Current code fallback is complete for the captured Wave combat issue, and Draw/Pending/Invalid/Wave helper-state setup plus suite capture/evidence verification are now gate-checked while manual record remains open.
- PASS recording now reuses saved suite screenshot evidence, so the manual record can survive Editor reloads between suite capture and PASS confirmation.
