using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全屏黑幕淡入淡出，挂在一个 Canvas 上（建议挂在 MainCamera 下面的子物体，
/// Canvas Render Mode 设为 Screen Space - Camera，这样 VR 里也能正确遮挡视野）。
/// 用法：ScreenFader.Instance.FadeOut(1f, () => { /* 黑幕完全遮挡后做的事，比如传送 */ });
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Tooltip("承载黑色的 UI Image，铺满全屏，初始 alpha = 0")]
    public Image fadeImage;

    void Awake()
    {
        Instance = this;
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false; // 黑幕不挡点击/交互
        }
    }

    public void FadeOut(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
    }

    public void FadeIn(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
        onComplete?.Invoke();
    }
}
