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

        hpSlider.value = player.stat.HP / player.stat.MaxHP;
    }
}
