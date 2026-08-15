using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// 事件触发字幕（支持多句连续播放，两种配音模式）：
///
/// 模式一：整段语音（比如ElevenLabs一次性生成的多角色对话）
///   - 把整段 AudioClip 拖进 masterAudioClip
///   - 每句台词填 startTime（这句话在整段语音里第几秒开始说，自己听音频对时间戳）
///   - 字幕会按时间戳走，跟整段语音同步播放
///
/// 模式二：每句单独一个 AudioClip
///   - masterAudioClip 留空
///   - 每句台词自己的 audioClip 字段拖对应的语音文件
///   - 时长自动等于这句语音的长度，startTime 不用填
///
/// 调用 Show() 从头开始播放。调用 Hide() 随时打断，立刻渐隐消失并停止语音。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TriggeredSubtitle : MonoBehaviour
{
    [System.Serializable]
    public class SubtitleLine
    {
        [TextArea] public string text;

        [Header("模式二：单独配音（不用整段语音时填这个）")]
        [Tooltip("这一句单独的语音，留空则用 holdDuration 兜底时长")]
        public AudioClip audioClip;

        [Header("模式一：整段语音（填了上面的 masterAudioClip 才需要这个）")]
        [Tooltip("这句台词在整段语音里第几秒开始说")]
        public float startTime;
    }

    [Header("延迟")]
    [Tooltip("调用 Show 后，等几秒才开始显示字幕")]
    public float startDelay = 3f;

    [Header("字幕内容（按顺序，一句一条）")]
    public List<SubtitleLine> lines = new List<SubtitleLine>();

    [Header("配音播放")]
    [Tooltip("整段语音（包含所有句子）。填了这个就走时间戳模式，忽略每句自己的audioClip")]
    public AudioClip masterAudioClip;
    [Tooltip("留空会自动在这个物体上添加一个 AudioSource")]
    public AudioSource audioSource;

    [Header("渐变")]
    public float fadeInDuration = 1f;
    [Tooltip("兜底停留时长：模式二没配audioClip时用这个；模式一里最后一句播完后再停留这么久")]
    public float holdDuration = 3f;
    public float fadeOutDuration = 1f;
    [Tooltip("只在模式二（每句单独配音）里生效，句子之间额外停顿")]
    public float gapBetweenLines = 0.5f;
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Tooltip("每句最短显示这么久，即使按时间戳算出来更短，也不会一闪而过")]
    public float minLineHold = 1.5f;

    [Header("朝向玩家")]
    public bool faceCamera = false;

    [Header("调试")]
    public bool showDebugLogs = true;

    [Header("事件")]
    [Tooltip("所有句子都播完（包括最后的淡出）之后触发一次")]
    public UnityEvent onSequenceComplete;
    [Tooltip("调用Show()、第一句刚开始显示的那一刻触发一次")]
    public UnityEvent onShowStarted;

    private TMP_Text tmp;
    private Camera cam;
    private Coroutine routine;

    private void Start()
    {
        tmp = GetComponent<TMP_Text>();
        cam = Camera.main;
        SetAlpha(0f);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (faceCamera && cam != null && tmp.color.a > 0.01f)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

    /// <summary>外部调用：从头开始播放所有句子</summary>
    public void Show()
    {
        if (routine != null) StopCoroutine(routine);
        onShowStarted.Invoke();
        routine = StartCoroutine(PlayAllLines());
    }

    /// <summary>外部调用：不管当前播放到哪，立刻打断并渐隐消失、停止语音</summary>
    public void Hide()
    {
        var floatingSubtitle = GetComponent("FloatingSubtitle") as MonoBehaviour;
        if (floatingSubtitle != null) floatingSubtitle.enabled = false;

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator FadeOutAndStop()
    {
        yield return Fade(tmp.color.a, 0f, fadeOutDuration);
    }

    /// <summary>外部调用：只播放指定下标的这一句(渐显->等语音播完->渐隐)，播完才返回。
    /// 给 FarewellDialogueConductor 这类"多个说话人交替播放"的指挥脚本用。</summary>
    public IEnumerator PlayLineRoutine(int index)
    {
        if (index < 0 || index >= lines.Count)
        {
            Debug.LogWarning($"[TriggeredSubtitle] PlayLineRoutine索引越界: {index}");
            yield break;
        }

        var line = lines[index];
        tmp.text = line.text;
        yield return Fade(0f, maxAlpha, fadeInDuration);

        float hold = holdDuration;
        if (line.audioClip != null)
        {
            audioSource.clip = line.audioClip;
            audioSource.Play();
            hold = line.audioClip.length;
        }
        hold = Mathf.Max(hold, minLineHold);

        if (showDebugLogs) Debug.Log($"[TriggeredSubtitle] (单句模式) 第{index}句「{line.text}」，停留{hold:F2}秒");

        yield return new WaitForSeconds(hold);
        yield return Fade(maxAlpha, 0f, fadeOutDuration);
    }

    private IEnumerator PlayAllLines()
    {
        yield return new WaitForSeconds(startDelay);

        bool useMasterClip = masterAudioClip != null;

        if (useMasterClip)
        {
            audioSource.clip = masterAudioClip;
            audioSource.Play();
        }

        float elapsed = 0f; // 只在 useMasterClip 模式下用来追踪时间轴

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (useMasterClip)
            {
                float waitTime = line.startTime - elapsed;
                if (waitTime > 0f)
                {
                    yield return new WaitForSeconds(waitTime);
                    elapsed += waitTime;
                }
            }

            tmp.text = line.text;
            yield return Fade(0f, maxAlpha, fadeInDuration);

            float lineHold;

            if (useMasterClip)
            {
                elapsed += fadeInDuration;
                float nextStart = (i < lines.Count - 1) ? lines[i + 1].startTime : (line.startTime + holdDuration);
                lineHold = Mathf.Max(minLineHold, nextStart - elapsed - fadeOutDuration);
            }
            else
            {
                lineHold = holdDuration;
                if (line.audioClip != null)
                {
                    // 自愈：万一 Start() 没来得及跑或者audioSource被清空了，这里再兜底加一次
                    if (audioSource == null)
                    {
                        audioSource = GetComponent<AudioSource>();
                        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                        audioSource.playOnAwake = false;
                        if (showDebugLogs) Debug.LogWarning("[TriggeredSubtitle] audioSource是空的，播放前临时补加了一个");
                    }

                    audioSource.clip = line.audioClip;
                    audioSource.Play();
                    lineHold = line.audioClip.length;

                    if (showDebugLogs)
                        Debug.Log($"[TriggeredSubtitle] 播放语音「{line.audioClip.name}」，volume={audioSource.volume}，mute={audioSource.mute}，isPlaying={audioSource.isPlaying}，spatialBlend={audioSource.spatialBlend}");
                }
            }

            if (showDebugLogs) Debug.Log($"[TriggeredSubtitle] 第{i}句「{line.text}」startTime={line.startTime}，计算出的停留时长={lineHold:F2}秒");
            yield return new WaitForSeconds(lineHold);
            yield return Fade(maxAlpha, 0f, fadeOutDuration);

            if (useMasterClip)
            {
                elapsed += lineHold + fadeOutDuration;
            }
            else
            {
                yield return new WaitForSeconds(gapBetweenLines);
            }
        }

        onSequenceComplete.Invoke();
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (tmp == null) return;
        Color c = tmp.color;
        c.a = a;
        tmp.color = c;
    }
}
