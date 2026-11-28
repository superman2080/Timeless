# MarioHorrorUI — 적용 요약

> **구현 현황 (2026-11-28)**: 완료 — 공식 무드보드(`어두움 · 긴박함 · 신비함`) 6색 팔레트(`#17171B`, `#263C35`, `#42645A`, `#B99A55`, `#553B32`, `#633A48`) 기준으로 8종 스프라이트 재제작 및 반영 완료 (C# 코드 변경 0건, 컴파일/콘솔 에러 0건).

## 컬러 팔레트 (공식 무드보드 기준)
- 검정 `#17171B` / 짙은 녹색 `#263C35` / 녹청 `#42645A` — 패널·버튼·토관·벽돌·안개 톤
- 금색 `#B99A55` / 갈색 `#553B32` — 브래스 프레임·코너 리벳·상단 한 줄기 빛 베벨·슈퍼스타·부식 벽돌
- 와인색 `#633A48` — `BulletBillWarning` 경고 블록 본체, `HPIcon` 버섯 갓, `HPFrame` 50% 마커

## 추가된 스프라이트 (`Assets/10. Sprites/MarioHorrorUI/`)

| 에셋 | 크기 | Type / 9-Slice Border | 용도 |
|---|---:|---|---|
| `UI_MarioHorror_Panel.png` | `384×384` | `Sliced` `(48, 48, 48, 48)` | `SettingsMenu` `MenuPanel/Box`, `ConfirmPopup/Box` |
| `UI_MarioHorror_Button.png` | `256×64` | `Sliced` `(28, 14, 28, 14)` | `SettingsMenu` 전체 버튼(12개) + `Title` 시작/설정 버튼(2개) |
| `UI_MarioHorror_PipeTrack.png` | `256×32` | `Sliced` `(20, 4, 20, 4)` | `SettingsMenu` 볼륨 슬라이더 배경 트랙(3개) |
| `UI_MarioHorror_HPFill.png` | `512×64` | `Sliced` `(6, 4, 6, 4)` | `GameScene` `HPBar/Fill` + `SettingsMenu` 볼륨 슬라이더 Fill(3개) |
| `UI_MarioHorror_Knob.png` | `128×128` | `Simple` (`Preserve Aspect`) | `SettingsMenu` 볼륨 슬라이더 Handle(3개, 저주받은 슈퍼스타) |
| `UI_MarioHorror_Warning.png` | `256×256` | `Simple` (`Preserve Aspect`) | `GameScene` `BulletBillWarning/Root/Icon` (금 간 `!` 블록) |
| `UI_MarioHorror_HPFrame.png` | `668×104` | `Simple` | `GameScene` `HPBar/Background` (50% `threshold2D` 붉은 마커 포함) |
| `UI_MarioHorror_HPIcon.png` | `256×256` | `Simple` (`Preserve Aspect`) | `GameScene` `HPBar/Icon` (저주받은 버섯 원형 문장) |

## 수정된 프리팹 / 씬 (스크립트 변경 없음)
- [x] `Assets/07. Prefabs/UI/SettingsMenu.prefab` — `Box`(2개, `pixelsPerUnitMultiplier = 1.25`), 버튼(12개, 아이보리 텍스트 `#F2E6C7`), 볼륨 슬라이더(3개, 높이 34 / Handle 폭 36 / 토관 트랙 + 슈퍼스타 노브)
- [x] `Assets/01. Scenes/Title.unity` — `Canvas/Button (Legacy)`, `Canvas/SettingsButton` (`UI_MarioHorror_Button`, `Sliced`, `pixelsPerUnitMultiplier = 0.75`, 아이보리 텍스트 `#F2E6C7`) + `SettingsMenu` 프리팹 자동 반영
- [x] `Assets/01. Scenes/GameScene.unity` — `Canvas/HPBar` (`Background`/`Fill`/`Icon`), `Canvas/BulletBillWarning` (`Icon` 교체, `Backdrop` 비활성화) + `SettingsMenu` 프리팹 자동 반영

## 검증 (Play 모드)
- [x] `Title` 시작 화면 버튼 + `Settings` 클릭 메뉴 열기 확인
- [x] `Title` → `Press to Start` → `GameScene` Addressables 전환 후 HUD(`HPBar`, `BulletBillWarning`) + `SettingsMenu` 확인
- [x] 컴파일/콘솔 에러 `0`, 비의도적 Play diff(`PC_Renderer`, `GlitchMetetrial`, `NotoSansKR-VF SDF`) 복원 확인
