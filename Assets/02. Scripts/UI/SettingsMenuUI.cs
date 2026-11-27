using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

// ESC 설정 메뉴 (UIStack의 rootPanel). 탭 2개: SETTINGS(볼륨 + 타이틀로 나가기) / KEY BINDINGS
public class SettingsMenuUI : UIPanel
{
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject keysPage;
    [SerializeField] private Button settingsTabButton;
    [SerializeField] private Button keysTabButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button exitTitleButton;
    [SerializeField] private VolumeSettingsUI volumeSettings;
    [SerializeField] private KeyBindingUI keyBinding;
    [SerializeField] private ConfirmPopupUI confirmPopup;

    [Tooltip("Title 씬에서는 끔 (버튼 숨김)")]
    [SerializeField] private bool allowExitToTitle = true;
    [SerializeField] private string titleSceneName = "Title";

    private bool exiting;

    private void Awake()
    {
        settingsTabButton.onClick.AddListener(() => ShowPage(true));
        keysTabButton.onClick.AddListener(() => ShowPage(false));
        closeButton.onClick.AddListener(() => Stack.Clear());
        exitTitleButton.onClick.AddListener(() => confirmPopup.Show("타이틀로 나가시겠습니까?\n진행 상황은 저장되지 않습니다.", ExitToTitle));
        exitTitleButton.gameObject.SetActive(allowExitToTitle);
    }

    public override void OnPush()
    {
        base.OnPush();
        volumeSettings.Refresh();
        keyBinding.Refresh();
        ShowPage(true);
    }

    public override void OnPop()
    {
        keyBinding.CancelRebind();
        base.OnPop();
    }

    private void ShowPage(bool settings)
    {
        keyBinding.CancelRebind();
        settingsPage.SetActive(settings);
        keysPage.SetActive(!settings);
        // 선택된 탭은 비활성 표시
        settingsTabButton.interactable = !settings;
        keysTabButton.interactable = settings;
    }

    private void ExitToTitle()
    {
        if (!allowExitToTitle || exiting)
            return;
        exiting = true;

        Stack.Clear();
        Time.timeScale = 1f;
        // 렌더러 에셋 값(픽셀화/글리치)이 다음 씬에 남지 않도록 기본값으로 (CameraManager는 게임 씬에만 있음)
        CameraManager.Instance.SetPixelateIntensity(1);
        CameraManager.Instance.SetGlitch(0.002f);
        PlayerPrefs.Save();
        Addressables.LoadSceneAsync(titleSceneName);
    }
}
