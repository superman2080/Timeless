using UnityEngine;
using UnityEngine.UI;

// ESC 설정 메뉴 (UIStack의 rootPanel). 탭 2개: 설정(볼륨 + 타이틀로 나가기) / 키 설정
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
        // CameraManager/GameManager는 게임 씬에만 있으므로 Title에서는 호출하지 않음 (싱글톤 자동 생성 방지)
        if (!allowExitToTitle)
            return;

        Stack.Clear();
        GameManager.Instance.ReturnToTitle();
    }
}
