using UnityEngine;
using UnityEngine.UI;

// 설정 메뉴의 볼륨 슬라이더. 값은 SoundManager가 PlayerPrefs에 저장 (여닫기/정지는 UIStack 담당)
public class VolumeSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        Bind(masterSlider, VolumeChannel.Master);
        Bind(bgmSlider, VolumeChannel.BGM);
        Bind(sfxSlider, VolumeChannel.SFX);
    }

    private static void Bind(Slider slider, VolumeChannel channel)
        => slider.onValueChanged.AddListener(v => SoundManager.Instance.SetVolume(channel, v));

    public void Refresh()
    {
        masterSlider.SetValueWithoutNotify(SoundManager.Instance.GetVolume(VolumeChannel.Master));
        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.GetVolume(VolumeChannel.BGM));
        sfxSlider.SetValueWithoutNotify(SoundManager.Instance.GetVolume(VolumeChannel.SFX));
    }
}
