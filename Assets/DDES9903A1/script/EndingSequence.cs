using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 结尾序列:门关上后,黑球渐渐笼罩(3D黑暗包裹),然后浮现最后一句字幕。
/// 由门的 On Force Close 触发 PlayEnding()。
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [Header("黑暗包裹")]
    [Tooltip("内翻黑球的 Renderer(材质需透明)")]
    public Renderer blackSphere;

    [Tooltip("黑暗笼罩的时长")]
    public float darkenDuration = 3f;

    [Header("最后的字幕")]
    [Tooltip("结尾字幕的 TMP 文字")]
    public TMP_Text finalText;

    [Tooltip("黑暗后,等几秒才浮现字幕")]
    public float textDelay = 1.5f;

    [Tooltip("字幕渐显时长")]
    public float textFadeDuration = 3f;

    private Material sphereMat;

    private void Start()
    {
        if (blackSphere != null) sphereMat = blackSphere.material;
        // 初始:黑球透明、字幕透明
        if (sphereMat != null) SetAlpha(sphereMat, 0f);
        if (finalText != null) SetTextAlpha(0f);
    }

    /// <summary>门关上时调用,播放结尾</summary>
    public void PlayEnding()
    {
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        // 1. 黑球渐渐笼罩(世界陷入黑暗)
        if (sphereMat != null)
            yield return StartCoroutine(FadeMat(sphereMat, 0f, 1f, darkenDuration));

        // 2. 全黑后停顿
        yield return new WaitForSeconds(textDelay);

        // 3. 字幕浮现
        if (finalText != null)
            yield return StartCoroutine(FadeText(0f, 1f, textFadeDuration));

        // 4. 字幕停留(结束,或之后接退出/黑屏)
        // 这里字幕会一直留着,游戏在此定格
    }

    private IEnumerator FadeMat(Material m, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetAlpha(m, Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetAlpha(m, to);
    }

    private IEnumerator FadeText(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetTextAlpha(to);
    }

    private void SetAlpha(Material m, float a)
    {
        Color c = m.color; c.a = a; m.color = c;
    }

    private void SetTextAlpha(float a)
    {
        Color c = finalText.color; c.a = a; finalText.color = c;
    }
}