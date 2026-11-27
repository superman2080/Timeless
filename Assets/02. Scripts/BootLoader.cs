using UnityEngine;
using UnityEngine.AddressableAssets;

// 빌드 첫 씬(Boot). Title도 Addressables 씬이라 빌드 목록에 둘 수 없어서, 여기서 Title을 로드한다
public class BootLoader : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "Title";

    private void Start()
    {
        Addressables.LoadSceneAsync(titleSceneName);
    }
}
