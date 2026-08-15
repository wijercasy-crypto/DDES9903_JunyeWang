using UnityEngine;
using TMPro;
using System.Collections;

// 挂在"THE END"这个TMP文字物体上。
// 不会自动触发，需要外部调用 TriggerFadeIn()（比如"End"按钮点击时）。
[RequireComponent(typeof(TMP_Text))]
public class TheEndFadeIn : MonoBehaviour
{
    [Tooltip("调用TriggerFadeIn后，先等多久（纯黑停留）才开始淡入")]
    public float startDelay = 1f;
    [Tooltip("淡入本身用多久")]
    public float fadeInDuration = 3f;

    private TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        SetAlpha(0f);
    }

    // 挂在"End"按钮的点击事件上
    public void TriggerFadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(0f, 1f, t / fadeInDuration));
            yield return null;
        }
        SetAlpha(1f);
    }

    void SetAlpha(float a)
    {
        Color c = tmp.color;
        c.a = a;
        tmp.color = c;
    }
}
