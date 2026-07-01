using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 设施发光控制器（光源版）—— 不依赖 Bloom 后处理，靠真实光源照亮设施。
/// 设施被点亮时，在它中心创建一个明亮的 Point Light 把设施和周围照亮，
/// 玩家一眼就能看到哪个设施在发光。同时也设置材质 Emission（有 Bloom 时更好看）。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在设施根物体上（Carousel、Rockets、Roller、FerrisWheel）
/// 2. 调 glowColor（发光颜色）和 lightIntensity（光源亮度）
/// 3. 由 GuideManager 控制何时发光，不用手动调用
/// </summary>
public class GlowController : MonoBehaviour
{
    [Header("发光颜色")]
    [Tooltip("发光颜色（建议暖金色，呼应童年回忆）")]
    public Color glowColor = new Color(1f, 0.85f, 0.5f);

    [Header("光源设置（主要可见效果）")]
    [Tooltip("光源最大亮度。看不清就调大，太刺眼就调小")]
    public float lightIntensity = 8f;

    [Tooltip("光源照射范围（米）。要能覆盖整个设施，大型设施调大")]
    public float lightRange = 15f;

    [Tooltip("光源相对设施中心的高度偏移（米），抬高一点照明更均匀")]
    public float lightHeightOffset = 3f;

    [Header("材质发光（有 Bloom 时的额外效果）")]
    [Tooltip("材质自发光强度")]
    public float emissionIntensity = 3f;

    [Header("渐变设置")]
    [Tooltip("渐亮/渐灭的时长（秒）")]
    public float fadeDuration = 1.5f;

    [Header("脉动效果")]
    [Tooltip("发光时是否轻微脉动（呼吸感），更吸引注意")]
    public bool pulseWhenGlowing = true;
    public float pulseSpeed = 2f;
    [Range(0f, 1f)]
    public float pulseAmount = 0.3f;

    // ── 内部状态 ──
    private List<Material> glowMaterials = new List<Material>();
    private Light glowLight;                // 动态创建的光源
    private bool isGlowing = false;
    private float currentLevel = 0f;        // 当前发光程度 0~1
    private Coroutine fadeCoroutine;

    private static readonly string EmissionColorProperty = "_EmissionColor";
    private static readonly string EmissionKeyword = "_EMISSION";

    private void Awake()
    {
        CollectMaterials();
        CreateLight();
    }

    /// <summary>收集所有渲染器材质</summary>
    private void CollectMaterials()
    {
        glowMaterials.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat != null && !glowMaterials.Contains(mat))
                    glowMaterials.Add(mat);
            }
        }
    }

    /// <summary>创建发光用的光源（一开始关闭）</summary>
    private void CreateLight()
    {
        // 计算设施中心（用所有渲染器的包围盒中心）
        Vector3 center = GetBounds().center;

        GameObject lightObj = new GameObject("GlowLight_" + gameObject.name);
        lightObj.transform.position = center + Vector3.up * lightHeightOffset;
        lightObj.transform.SetParent(transform, true);  // 跟着设施走

        glowLight = lightObj.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = lightRange;
        glowLight.intensity = 0f;       // 初始关闭
        glowLight.enabled = false;

        // 初始关闭材质发光
        SetLevel(0f);
    }

    private void Update()
    {
        // 发光时的脉动
        if (isGlowing && pulseWhenGlowing && currentLevel > 0.01f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            ApplyVisuals(currentLevel * pulse);
        }
    }

    // ─────────────────────────────────────────────
    // 公开方法：由 GuideManager 调用
    // ─────────────────────────────────────────────

    public void StartGlow()
    {
        if (glowLight != null) glowLight.enabled = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f));
        isGlowing = true;
        Debug.Log($"[发光] {gameObject.name} 开始发光（光源已点亮）");
    }

    public void StopGlow()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(currentLevel, 0f));
        isGlowing = false;
        Debug.Log($"[发光] {gameObject.name} 熄灭");
    }

    public bool IsGlowing()
    {
        return isGlowing;
    }

    // ─────────────────────────────────────────────
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

        // 完全熄灭后关闭光源（省性能）
        if (to <= 0.01f && glowLight != null)
            glowLight.enabled = false;
    }

    /// <summary>应用发光程度到光源和材质</summary>
    private void ApplyVisuals(float level)
    {
        SetLevel(level);
    }

    private void SetLevel(float level)
    {
        // 光源亮度
        if (glowLight != null)
            glowLight.intensity = lightIntensity * level;

        // 材质自发光
        Color emissionColor = glowColor * (emissionIntensity * level);
        foreach (Material mat in glowMaterials)
        {
            if (mat == null) continue;
            if (level > 0.01f)
            {
                mat.EnableKeyword(EmissionKeyword);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor(EmissionColorProperty, emissionColor);
            }
            else
            {
                mat.SetColor(EmissionColorProperty, Color.black);
            }
        }
    }

    // ─────────────────────────────────────────────
    private Bounds GetBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = glowColor;
        Bounds b = GetBounds();
        Gizmos.DrawWireSphere(b.center + Vector3.up * lightHeightOffset, lightRange);
    }
}
