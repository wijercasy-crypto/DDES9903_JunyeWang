using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// 挂在终点站最后出现的那扇门上。
// 门上需要一个 Collider，勾选 Is Trigger，玩家走进去时触发场景切换。
// 如果场景里没有全屏黑色 Image，把 fadeCanvasGroup 留空即可，会直接切场景不做淡出。
public class DoorSceneTransition : MonoBehaviour
{
    [Tooltip("要返回的场景名，填 Build Settings 里注册过的场景名")]
    public string targetSceneName = "FirstRoom";

    [Tooltip("可选：全屏黑色 Image 上的 CanvasGroup，用于淡出效果")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.2f;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
