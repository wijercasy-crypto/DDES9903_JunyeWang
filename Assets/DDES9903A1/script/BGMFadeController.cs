using UnityEngine;
using System.Collections;

// 挂在场景里一个空物体上（比如叫 "SwampBGM"）。
// 场景开始自动播放BGM循环，调用 FadeOut() 会渐隐消失并停止。
public class BGMFadeController : MonoBehaviour
{
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float volume = 0.6f;
    public bool playOnStart = true;
    public float fadeOutDuration = 2f;

    private AudioSource source;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D音效，不随位置衰减
    }

    void Start()
    {
        if (playOnStart && bgmClip != null)
        {
            source.clip = bgmClip;
            source.volume = volume;
            source.Play();
        }
    }

    // 挂在需要淡出BGM的事件上（比如 TriggeredSubtitle 的 On Show Started()）
    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        float startVol = source.volume;
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeOutDuration);
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }
}
