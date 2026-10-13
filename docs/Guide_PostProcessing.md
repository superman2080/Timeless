# 포스트 프로세싱 현황

에셋에서 직접 읽은 값이다(2026-10-02 기준). 설계 근거는 CLAUDE.md §12 · §12-1 · §14 · §11-8을 본다.

## 1. 파이프라인 공통 (URP RP Asset)

| 항목 | PC_RPAsset | Mobile_RPAsset |
|---|---|---|
| HDR | 켬 (정밀도 32bit) | 켬 |
| MSAA | 끔 | 끔 |
| Render Scale | 1.0 | **0.8** |
| Color Grading | LDR, LUT 32 | LDR, LUT 32 |
| Fast sRGB 변환 | 끔 | 켬 |
| 기본 Volume Profile | `SampleSceneProfile` | `SampleSceneProfile` |

`SampleSceneProfile`이 RP 에셋의 기본 프로파일이라 **Volume이 없는 씬(`SongSelectScene`·`Explore_Stage1`)에도 적용된다.** `DefaultVolumeProfile`은 전부 중립값(효과 0)이다.

## 2. 씬 Volume

| Volume | 프로파일 | Priority | Weight | 있는 씬 |
|---|---|---|---|---|
| `Global Volume` | `SampleSceneProfile` | 0 | 1 | BattleScene · Tutorial |
| `FilmLookVolume` | `FilmLookProfile` | 5 | 1 | BattleScene만 |
| `ComboPostFxVolume` | `ComboPostFxProfile` | 10 | 0 (런타임) | BattleScene · Tutorial |

### SampleSceneProfile (상시 룩)
- **Bloom** — Threshold 1.0 · Intensity 0.25 · Scatter 0.5 · High Quality Filtering 켬
- **Tonemapping** — ACES · (HDR 출력용) Paper White 234 · Min/Max Nits 0.005 / 647
- **Vignette** — 검정 · Intensity 0.2
- **Motion Blur** — Quality High · Intensity 0.6 · ⚠ 컴포넌트가 **비활성**이라 실제로는 꺼져 있다

### FilmLookProfile (채도 저하)
- ⚠ **비어 있다**(`components: [{fileID: 0}]`) — 커밋 `0d3ca41b` 시점부터 그렇다. CLAUDE.md §12-1이 말하는 `ColorAdjustments`(Saturation −25 · Contrast +5 · Post Exposure −0.05)는 **현재 적용되지 않는다.** 다시 넣어야 한다.

### ComboPostFxProfile (콤보 단계 연출, `ComboPostFxView`)
- **Vignette** — 색 (0.60, 0.05, 0.05) · Intensity 0.42 · Smoothness 0.55
- **Chromatic Aberration** — Intensity 0.25
- Weight는 코드가 민다: 단계별 `tierWeights = {0, 0.35, 0.65, 1}`, 상승 감쇠 `riseDamp` 0.25초, 콤보가 끊기면 즉시 0.

## 3. Renderer Feature

PC_Renderer (리스트 순서 = 실행 순서 판단 기준):

| # | 이름 | 종류 | 활성 | 값 |
|---|---|---|---|---|
| 1 | ScreenSpaceAmbientOcclusion | SSAO | 켬 | Depth Normals · Intensity 0.4 · Radius 0.3 · Direct Lighting 0.25 · Falloff 100 |
| 2 | CharacterOutline | RenderObjects | **끔** | 레이어 8 |
| 3 | AmbushOutline | RenderObjects | 켬 | 레이어 9(`AmbushOutline`) · AfterOpaques · 깊이 상태 안 건드림 · 머티리얼 `_OutlineColor` (1, 0.35, 0.25) · `_OutlineWidth` 0.035 |
| 4 | WatercolorFx | VolFx | 켬 | Volume 쪽 `WatercolorVol`이 어느 프로파일에도 없어 **사실상 무효** |
| 5 | FlowFx | VolFx | 켬 | 위와 같음(`FlowVol` 없음) |
| 6 | FinaleBackground | RenderObjects | 끔(런타임 켬) | 레이어 10·9 제외 전부 · 빨강 (0.85, 0.03, 0.03) |
| 7 | FinaleActors | RenderObjects | 끔(런타임 켬) | 레이어 `Silhouette`(10) + `SlicePiece`(8) · 검정 |
| 8 | ToonPosterize | FullScreenPass | 켬 | AfterPostProcessing · Fetch Color Buffer 켬 · `_Steps` 8 · `_Strength` 1 · `_Softness` 0.02 · `_PosterizeRGB` 0 |
| 9 | FilmScratch | FullScreenPass | 켬 | AfterPostProcessing · Fetch Color Buffer 끔 · 아래 머티리얼 값 |

