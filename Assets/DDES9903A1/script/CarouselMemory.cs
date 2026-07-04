using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 旋转木马回忆序列（视线浮现版）
/// 玩家坐上木马几秒后，黑球笼罩下来，内壁上的照片随玩家视线转到时渐显、转过去渐隐，
/// 形成"走马观花地路过回忆"的效果。照片和黑球都在世界空间固定，不跟相机，符合 VR 原则。
/// 
/// 挂载：挂在一个管理物体上（比如黑球，或空物体 MemorySequence）
/// </summary>
public class CarouselMemory : MonoBehaviour
{
    [Header("回忆期间隐藏的描边（防止穿帮）")]
    [Tooltip("进入回忆时临时关闭这些 Outline 描边，比如远处火箭的描边")]
    public List<Outline> hideOutlines = new List<Outline>();

    public List<GlowController> dimDuringMemory = new List<GlowController>();
    [Header("引用")]
    [Tooltip("玩家摄像机（用它的朝向判断看向哪张照片）")]
    public Camera playerCamera;

    [Tooltip("黑球的渲染器（用来渐黑笼罩）。可留空表示不用黑球淡入")]
    public Renderer blackSphere;

    [Tooltip("所有回忆照片（贴在黑球内壁的 Quad），按什么顺序摆无所谓，脚本靠角度判断")]
    public List<Renderer> photos = new List<Renderer>();

    [Header("视线浮现设置")]
    [Tooltip("照片方向与视线夹角小于这个角度时开始亮（度）")]
    public float fadeInAngle = 40f;

    [Tooltip("夹角小于这个角度时完全亮（度）。要小于 fadeInAngle")]
    public float fullBrightAngle = 12f;

    [Tooltip("照片最大不透明度")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("时机")]
    [Tooltip("坐上木马后，等几秒开始笼罩黑球")]
    public float startDelay = 3f;

    [Tooltip("黑球笼罩（渐黑）的时长（秒）")]
    public float blackFadeDuration = 2f;

    // ── 内部状态 ──
    private bool sequenceActive = false;   // 回忆序列是否进行中
    private Material blackMat;
    private List<Material> photoMats = new List<Material>();

    private void Awake()
    {
        // 抓取黑球材质
        if (blackSphere != null)
            blackMat = blackSphere.material;

        // 抓取每张照片的材质，并先设成全透明
        foreach (var r in photos)
        {
            if (r == null) { photoMats.Add(null); continue; }
            Material m = r.material;
            photoMats.Add(m);
            SetMatAlpha(m, 0f);
        }

        // 黑球初始透明（还没笼罩）
        if (blackMat != null) SetMatAlpha(blackMat, 0f);

        if (playerCamera == null) playerCamera = Camera.main;
    }

    /// <summary>由 CarouselRider 在玩家坐上木马时调用，开始回忆序列</summary>
    public void BeginMemory()
    {
        FindObjectOfType<BGMManager>()?.DuckVolume();
        foreach (var o in hideOutlines)
            if (o != null) o.enabled = false;
        if (sequenceActive) return;
        StartCoroutine(MemoryRoutine());
    }

    /// <summary>由 CarouselRider 在玩家起身时调用，中止回忆、恢复</summary>
    public void EndMemory()
    {
        FindObjectOfType<BGMManager>()?.RestoreVolume();
        foreach (var o in hideOutlines)
            if (o != null) o.enabled = true;
        StopAllCoroutines();
        sequenceActive = false;
        // 黑球和照片都淡回透明
        if (blackMat != null) StartCoroutine(FadeMat(blackMat, blackMat.color.a, 0f, blackFadeDuration));
        foreach (var m in photoMats)
            if (m != null) SetMatAlpha(m, 0f);
    }

    private IEnumerator MemoryRoutine()
    {
        // 1. 坐稳后等几秒
        yield return new WaitForSeconds(startDelay);

        // 2. 黑球渐渐笼罩（渐黑）
        if (blackMat != null)
            yield return StartCoroutine(FadeMat(blackMat, 0f, 1f, blackFadeDuration));

        // 3. 开启视线浮现（Update 里处理）
        sequenceActive = true;
    }

    private void Update()
    {
        if (!sequenceActive) return;
        if (playerCamera == null) return;

        Vector3 camPos = playerCamera.transform.position;
        Vector3 camForward = playerCamera.transform.forward;

        // 遍历每张照片，按"视线与照片方向的夹角"决定它的亮度
        for (int i = 0; i < photos.Count; i++)
        {
            if (photos[i] == null || photoMats[i] == null) continue;

            // 从玩家到这张照片的方向
            Vector3 toPhoto = (photos[i].transform.position - camPos).normalized;

            // 视线方向和"看向照片方向"的夹角（度）
            float angle = Vector3.Angle(camForward, toPhoto);

            // 夹角映射成亮度：
            // angle <= fullBrightAngle → 全亮(1)
            // angle >= fadeInAngle     → 全暗(0)
            // 中间线性过渡
            float alpha;
            if (angle <= fullBrightAngle)
                alpha = 1f;
            else if (angle >= fadeInAngle)
                alpha = 0f;
            else
                alpha = Mathf.InverseLerp(fadeInAngle, fullBrightAngle, angle);

            SetMatAlpha(photoMats[i], alpha * maxAlpha);
        }
    }

    // ── 工具方法 ──

    /// <summary>设置材质的透明度（材质需为支持透明的 Shader）</summary>
    private void SetMatAlpha(Material m, float a)
    {
        if (m == null) return;
        Color c = m.color;
        c.a = a;
        m.color = c;
    }

    private IEnumerator FadeMat(Material m, float from, float to, float duration)
    {
        if (m == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetMatAlpha(m, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetMatAlpha(m, to);
    }
}