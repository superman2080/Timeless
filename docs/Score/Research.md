# Score — Research

## 1. 기존 UI 구성 (`Assets/01. Scenes/SampleScene.unity`)
- `===UI===` 아래 `Canvas` 1개. `Canvas` RectTransform 자식: `OctoombaScreenEffect`(fileID 2061374712), `HPBar`(52876968).
- Canvas: `m_RenderMode: 0`(Screen Space - Overlay), CanvasScaler `m_UiScaleMode: 0`(Constant Pixel Size, ReferenceResolution 800x600은 미사용).
- `HPBar`: 앵커 좌상단 (0,1), anchoredPosition (300, -100), scale 0.75. → 우상단은 비어 있음.

## 2. 텍스트 컴포넌트
- 게임 스크립트(`Assets/02. Scripts`)에서 TMP/Text를 쓰는 곳은 없음.
- `Title.unity`는 TextMeshPro(UGUI)를 사용 중. TMP 리소스(`TMP Settings.asset`, `LiberationSans SDF.asset`)는 `Assets/UnityTechnologies/ParticlePack/TextMesh Pro/Resources/`에 있음 → `TextMeshProUGUI` 바로 사용 가능.
- `Packages/manifest.json`: `com.unity.ugui 2.6.0` (TMP 포함).

## 3. 게임 진행/사망 흐름 (`Assets/02. Scripts/Player/Player.cs`)
- 별도 "게임 시작" 상태 없음. SampleScene 로드 = 게임 시작 (`Player.Start`에서 스탯/상태 초기화).
- 사망: `stat.onDied` → 글리치, 콜라이더 끔, `onPlayerDied` 채널 발행(`EventChannelSO`, guid `85711a2f47affb245ae1718e84ddd92f`), `Player.enabled = false`, 1.5초 후 `Restart()`가 SampleScene 재로드.
- 재로드 시 씬 오브젝트(Canvas 포함)가 새로 생성되므로 씬 오브젝트에 둔 점수는 자연히 0으로 리셋됨.

## 4. 이벤트 채널 (`Assets/02. Scripts/EventListener/`)
- `EventListener`: `eventChannel` + `UnityEvent response`. `OnEnable/OnDisable`에서 등록/해제.
- `HPBar`는 `EventListener` 상속. → 사망 시 점수 정지는 Score 오브젝트에 `EventListener`(채널 = onPlayerDied)를 붙이고 response에 `ScoreUI.enabled = false`를 인스펙터로 연결하면 코드 없이 가능.

## 5. 참고 패턴
- `OctoombaScreenEffect`: 씬 Canvas 아래 컴포넌트. `Singleton<T>`은 DDOL이라 씬 재로드 시 중복되므로 UI에는 쓰지 않음.

## 6. 영향 범위 / 위험
- 신규 스크립트 1개 + 씬에 UI 오브젝트 추가뿐. 기존 코드 변경 없음.
- SampleScene에 이미 미커밋 변경이 있음 → 씬 저장 시 해당 변경도 함께 저장됨(기존 변경은 건드리지 않음).
- 스케일 강조 시 pivot이 중앙이면 우측 정렬 텍스트가 오른쪽 화면 밖으로 커질 수 있음 → pivot을 우측 (1, 0.5)로.
