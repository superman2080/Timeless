using UnityEngine;
using UnityEngine.UI;

public class HPBar : EventListener
{
    public Slider hpSlider;
    private Player player;

    private void Start()
    {
        hpSlider = gameObject.GetComponent<Slider>();
        player = FindAnyObjectByType<Player>();
    }

    public void OnTakeDamage()
    {
        if(hpSlider == null) return;

        float ratio = player.stat.HP / player.stat.MaxHP;
        hpSlider.value = Mathf.Lerp(EmptyValue(), 1f, ratio);
    }

    // Fill의 음수 sizeDelta 때문에 슬라이더 값이 이 값 이하면 막대가 이미 비어 보임 → HP 0을 여기에 맞춤
    float EmptyValue()
    {
        RectTransform fill = hpSlider.fillRect;
        if (fill == null || fill.parent is not RectTransform area || area.rect.width <= 0f) return 0f;
        return Mathf.Clamp01(-fill.sizeDelta.x / area.rect.width);
    }
}
