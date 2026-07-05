using UnityEngine;
using System.Collections;

public class CarouselMusic : MonoBehaviour
{
    [Header("音源(可多个,一起同时渐入渐出)")]
    public AudioSource[] sources;

    [Range(0f, 1f)]
    public float targetVolume = 0.7f;
    public float fadeInDuration = 2f;
    public float fadeOutDuration = 2f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        // 没手动拖就自动抓本物体上所有 AudioSource
        if (sources == null || sources.Length == 0)
            sources = GetComponents<AudioSource>();

        foreach (var s in sources)
        {
            if (s == null) continue;
            s.volume = 0f;
            s.playOnAwake = false;
            s.loop = true;
        }
    }

    public void FadeIn()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        // 所有音源同时开始播放
        foreach (var s in sources)
            if (s != null && !s.isPlaying) s.Play();
        fadeRoutine = StartCoroutine(FadeTo(targetVolume, fadeInDuration));
    }

    public void FadeOut()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(0f, fadeOutDuration, true));
    }

    private IEnumerator FadeTo(float target, float duration, bool stopAtEnd = false)
    {
        int n = sources.Length;
        float[] starts = new float[n];
        for (int i = 0; i < n; i++)
            starts[i] = sources[i] != null ? sources[i].volume : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio = t / duration;
            for (int i = 0; i < n; i++)
                if (sources[i] != null)
                    sources[i].volume = Mathf.Lerp(starts[i], target, ratio);
            yield return null;
        }
        for (int i = 0; i < n; i++)
        {
            if (sources[i] == null) continue;
            sources[i].volume = target;
            if (stopAtEnd && target <= 0.01f) sources[i].Stop();
        }
    }
}