`AnimeFilmScratchMaterial`: `_Columns` 220 · `_ScratchesPerSecond` 0.6 · `_FlickerRate` 12 · `_EdgeStart` 0.62 · `_Intensity` 0.35 · `_LineWidth` 0.25 · `_LineColor` (0.92, 0.92, 0.88) · `_Density` 0.02(미사용 잔재로 보임).

Mobile_Renderer: FinaleBackground → FinaleActors → ToonPosterize → FilmScratch **넷뿐**이다. ⚠ SSAO·AmbushOutline이 없어 **모바일에서는 기습자 아웃라인이 안 보인다.**

## 4. 런타임에 바뀌는 것

| 시점 | 변화 | 주인 |
|---|---|---|
| 콤보 단계 변화 | `ComboPostFxVolume.weight` 0 → 0.35 / 0.65 / 1 | `ComboPostFxView` |
| 기습 텔레그래프 | 기습자 레이어 → 9 (아웃라인) | `EnemyView.SetHighlight` |
| 마무리 실루엣 | Finale 피처 2장 켬 · `FilmLookVolume.weight` 0 · FilmScratch 끔 · `timeScale` 0.1 · 노출 1초(unscaled) | `FinaleSilhouetteDirector` |

## 5. 다른 프로젝트로 옮기기 (상시 룩만)

"상시"는 게임 상태와 무관하게 늘 걸려 있는 것이다 — **Bloom · Tonemapping · Vignette · SSAO · 툰 단색화 · 필름 세로선.** 콤보 Volume·기습 아웃라인·마무리 실루엣은 이 게임 코드에 묶여 있어 뺀다. WatercolorFx·FlowFx는 실제로 아무 일도 안 하므로 옮기지 않는다(옮기면 외부 에셋 `99. External Assets/Watercolor`까지 딸려 온다). `FilmLookProfile`은 비어 있어 옮길 것이 없다.

### 5-1. 전제 조건
- Unity **6000.3.x** · URP **17.3.0** (`FullScreenPassRendererFeature`는 URP 14+ 내장. 그 아래 버전이면 셰이더 include 경로부터 깨진다)
- Color Space **Linear** — ⚠ 툰 단색화 셰이더가 선형→sRGB→양자화→선형을 전제로 짜여 있다. Gamma 프로젝트에서는 단계가 어긋난다.
- 렌더 경로 **Forward+** (`PC_Renderer` Rendering Mode). Forward여도 결과는 같다.

### 5-2. 복사할 파일 (meta 포함 — GUID가 유지돼야 머티리얼→셰이더 참조가 산다)
| 파일 | 용도 |
|---|---|
| `Assets/Shaders/AnimeToonPosterize.shader` | 툰 단색화 (`Hidden/AnimeToonPosterize`) |
| `Assets/Shaders/AnimeFilmScratch.shader` | 필름 세로선 (`Hidden/AnimeFilmScratch`) |
| `Assets/Shaders/Materials/AnimeToonPosterizeMaterial.mat` | 위 셰이더의 값 |
| `Assets/Shaders/Materials/AnimeFilmScratchMaterial.mat` | 위 셰이더의 값 |
| `Assets/Settings/SampleSceneProfile.asset` | Bloom · Tonemapping · Vignette |

C# 스크립트는 **하나도 필요 없다.** 셰이더 두 개는 URP·Core 패키지 include만 쓴다.

