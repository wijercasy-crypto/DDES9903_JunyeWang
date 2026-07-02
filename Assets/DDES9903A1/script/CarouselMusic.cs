using UnityEngine;
using System.Collections;

/// <summary>
/// 旋转木马音乐（渐入渐出）
/// 坐上木马时音乐渐入，转完/起身时渐出。挂在木马上，和 AudioSource 同物体。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CarouselMusic : MonoBehaviour
{
    [Header("音量")]
    [Tooltip("音乐播放时的目标音量")]
    [Range(0f, 1f)]
    public float targetVolume = 0.7f;

    [Header("渐变时长")]
    [Tooltip("渐入时长（秒）")]
    public float fadeInDuration = 2f;

    [Tooltip("渐出时长（秒）")]
    public float fadeOutDuration = 2f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0f;       // 开局静音
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    /// <summary>音乐渐入（坐上木马时调用）</summary>
    public void FadeIn()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (!audioSource.isPlaying) audioSource.Play();
        fadeRoutine = StartCoroutine(FadeTo(targetVolume, fadeInDuration));
    }

    /// <summary>音乐渐出（转完/起身时调用）</summary>
    public void FadeOut()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(0f, fadeOutDuration, stopAtEnd: true));
    }

    private IEnumerator FadeTo(float target, float duration, bool stopAtEnd = false)
    {
        float start = audioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        audioSource.volume = target;

        // 渐出到 0 后停止播放，省资源
        if (stopAtEnd && target <= 0.01f)
            audioSource.Stop();
    }
}