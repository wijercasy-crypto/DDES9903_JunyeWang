using UnityEngine;
using System.Collections;

// 挂在场景里一个空物体上（比如叫 "SceneBGMManager"）。
// 负责终点站这场戏的背景音乐，切换时用交叉淡入淡出，不会突兀硬切。
//
// 用法：
// 1. introClip / chapter3Clip / lightClip 三个字段拖对应的BGM
// 2. 场景一开始会自动播放 introClip（加重版）
// 3. 把 SwitchToChapter3() 挂在 FinalStopFarewellSequence 的
//    On Group2 Disappeared() 事件上
// 4. 把 SwitchToLight() 挂在 On Group4 Appeared() 事件上
public class SceneBGMManager : MonoBehaviour
{
    [Header("三段BGM")]
    [Tooltip("入场时播放的加重版")]
    public AudioClip introClip;
    [Tooltip("第二组消失、第三组出现时切换到这个")]
    public AudioClip chapter3Clip;
    [Tooltip("第四组出现时切换到这个（轻巧版）")]
    public AudioClip lightClip;

    [Header("交叉淡入淡出")]
    public float crossfadeDuration = 2f;

    [Header("音量")]
    [Range(0f, 1f)]
    public float volume = 0.6f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool usingA = true;
    private Coroutine crossfadeRoutine;

    void Awake()
    {
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { sourceA, sourceB })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D音效，不随位置衰减
        }
    }

    void Start()
    {
        if (introClip != null)
        {
            sourceA.clip = introClip;
            sourceA.volume = volume;
            sourceA.Play();
        }
    }

    // 挂在 On Group2 Disappeared() 上
    public void SwitchToChapter3()
    {
        CrossfadeTo(chapter3Clip);
    }

    // 挂在 On Group4 Appeared() 上
    public void SwitchToLight()
    {
        CrossfadeTo(lightClip);
    }

    public void CrossfadeTo(AudioClip newClip)
    {
        if (newClip == null) return;
        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(CrossfadeRoutine(newClip));
    }

    IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        AudioSource from = usingA ? sourceA : sourceB;
        AudioSource to = usingA ? sourceB : sourceA;
        usingA = !usingA;

        to.clip = newClip;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float pct = t / crossfadeDuration;
            from.volume = Mathf.Lerp(volume, 0f, pct);
            to.volume = Mathf.Lerp(0f, volume, pct);
            yield return null;
        }

        from.Stop();
        to.volume = volume;
    }
}
