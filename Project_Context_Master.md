# 📜 [Project Gotblin] 개발 마스터 컨텍스트 명세서

## 1. 기본 개발 스펙 & 프로젝트 개요
- **개발 엔진**: Unity 6 (6000.0.42f1)
- **타겟 플랫폼**: 모바일 (Android / iOS) 세로형 화면 (Portrait, 1080x1920 / 9:16 기준)
- **그래픽 아키텍처**: 2D 레트로 픽셀 아트 스타일 (16비트 GBA/SNES 감성, PPU = 16, Filter Mode = Point)
- **UI 아키텍처**: 단일 씬(Single Scene) 구조 내 UGUI Canvas 기반 화면 제어. 모든 인게임 월드 연출 및 전투 역시 Canvas RectTransform 상대 좌표계 기반으로 구동 (물리 엔진 Rigidbody/Collider 배제).

---

## 2. 핵심 게임 플레이 루프 (Core Loop)
게임은 유기적으로 연결된 3대 코어 시스템으로 구성되며, 하단 내비게이션 바를 통한 단일 씬 내부 패널 스위칭(`MainScreenManager`) 방식으로 화면을 전환합니다. 화면이 꺼지더라도 모든 백그라운드 매니저는 틱(Tick) 및 타이머 연산을 중단 없이 수행합니다.

- **[코어 1] 소셜 그래프 콘텐츠 (잠입 도둑질)**:
  - 유저가 시작 버튼을 누르면 실시간으로 배수와 누적 골드가 상승합니다.
  - 시스템이 정한 '정지 시간' 도래 전에 '그만!' 버튼을 눌러 보상을 획득하는 하이리스크-하이리턴 미니게임입니다.
- **[코어 2] 부락 건설 시스템**:
  - 훔쳐온 재화를 소모하여 필드 마당에 건물을 건설/업그레이드합니다.
  - 오프라인 시간 보간(Offline Progression)을 지원하며 최대 3개의 다중 건설 슬롯을 제공합니다.
- **[코어 3] 자동 전투 및 훈련 시스템**:
  - 훈련소에서 유닛을 강화하고, 벨트 스크롤 사이드뷰 형식의 전장에서 영웅과 소형 고블린들이 적들을 자동으로 소탕하여 재화를 파밍하는 방치형 루프입니다.

---

## 3. 유니티 하이러키(Hierarchy) 및 컴포넌트 구조
현재까지 확정 및 빌드된 씬 오브젝트 구조와 데이터 흐름 레이어입니다. 코딩 시 이 계층 구조와 UI 패널 컴포넌트들의 상속 관계를 엄격히 준수해야 합니다.

```text
Canvas (Canvas Scaler: Scale With Screen Size, 1080x1920, Match: 0.5)
 │   ├── [Managers] (LocalizationManager, CombatManager, BuildingManager)
 │
 ├── 📁 GraphGamePanel (코어 1 잠입 패널)
 │    ├── TopPanel / ControlArea / BlueBanner (가변 안내 및 획득량 출력 패널)
 │    └── CenterContent (중앙 연출 창)
 │         ├── FanfareBg (가장 후면 집중선: GraphGameBgJuiceController 부착)
 │         └── Dragon & Goblin Images / Gold Effect Spawner
 │
 ├── 📁 TownBuildingPanel (코어 2 부락 패널: UI_FieldDragController 부착)
 │    ├── FarBackground (원경: UI_ParallaxLayer, Multiplier = 0.1)
 │    ├── MidGround (중경: UI_ParallaxLayer, Multiplier = 0.5)
 │    └── FieldContentRoot (근경 마당판: 전체 스크롤 영역의 절대 기준 좌표축)
 │         └── 📁 Building_HQ / Building_Hut / Building_Guild 등 (UI_WorldBuildingObject 부착)
 │
 ├── 📁 CombatPanel (코어 3 전투 패널: EnemySpawner 내장 오브젝트 풀 관리)
 │    └── CombatFieldContext (보스전 시 좌측 이동 스크롤 축, 스폰 부모)
 │         ├── BattleBackground (벨트 스크롤용 가로 확장 배경 이미지)
 │         ├── [HeroGoblin] (모듈러 비주얼 및 AI 유닛 핵심 코어 루트)
 │         └── [Enemies / Spawns] (동적 스폰 개체 레이어)
 │
 └── 📁 BottomNavigation (최하단 메인 메뉴 바, Canvas Group 컴포넌트 내장)
```

---

## 4. 기획 핵심 규칙 및 예외 처리 정책 (QA & Policy)

### ① 코어 1: 이산 가중치 추첨 및 텐션 스케일링
- **확률 구조**: 정지 시간은 상대 가중치(Relative Weight) 시스템으로 작동하는 ScriptableObject 기반 데이터 테이블(`GraphGameTableData`)에서 런타임 시작 시 1개의 특정 후보 시간(Discrete Time)을 가중치 추첨하여 확정 고정합니다.
- **시도 횟수 제한**: 판당 결과와 관계없이 시작(`StartRound`) 즉시 시도 횟수(남은 고블린 수)가 1 차감되어 강제 종료 어뷰징을 차단합니다. 0회 시 시작 버튼이 잠깁니다.
- **시각 연출**: 게임 진행 시간이 길어질수록 `UnityEngine.Pool` 기반의 골드 파티클이 고블린 꽁무니에서 폭발적으로 증가하며, 후면 `FanfareBg`는 시간 비례 가속(Max Alpha: 0.3 제한) 렌더링을 유지합니다. 라운드 종료 즉시 상태가 동결(Freeze)되며 재도전 시 즉시 투명도 0으로 리셋됩니다.
- **내비게이션 락**: 게임이 진행 중(Running)일 때는 하단 메뉴 패널 전체의 `CanvasGroup.interactable`을 `false`로 차단하여 보상 증발을 방지합니다.

