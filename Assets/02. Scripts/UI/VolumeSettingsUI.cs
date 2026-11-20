using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ESC로 여닫는 볼륨 설정 패널. 열려 있는 동안 게임 정지, 값은 SoundManager가 PlayerPrefs에 저장
public class VolumeSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeButton;

    private float prevTimeScale = 1f;

    private void Start()
    {
        Bind(masterSlider, VolumeChannel.Master);
        Bind(bgmSlider, VolumeChannel.BGM);
        Bind(sfxSlider, VolumeChannel.SFX);
        closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    private static void Bind(Slider slider, VolumeChannel channel)
        => slider.onValueChanged.AddListener(v => SoundManager.Instance.SetVolume(channel, v));

    // Keyboard.current는 마지막 사용 키보드 하나만 보므로, 모든 키보드에 바인딩되는 액션으로 ESC 처리
    private InputAction toggleAction;

    private void OnEnable()
    {
        toggleAction = new InputAction("ToggleVolumeSettings", binding: "<Keyboard>/escape");
        toggleAction.performed += _ => Toggle();
        toggleAction.Enable();
    }

    private void OnDisable()
    {
        toggleAction.Disable();
        toggleAction.Dispose();
    }

    private void Toggle()
    {
        if (panel.activeSelf)
            Close();
        else
            Open();
    }

    public void Open()
    {
        // 컷씬/크기 변경 연출 등 이미 정지 중이면 열지 않음 (timeScale 복원 충돌 방지)
        if (Time.timeScale == 0f)
            return;

        masterSlider.SetValueWithoutNotify(SoundManager.Instance.GetVolume(VolumeChannel.Master));
        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.GetVolume(VolumeChannel.BGM));
        sfxSlider.SetValueWithoutNotify(SoundManager.Instance.GetVolume(VolumeChannel.SFX));

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        panel.SetActive(true);
    }

    public void Close()
    {
        if (!panel.activeSelf)
            return;

        panel.SetActive(false);
        Time.timeScale = prevTimeScale;
        PlayerPrefs.Save();
    }
}
