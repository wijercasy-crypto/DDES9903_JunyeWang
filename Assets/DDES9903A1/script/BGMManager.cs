using UnityEngine;
using System.Collections;

/// <summary>
/// 背景音乐管理器，支持两首 BGM 交叉淡入淡出切换。
/// 前半用 bgm1，触发后交叉过渡到 bgm2。挂在一个常驻物体上（如 GameManager）。
/// </summary>
public class BGMManager : MonoBehaviour
{
    [Header("回忆时压低音量")]
    [Tooltip("回忆时 bgm 压到的音量（几乎听不见）")]
    [Range(0f, 1f)]
    public float duckedVolume = 0.05f;

    [Tooltip("音量变化的过渡时长")]
    public float duckDuration = 1.5f;

    private Coroutine duckRoutine;
    private float normalVolume;   // 记住正常音量

    /// <summary>压低 bgm（进入回忆时调用）</summary>
    public void DuckVolume()
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(FadeVolume(duckedVolume, duckDuration));
    }

    /// <summary>恢复 bgm（离开回忆时调用）</summary>
    public void RestoreVolume()
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(FadeVolume(volume, duckDuration));
    }

    private IEnumerator FadeVolume(float targetVol, float duration)
    {
        // 对当前正在播的那个 source 渐变音量
        AudioSource active = onBgm2 ? sourceB : sourceA;
        if (active == null) yield break;

        float start = active.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            active.volume = Mathf.Lerp(start, targetVol, t / duration);
            yield return null;
        }
        active.volume = targetVol;
    }
    [Header("两首 BGM")]
    public AudioClip bgm1;   // 前半部分
    public AudioClip bgm2;   // 高潮后

    [Header("音量")]
    [Range(0f, 1f)]
    public float volume = 0.6f;

    [Header("过渡")]
    [Tooltip("交叉淡入淡出时长（秒）")]
    public float crossfadeDuration = 4f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool onBgm2 = false;

    private void Start()
    {
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        SetupSource(sourceA);
        SetupSource(sourceB);
        // 开局不播，等开门触发 PlayBGM1()
    }
    /// <summary>开门时调用，开始播放 bgm1（渐入）</summary>
    public void PlayBGM1()
    {
        if (sourceA.isPlaying) return;   // 已经在播就不重复
        sourceA.clip = bgm1;
        sourceA.Play();
        StartCoroutine(FadeIn(sourceA, volume, crossfadeDuration));
    }

    private IEnumerator FadeIn(AudioSource s, float targetVol, float duration)
    {
        s.volume = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            s.volume = Mathf.Lerp(0f, targetVol, t / duration);
            yield return null;
        }
        s.volume = targetVol;
    }
    private void SetupSource(AudioSource s)
    {
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;   // 2D，背景音乐不需要空间感
        s.volume = 0f;
    }

    /// <summary>切换到 bgm2（过山车顶点后调用）。交叉淡入淡出。</summary>
    public void SwitchToBGM2()
    {
        if (onBgm2) return;   // 已经切过了，不重复
        onBgm2 = true;
        StartCoroutine(Crossfade(sourceA, sourceB, bgm2));
    }

    /// <summary>如果需要切回 bgm1</summary>
    public void SwitchToBGM1()
    {
        if (!onBgm2) return;
        onBgm2 = false;
        StartCoroutine(Crossfade(sourceB, sourceA, bgm1));
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, AudioClip toClip)
    {
        // 准备切入的音源
        to.clip = toClip;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        float startFromVol = from.volume;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float ratio = t / crossfadeDuration;
            from.volume = Mathf.Lerp(startFromVol, 0f, ratio);   // 旧的渐弱
            to.volume = Mathf.Lerp(0f, volume, ratio);           // 新的渐强
            yield return null;
        }

        from.volume = 0f;
        from.Stop();
        to.volume = volume;
    }
}