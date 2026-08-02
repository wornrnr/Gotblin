# AI Agent Unity 작업 워크플로우 가이드라인

이 문서는 사용자의 수정 요청 사항이 실제 게임 화면에 누락 없이 확실하게 반영되도록 하기 위해 AI Agent가 반드시 준수해야 하는 작업 규정입니다. 향후 프로젝트의 모든 수정/추가 작업 시 이 프로세스를 참고하여 진행합니다.

## 1. 사전 진단 및 환경 파악 (Pre-Check)
- **목적**: 코드 수정 전에 실제 씬(Scene) 및 프리팹(Prefab)의 상태와 계층 구조를 파악.
- **수행 항목**:
  1. 관련된 GameObject와 Prefab 상태 조회 (`gameobject-find`, `gameobject-component-get` 사용).
  2. Inspector에 연결된 컴포넌트 설정값 및 참조된 오브젝트가 `null`이 아닌지 점검.
  3. Transform(좌표, 크기) 및 부모-자식 계층 구조가 화면에 렌더링 될 수 있는 상태인지 점검 (예: UI Canvas와 월드 좌표계 구분).

## 2. 코드 및 에셋 동기화 수정 (Synchronized Update)
- **목적**: 코드(.cs) 변경 사항이 실제 유니티 에셋 데이터에 동기화되도록 조치.
- **수행 항목**:
  1. `script-update-or-create` 스킬로 코드 수정 후 **반드시** `assets-refresh`를 호출하여 Unity 에디터에 컴파일 반영.
  2. 스크립트에 public 또는 [SerializeField]로 추가/변경된 변수는 코드 레벨에만 방치하지 말고, `gameobject-modify`, `object-modify` 스킬을 사용해 **Inspector 값을 직접 주입하고 Reference를 연결**할 것.

## 3. 자체 검증 및 디버깅 (Self-Verification)
- **목적**: 사용자에게 결과물을 제시하기 전, 에이전트 스스로 런타임 에러 유무 및 시각적 동작을 검증.
- **수행 항목**:
  1. `editor-application-set-state`로 잠시 플레이 모드를 실행.
  2. `console-get-logs`를 통해 NullReferenceException 등 런타임 에러가 발생하지 않는지 체크.
  3. **[중요]** 크기, 위치, 이펙트 등 시각적인 변화를 동반한 작업인 경우, `screenshot-game-view` 또는 `screenshot-scene-view` 스킬로 스크린샷을 찍어 에이전트가 먼저 의도대로 렌더링되었는지 확인할 것.

## 4. 시각적 자료와 함께 결과 보고 (Result Reporting)
- **목적**: 사용자가 불필요한 테스트를 반복하지 않도록 확실한 증거 자료와 함께 피드백 요청.
- **수행 항목**:
  1. 에이전트 자체 검증에 실패(에러 발생, 스크린샷에 미표시)했다면 사용자에게 보고하지 않고 즉시 원인 파악 후 재수정(재시도) 진행.
  2. 검증에 통과했을 때만 사용자에게 리뷰를 요청하며, 이때 촬영한 **스크린샷이나 변경된 데이터 덤프 등 확실한 근거 자료**를 텍스트 보고와 함께 제공할 것.
