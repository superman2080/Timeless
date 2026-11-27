using UnityEngine;
using UnityEngine.InputSystem;

// 키 리바인딩 결과(바인딩 오버라이드)를 PlayerPrefs에 JSON으로 저장/로드
public static class KeyBindingStore
{
    private const string PrefsKey = "Input.Bindings";

    public static void Load(InputActionAsset asset)
    {
        string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
            asset.LoadBindingOverridesFromJson(json);
    }

    public static void Save(InputActionAsset asset)
    {
        PlayerPrefs.SetString(PrefsKey, asset.SaveBindingOverridesAsJson());
    }

    public static void ResetAll(InputActionAsset asset)
    {
        asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PrefsKey);
    }
}
