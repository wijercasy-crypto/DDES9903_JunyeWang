using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 碰触穿越门（带全屏渐变遮罩）—— 玩家碰到门，画面渐变到纯色（盖住穿帮画面），再切场景。
/// 用白屏渐变最适合"穿过发光白门"的意境。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在 Quad（发光门面）上
/// 2. 给 Quad 添加 Box Collider，勾选 Is Trigger ✓，Size 覆盖门洞并给点厚度
/// 3. 填 Target Scene Name（如 "Chapter2"）
/// 4. 设置全屏遮罩（见下方 UI 设置说明）
/// 5. 目标场景加入 Build Settings；玩家 Tag 设为 "Player"
/// 
/// 全屏遮罩 UI 设置（让画面能变白/变黑）：
/// 1. Hierarchy 右键 → UI → Canvas（会自动建 Canvas + EventSystem）
/// 2. Canvas 的 Render Mode 保持 "Screen Space - Overlay"
/// 3. Canvas 下右键 → UI → Image，命名 "FadeOverlay"
/// 4. 选中 FadeOverlay：
///    - Rect Transform：点左上角的 Anchor Presets，按住 Alt 点右下角那个"拉满"选项
///      （让它铺满全屏）
///    - Color：选纯白(255,255,255)或纯黑，并把 Alpha(A) 滑到 0（一开始透明）
/// 5. 把 FadeOverlay 拖到本脚本的 Fade Overlay 槽位
/// </summary>
public class PortalOnTouch : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("玩家碰到后要加载的场景名称（必须已加入 Build Settings）")]
    public string targetSceneName = "Chapter2";

    [Header("过渡设置")]
    [Tooltip("画面渐变到纯色需要多久（秒）")]
    public float fadeDuration = 1f;

    [Header("全屏遮罩（关键：防止穿帮）")]
    [Tooltip("一个铺满全屏的 UI Image，玩家碰门时它会渐渐显现盖住画面")]
    public UnityEngine.UI.Image fadeOverlay;

    [Tooltip("遮罩最终颜色。白门建议用白色，普通门用黑色")]
    public Color fadeColor = Color.white;

    [Header("音效（可选）")]
    public AudioClip portalSound;

    [Header("光效（可选）")]
    [Tooltip("穿越时光源会增强，拖入门里的 Point Light")]
    public Light portalLight;
    [Tooltip("穿越瞬间光强冲到多少")]
    public float flashIntensity = 20f;

    // ── 内部状态 ──
    private bool isTriggered = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 确保遮罩一开始是完全透明的
        if (fadeOverlay != null)
        {
            Color c = fadeColor;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isTriggered) return;

        StartCoroutine(DoTransition());
    }

    // ─────────────────────────────────────────────
    private IEnumerator DoTransition()
    {
        isTriggered = true;
        Debug.Log("[穿越门] 玩家碰到了门，穿越到：" + targetSceneName);

        // 播放穿越音效
        if (portalSound != null)
            audioSource.PlayOneShot(portalSound);

        // 光源增强
        if (portalLight != null)
            StartCoroutine(FlashLight());

        // 画面渐变到纯色（盖住穿帮画面）
        if (fadeOverlay != null)
            yield return StartCoroutine(FadeOverlay());
        else
        {
            // 没设置遮罩的话，至少等一下（但会穿帮，建议一定要设遮罩）
            Debug.LogWarning("[穿越门] 没有设置 Fade Overlay 遮罩，等待期间可能穿帮！");
            yield return new WaitForSeconds(fadeDuration);
        }

        // 切换场景
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>全屏遮罩从透明渐变到不透明（盖住画面）</summary>
    private IEnumerator FadeOverlay()
    {
        float elapsed = 0f;
        Color c = fadeColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        // 确保最后完全不透明
        c.a = 1f;
        fadeOverlay.color = c;
    }

    /// <summary>穿越瞬间光源由当前强度冲到最亮</summary>
    private IEnumerator FlashLight()
    {
        float startIntensity = portalLight.intensity;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            portalLight.intensity = Mathf.Lerp(startIntensity, flashIntensity, t);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 1f, 0.8f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 1f, 0.6f, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
