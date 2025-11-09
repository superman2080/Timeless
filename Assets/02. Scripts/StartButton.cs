using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class StartButton : MonoBehaviour
{
    public string sceneToLoad = "NextSceneName";

    public void StartLoading()
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneToLoad));
    }

    IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        // 1. 비동기 로드 작업 시작
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // 2. 로딩이 완료될 때까지 반복
        while (!asyncLoad.isDone)
        {
            // 3. 로딩 진행률 (0.0 ~ 0.9)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 이곳에서 로딩 바 UI를 업데이트합니다.
            Debug.Log("로딩 진행률: " + (progress * 100) + "%");

            // 4. 다음 프레임까지 대기 (메인 스레드 유지)
            yield return null;
        }
    }
}