### ② 코어 2: 패럴랙스 카메라 및 다중 건설 데이터
- **시차 상속 차단**: 입체적 공간감을 구현하는 패럴랙스 시스템에서 `FarBackground`와 `MidGround`는 기준판인 `FieldContentRoot`와 자식 관계가 아닌 동등한 형제 관계로 배치하여 이동량이 중복 상속되는 물리적 모순(상속 폭주)을 배제합니다.
- **세이브 및 오프라인 보간**: `System.DateTime.UtcNow` 기반으로 앱 종료/포커스 아웃 순간을 기록하여 복귀 시 경과 시간(`elapsedSeconds`)을 계산합니다. 이 경과 시간은 현재 건설 중인 모든 활성 슬롯(최대 3개, BM 연동 가능)에 동일하게 분배 적용됩니다.
- **최고 레벨 방어**: `BuildingData` 인덱스의 리스트를 초과하는 업그레이드가 요청될 경우, `IndexOutOfRangeException`을 방어하고 UI 버튼을 "MAX"로 잠급니다. 레벨 0(미건설) 시에는 0레벨 스프라이트를 노출하되 비용은 1레벨 건설 비용을 파싱합니다.

### ③ 코어 3: 모듈러 비주얼 및 수치 기반 전투 FSM
- **데이터 중심 연출 분리**: 배속 시스템 및 오프라인 전투 시뮬레이션 확장을 위해, 유닛의 FSM(대기, 이동, 공격, 사망)은 물리 충돌이나 애니메이션 프레임에 의존하지 않고 오직 픽셀 거리 계산과 타이머 수치로만 판정합니다.
- **모듈러 계층 구조 및 피벗 추적**: 영웅 고블린 본체는 이미지가 없으며 자식인 `BodyVisual`에서 스킨 이미지와 애니메이터를 제어합니다. 무기인 `WeaponVisual`은 `BodyVisual` 내부의 `Hand_Anchor` 오브젝트의 월드 좌표와 회전값을 `LateUpdate()`에서 실시간 동기화 추적하여 불규칙한 팔 움직임 궤적에 자석처럼 결합합니다. 적의 위치(X축)에 따라 루트의 `localScale.x` 부호를 스위칭하여 반전 처리를 제어합니다.
- **덩치 반경(Body Radius) 포메이션**: 물리적 유닛 겹침을 방지하기 위해 `진짜 공격 가능 거리 = 내 사거리 + 상대방의 덩치 반경(Body Radius)` 공식을 적용합니다. 아울러 생성 시 부여받은 고유 진형 오프셋(`attackPositionOffset`)을 연산하여 포위망 형태로 집결하며, 목적지 도착 시 데드존 처리를 적용해 스프라이트의 덜덜거림(Jittering)을 원천 차단합니다.
- **무적 및 바운더리 클램프**: 방치 모드(`IdleMode`)에서는 적이 아군을 공격할 때 가해지는 데미지를 0으로 치환하는 무적 예외 처리를 유지합니다. 유닛이 피격 넉백(0.15초 매직 코루틴)될 때 맵 테두리 밖으로 튕겨 나가는 현상은 `GetWorldCorners` 기반의 Y축 절대 경계선 `Mathf.Clamp`로 차단합니다.
- **오브젝트 풀링 컴포넌트**: 무한히 스폰되는 일반 몬스터는 `Queue<GameObject>` 기반의 오브젝트 풀로 관리됩니다. 풀에서 꺼내 재활용되는 순간 체력 완충, 특수 이동 해제, 붉은 피격 색상 및 크기 원상복구(4대 리셋 규칙)를 보장하는 `ResetUnitStateForReuse()`를 호출합니다.

### ④ 다국어 번역(Localization) 파싱
- 데이터 관리 편의성을 위해 `Assets/Resources/LocalizationTable.csv` 경로의 파일을 로드합니다.
- 번역 텍스트 내 콤마(,) 에러를 방지하기 위해 따옴표(`""`)로 둘러싸인 콤마를 안전하게 예외 처리하는 정규식(Regex) 파싱 규칙을 사용합니다.
- UI 컴포넌트 `LocalizedTextTMP`는 인스펙터에 등록된 고유 스트링 Key를 기반으로 매핑 텍스트를 파싱하여 적용합니다.

---

## 5. Antigravity IDE 핵심 코딩 지침 (Strict Coding Rules)
1. **싱글톤(Singleton) 타이밍 엄수**: 모든 전역 매니저 클래스의 인스턴스 초기화는 `Awake()` 단계에서 완결되어야 하며, 유닛 컴포넌트가 자신을 매니저 주소록 리스트에 등록/해제하는 타이밍은 `Start()` 및 `OnDestroy()` 단계로 분리하여 런타임 Null 참조 순서 꼬임을 원천 봉쇄합니다.
2. **좌표계 분리 인지**: `anchoredPosition`(로컬 UI 좌표) 연산 구문과 `transform.position`(월드 좌표) 연산 구문이 혼용되어 스케일 크래시가 나지 않도록 데이터 타입을 명확히 세팅해야 합니다.
3. **방어적 프로그래밍**: UI 컴포넌트나 백엔드 데이터 테이블이 유니티 인스펙터 상에서 예기치 않게 비어있거나(null) 누락되어도 게임 전체 루프가 멈추거나 튕기지 않도록 모든 참조부에 안전 래핑 및 예외 로그 처리를 보장해야 합니다.
