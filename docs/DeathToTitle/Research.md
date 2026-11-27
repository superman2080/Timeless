# 사망 시 타이틀로 이동 — Research

요청: 플레이어가 죽으면 게임 씬을 다시 불러오지 말고 Title 씬으로 보낸다.

## 현재 사망 흐름 (`Assets/02. Scripts/Player/Player.cs`, CP949 + CRLF)

`Player.Start`(:140~)에서 `stat.onDied`에 다음을 등록한다.
```csharp
stat.onDied += (player) => {
    CameraManager.Instance.FadeGlitch(0.01f, 0.9f, 1f);
    Col.enabled = false;
    Invoke("Restart", 1.5f);
};
stat.onDied += (player) => onPlayerDied.RaiseEvent();
stat.onDied += (player) => enabled = false;
```
`Restart()`(:373)는 아래와 같다.
```csharp
private void Restart()
{
    CameraManager.Instance.SetGlitch(0.02f);
    CameraManager.Instance.SetPixelateIntensity(1);
    UnityEngine.AddressableAssets.Addressables.LoadSceneAsync("SampleScene");
}
```
- `Invoke`는 scaled time 기준이다. 사망 후 1.5초 동안 글리치 연출이 진행된 뒤 씬을 로드한다.
- `onPlayerDied` 채널: `GameScene`에서 `ScoreUI`의 EventListener 등이 받는다. 점수 정지 용도이며 이번 변경과 무관하다.
- `Restart`를 문자열로 참조하는 곳은 위의 `Invoke("Restart", ...)` 하나뿐이다. 씬/프리팹/애니메이션 이벤트에서는 참조하지 않는다(grep으로 확인).
  - `OctoombaScreenEffect.Restart`는 이름만 같은 별개 메서드다.

## 이미 있는 "타이틀로 가기" 코드: `SettingsMenuUI.ExitToTitle()`
```csharp
Stack.Clear();
Time.timeScale = 1f;
CameraManager.Instance.SetPixelateIntensity(1);
CameraManager.Instance.SetGlitch(0.002f);
PlayerPrefs.Save();
Addressables.LoadSceneAsync(titleSceneName); // "Title"
```
- 사망 처리와 같은 일(렌더러 에셋 리셋 + Title 로드)을 하므로 **공통 함수로 묶을 수 있다**.
- `Restart`는 글리치를 `0.02f`로 둔다. 다음 씬이 GameScene이면 `Player.Start`의 `FadeGlitch(0.9f, 0.002f, 1.5f)`가 다시 덮어쓰므로 문제가 없었다.
  - **Title로 가면 덮어쓰는 코드가 없다.** 렌더러 에셋 값 0.02가 남아 Title에 글리치가 약하게 남는다. 기본값 `0.002f`로 리셋해야 한다(`CameraManager.OnApplicationQuit`과 같은 값).

## 씬 / 싱글톤
- Title 주소는 `Title`이다(Addressables, Boot에서 로드. `docs/SettingsMenu` 참고).
- `GameManager`(`GameManager.cs`, UTF-8)는 GameScene에 배치된 싱글톤이다(`dontDestroyOnLoad: 0`). 씬 이동 로직을 둘 곳으로 적당하다.
  - `CameraManager`도 같은 씬에 있다.
- Title에는 GameManager/CameraManager가 없다. Title 쪽 코드에서 이 함수를 부르면 싱글톤이 자동 생성되므로 GameScene에서만 호출해야 한다.
  - `SettingsMenuUI`는 `allowExitToTitle` 가드가 이미 있다.

## 위험 요소
- `Player.cs`는 **CP949**다. UTF-8로 저장하면 diff 전체가 바뀐다. 바이트를 보존하는 방식으로 수정해야 한다(cp949 decode/encode).
  - 바꿀 줄은 ASCII뿐이다.
- 사망 후 1.5초 안에 ESC 메뉴를 열면 timeScale이 0이 되어 `Invoke`가 멈춘다. 메뉴를 닫으면 이어서 진행되므로 문제가 없다.
  - 메뉴에서 직접 타이틀로 나가도 결과는 같다.
- 사망 연출 중 `timeScale`이 0인 연출(크기 변경)과 겹치는 경우: `ResizeCoroutine`이 원래 값으로 복원한다. 공통 함수에서 `timeScale = 1`도 강제로 설정한다.
