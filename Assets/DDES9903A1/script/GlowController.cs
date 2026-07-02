using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 设施发光控制器（Quick Outline 描边版）
/// 用 Quick Outline 插件给设施加边缘描边高亮，不遮挡物体本色。
/// 接口与原版一致（StartGlow / StopGlow / IsGlowing），仍由 GuideManager 调用。
/// 需先从 Asset Store 导入免费插件 Quick Outline。
/// </summary>
public class GlowController : MonoBehaviour
{
    [Header("描边颜色")]
    [Tooltip("描边颜色（建议暖金色，呼应童年回忆）")]
    public Color glowColor = new Color(1f, 0.85f, 0.5f);

    [Header("描边设置")]
    [Range(0f, 10f)]
    [Tooltip("描边最大宽度")]
    public float outlineWidth = 4f;

    [Header("渐变设置")]
    [Tooltip("渐显/渐隐的时长（秒）")]
    public float fadeDuration = 1.5f;

    [Header("脉动效果")]
    [Tooltip("发光时是否轻微脉动（呼吸感），更吸引注意")]
    public bool pulseWhenGlowing = true;
    public float pulseSpeed = 2f;
    [Range(0f, 1f)]
    public float pulseAmount = 0.3f;

    // ── 内部状态 ──
    private Outline outline;          // Quick Outline 组件
    private bool isGlowing = false;
    private float currentLevel = 0f;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 加 Quick Outline 组件（一次给整个设施加描边）
        outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();

        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = glowColor;
        outline.OutlineWidth = 0f;      // 初始不显示
        outline.enabled = false;
    }

    private void Update()
    {
        if (isGlowing && pulseWhenGlowing && currentLevel > 0.01f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            ApplyVisuals(Mathf.Clamp01(currentLevel * pulse));
        }
    }

    // ── 公开接口：与原版一致 ──

    public void StartGlow()
    {
        if (outline != null) outline.enabled = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f));
        isGlowing = true;
        Debug.Log($"[发光] {gameObject.name} 开始描边高亮");
    }

    public void StopGlow()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(currentLevel, 0f));
        isGlowing = false;
        Debug.Log($"[发光] {gameObject.name} 描边熄灭");
    }

    public bool IsGlowing()
    {
        return isGlowing;
    }

    // ──
    private IEnumerator FadeRoutine(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            currentLevel = Mathf.Lerp(from, to, elapsed / fadeDuration);
            ApplyVisuals(currentLevel);
            yield return null;
        }
        currentLevel = to;
        ApplyVisuals(to);

        if (to <= 0.01f && outline != null)
            outline.enabled = false;   // 完全熄灭后关掉，省性能
    }

    private void ApplyVisuals(float level)
    {
        if (outline == null) return;
        // 用描边宽度表现强弱，颜色的 alpha 也跟着渐变
        outline.OutlineWidth = outlineWidth * level;
        Color c = glowColor;
        c.a = level;
        outline.OutlineColor = c;
    }
}