> 렌더러 에셋(`PC_Renderer.asset`)을 통째로 복사하지 않는다 — Finale·Ambush·Watercolor 피처와 이 프로젝트 레이어(8·9·10) 참조가 따라와 대상 프로젝트에서 깨진다. 아래처럼 피처만 새로 추가한다.

### 5-3. URP Asset 설정 (대상 프로젝트의 RP Asset)
- HDR **켬**(정밀도 32bit) · MSAA **끔** · Render Scale 1.0(모바일 0.8)
- Post Processing → Grading Mode **LDR**, LUT Size **32**
- Depth Texture **켬** · Opaque Texture **켬**
- **Volume Profile = `SampleSceneProfile`** — 여기에 꽂으면 씬마다 Volume을 안 놓아도 모든 씬에 걸린다(이 프로젝트 방식). 씬별로 다르게 하려면 대신 Global Volume(priority 0, weight 1)에 꽂는다.

### 5-4. Renderer에 피처 추가 (순서가 곧 설계다)
1. **Screen Space Ambient Occlusion** — Method Interleaved Gradient · Source **Depth Normals** · Normal Quality Medium · Intensity **0.4** · Radius **0.3** · Falloff **100** · Direct Lighting Strength **0.25** · Samples Medium · Blur High · Downsample 끔 · After Opaque 끔
2. **Full Screen Pass** 이름 `ToonPosterize` — Material `AnimeToonPosterizeMaterial` · Injection **After Rendering Post Processing** · **Fetch Color Buffer 켬** · Requirements None
3. **Full Screen Pass** 이름 `FilmScratch` — Material `AnimeFilmScratchMaterial` · Injection **After Rendering Post Processing** · **Fetch Color Buffer 끔** · Requirements None

⚠ 2가 3보다 **위**여야 한다 — 같은 주입 지점이라 리스트 순서만이 실행 순서를 정하고, 뒤집히면 세로선까지 계단화된다.
⚠ 렌더러를 여러 개 쓰면(PC/Mobile) 각 렌더러에 같은 이름으로 넣는다. 원본 모바일 렌더러는 SSAO가 없다.

### 5-5. 값 (복사로 따라오지만 수동으로 만들 때 기준)
- `SampleSceneProfile`: Bloom(Threshold 1 · Intensity 0.25 · Scatter 0.5 · HQ Filtering 켬) · Tonemapping **ACES** · Vignette(검정 · 0.2). Motion Blur는 비활성이라 빼도 된다.
- `AnimeToonPosterizeMaterial`: Steps 8 · Strength 1 · Softness 0.02 · PosterizeRGB 0
- `AnimeFilmScratchMaterial`: Columns 220 · ScratchesPerSecond 0.6 · FlickerRate 12 · EdgeStart 0.62 · Intensity 0.35 · LineWidth 0.25 · LineColor (0.92, 0.92, 0.88)

### 5-6. 카메라
- Rendering → **Post Processing 켬** (빠지면 Volume 효과가 전부 안 나온다. Full Screen Pass 둘은 이 체크와 무관하게 돈다)
- Anti-aliasing 없음 · Dithering 끔 · Stop NaN 끔 · HDR Output 허용

### 5-7. 확인
- 화면이 8단계 밝기 띠로 보이고 가장자리에 1~2초마다 세로선이 뜨면 성공.
- 화면이 통째로 검어지면 Color Space가 Gamma이거나 HDR이 꺼진 것이다.
- 세로선이 계단처럼 끊겨 보이면 피처 순서가 뒤집힌 것이다.

## 6. 확인이 필요한 것
1. `FilmLookProfile`이 비어 있다 — 채도 저하가 안 걸린다.
2. Tutorial 씬에 `FilmLookVolume`이 없다 — 의도인지 확인.
3. WatercolorFx · FlowFx 피처는 켜져 있지만 볼륨 값이 없어 매 프레임 비용만 남을 수 있다 — 안 쓰면 끈다.
4. Motion Blur 컴포넌트가 비활성 상태로 프로파일에 남아 있다.
