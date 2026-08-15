using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// 挂在场景里一个空物体上（比如叫 "FearOutcomeManager"）。
// 成功的路径触发 GoToXXX() 方法之一，直接切场景。
// 走错路触发 TriggerVoidEnding()，画面渐黑，黑完之后切换到专门的"虚空"场景
// （The End 的显示放在那个新场景里，不在这里处理）。
public class FearOutcomeManager : MonoBehaviour
{
    [Header("失败结局：堕入虚空")]
    [Tooltip("贴在摄像机前方的黑色Quad（跟终点站那块闪光Quad同一个做法，颜色换成黑色）")]
    public Renderer blackoutQuadRenderer;
    public float voidFadeDuration = 2.5f;
    [Tooltip("可选：渐黑过程中播放的音效（比如坠落声/风声）")]
    public AudioSource voidAmbientAudio;
    [Tooltip("渐黑完成后，专门的虚空场景名（需要在 Build Settings 里注册），The End 在那个场景里显示")]
    public string voidSceneName = "Void";

    [Header("场景名（需要在 Build Settings 里注册）")]
    public string firstRoomSceneName = "FirstRoom";
    public string amusementParkSceneName = "AmusementPark";
    public string swampSceneName = "GramophoneSwamp";

    // ===== 成功路径：挂在对应 FearPathTrigger 的 onCorrectPathTaken 上 =====

    public void GoToFirstRoom()
    {
        SceneManager.LoadScene(firstRoomSceneName);
    }

    public void GoToAmusementPark()
    {
        SceneManager.LoadScene(amusementParkSceneName);
    }

    public void GoToSwamp()
    {
        SceneManager.LoadScene(swampSceneName);
    }

    // ===== 失败路径：挂在 FearPathTrigger 的 onWrongPathTaken 上，或 FallDetector 上 =====

    public void TriggerVoidEnding()
    {
        StartCoroutine(VoidEndingRoutine());
    }

    IEnumerator VoidEndingRoutine()
    {
        if (voidAmbientAudio != null) voidAmbientAudio.Play();

        if (blackoutQuadRenderer != null)
        {
            blackoutQuadRenderer.gameObject.SetActive(true);
            float t = 0f;
            Color c = blackoutQuadRenderer.material.color;
            c.a = 0f;
            blackoutQuadRenderer.material.color = c;

            while (t < voidFadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, t / voidFadeDuration);
                blackoutQuadRenderer.material.color = c;
                yield return null;
            }
        }

        SceneManager.LoadScene(voidSceneName);
    }
}
