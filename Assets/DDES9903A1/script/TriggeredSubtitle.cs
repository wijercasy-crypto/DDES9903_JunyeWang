using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 事件触发字幕（支持多句连续播放）。
/// 调用 Show() 后，按顺序播放 lines 里的每一句：渐显→停留→渐隐→下一句。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TriggeredSubtitle : MonoBehaviour
{
    [Header("延迟")]
    [Tooltip("调用 Show 后，等几秒才开始显示字幕")]
    public float startDelay = 3f;
    [Header("字幕内容（按顺序，一句一行）")]
    [TextArea]
    public List<string> lines = new List<string>();

    [Header("渐变")]
    public float fadeInDuration = 1f;
    public float holdDuration = 3f;       // 每句停留时长
    public float fadeOutDuration = 1f;
    public float gapBetweenLines = 0.5f;  // 句子之间的间隔
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("朝向玩家")]
    public bool faceCamera = false;

    private TMP_Text tmp;
    private Camera cam;
    private Coroutine routine;

    private void Start()
    {
        tmp = GetComponent<TMP_Text>();
        cam = Camera.main;
        SetAlpha(0f);
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
        routine = StartCoroutine(PlayAllLines());
    }

    private IEnumerator PlayAllLines()
    {
        // 先等待延迟
        yield return new WaitForSeconds(startDelay);

        foreach (string line in lines)
        {
            tmp.text = line;
            yield return Fade(0f, maxAlpha, fadeInDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return Fade(maxAlpha, 0f, fadeOutDuration);
            yield return new WaitForSeconds(gapBetweenLines);
        }
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