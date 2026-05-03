# 재미난 게임 제작 리소스 명세 (Zombie Foodcenter 기준)

## 1) 목적
- 목표: `10초 이해`, `30초 긴장`, `5분 성장`이 체감되는 플레이 루프 완성.
- 기준 루프: `Draw 3 -> Choose 1 -> Rotate/Place -> Wave 대응 -> 다음 선택`.
- 원칙: 불필요한 UI 노출을 줄이고, 현재 단계에서 필요한 정보만 보이게 한다.

## 2) 핵심 리소스 맵 (필수 우선)

| 영역 | 필수 리소스 | 완료 기준 |
|---|---|---|
| 게임 디자인 | 웨이브 구조표, 카드 풀 표, 배치 실패 사유 표준 문구 | 플레이어가 실패 원인을 1초 내 이해 |
| 시스템/코드 | 상태머신(phase gating), 배치 판정기, 실패 이유 로깅 | 잘못된 입력 경로 0건 |
| UX/UI | 단계별 HUD 노출 규칙, 카드 EV/Risk 라벨, 배치 피드백 텍스트 | 강한 상황인식(지금 해야 할 행동이 명확) |
| 2D/3D 아트 | 타일/배치 프리뷰, 카드 아이콘, 적/트럭 핵심 실루엣 | 오인식 없는 시각 구분 |
| 사운드 | 배치 성공/실패, 과열, 웨이브 시작 큐 | 행동-결과 연결이 청각으로 강화 |
| QA/밸런싱 | 핵심 KPI 수집, blocked reason 통계, 5분 플레이 로그 | 막힘 구간 재현/개선 가능 |
| 툴링/운영 | 검증 스크립트, 상태 리포트(JSON), 실패 분류 게이트 | 매일 같은 방식으로 품질 확인 |

## 3) UX 노출 전략 (불필요 UI 제거)

| 게임 상태 | 보여줄 것 | 숨길 것 |
|---|---|---|
| EventPending | 이벤트 선택지, 선택 입력 가이드 | 배치 상세 패널, 과열 세부 패널 |
| DrawPhase | 3카드 비교(EV/Risk/역할) | 웨이브 진행 관련 불필요 버튼 |
| PendingPlacement | 배치 가능 영역, 실패 사유, 회전 안내 | 이벤트/드로우 관련 컨트롤 |
| WaveCombat | 웨이브 핵심 정보(압력/열/생존) | 카드 상세 툴팁, 편집형 UI |
| RestPhase | 다음 웨이브 준비/정리 UI | 전투 중 전용 경고 UI |

- 규칙:
  - 한 시점의 “핵심 행동”은 1개만 강조.
  - 실패 피드백은 타겟 근처(그리드 우선) + 짧은 텍스트 동시 제공.
  - 상단 HUD는 고정 정보 최소화, 상태별 컨텍스트 정보만 노출.

## 4) 콘텐츠/에셋 체크리스트

### P0 (즉시 필요)
- 카드 3종 기본 프레임 + EV/Risk 뱃지 스프라이트.
- 배치 성공/실패 피드백용 이펙트 2종(간단 애니메이션 포함).
- 실패 사유별 아이콘 4종: `occupied`, `out_of_bounds`, `invalid_anchor`, `no_pending`.
- 사운드 5종: 배치 성공/실패, 웨이브 시작, 과열, 버스트 준비.

### P1 (다음)
- 적 타입 시각 구분 강화(실루엣/컬러 레벨링).
- 카드 희귀도/전략 태그 아이콘 확장.
- 콤보/버스트 타이밍용 리듬형 사운드 큐.

### P2 (확장)
- 웨이브 구간별 테마 변형(배경/조명/사운드 레이어).
- 고급 트럭/유닛 스킨(가독성 유지 전제).

## 4-1) 리소스 점검 명령
- 기본 점검: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`
- JSON 점검: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -JsonOnly`
- 최종 아트 누락까지 실패 처리: `powershell -ExecutionPolicy Bypass -File "Tools\Verify-PrototypeAssets.ps1" -ProjectPath "D:\uni\zombieFoodcenter" -Strict`
- PNG `.meta` 보강: `powershell -ExecutionPolicy Bypass -File "Tools\Ensure-PrototypeAssetMetas.ps1" -ProjectPath "D:\uni\zombieFoodcenter"`
- `needs_art`는 현재 더미/텍스트 fallback으로 게임이 실행 가능하지만, 최종 푸드트럭/주방/음식 아이콘 PNG가 아직 비어 있다는 뜻이다.
- `diagnostic_warnings`는 PNG 크기, 알파 채널, `.meta` 누락이 권장값과 다를 때 증가한다. 기본 게이트는 경고로만 보고, 파일 누락/런타임 필수 리소스 누락만 실패 처리한다.

## 5) 인력/역할 권장

| 역할 | 핵심 책임 |
|---|---|
| 게임 디자이너 | 카드/웨이브/열관리 규칙 설계, 밸런스 목표 정의 |
| 클라이언트 프로그래머 | phase gating, 배치 판정, HUD 상태 동기화 |
| UX/UI 디자이너 | 상태별 정보 우선순위, 노출/비노출 정책 |
| 2D/테크 아티스트 | 카드/아이콘/이펙트/프리뷰 제작, 스타일 통일 |
| 사운드 디자이너 | 행동-결과 연결 SFX, 긴장 리듬 설계 |
| QA/분석 | blocked reason 분포, 성공률/전환시간 측정, 회귀 테스트 |

## 6) 검증 지표 (재미 체감용)
- `pending_to_place_success_rate`
- `blocked_place_reason_count` (사유별)
- `draw_choice_pick_rate` (카드 슬롯/태그별)
- `draw_to_place_time_sec`
- `overheat_events_per_wave`

## 7) 실행 우선순위 (1주)
1. 배치 실패 사유 노출 + 근접 피드백 완성.
2. Draw 카드 EV/Risk 시각화(더미 값 포함) 완성.
3. 상태별 UI 노출 규칙 강제(phase gating 누수 제거).
4. 성공/실패 마이크로 피드백(애니+사운드) 연결.
5. KPI 로깅 및 리포트 자동 확인 루틴 고정.

## 8) 리스크와 대응
- 리스크: 정보 과다로 인한 판단 지연.
  - 대응: 상태별 UI 화이트리스트 운영.
- 리스크: 실패 원인 불명확으로 반복 이탈.
  - 대응: 실패 사유 코드/문구/아이콘 1:1 매핑.
- 리스크: 팀 작업 분리로 UX 불일치.
  - 대응: “상태별 노출 표”를 단일 기준 문서로 고정.
