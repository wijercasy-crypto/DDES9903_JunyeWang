using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 传送门 —— 准星看着发光体，点鼠标左键，切换到下一个场景
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在 PortalTrigger 物体上（洞口的发光片/球）
/// 2. PortalTrigger 必须有 Collider，且 Is Trigger 不勾选（射线才能打中）
/// 3. 在 Inspector 的 Target Scene Name 填入目标场景名，例如 "Chapter1"
///    —— 名字必须和 Scenes 文件夹里的场景文件名完全一致
/// 4. 把目标场景加入 Build Settings（File → Build Settings → Add Open Scenes）
/// 5. 确认场景里有 Tag=MainCamera 的摄像机
/// </summary>
public class PortalToScene : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("点击后要加载的场景名称（必须已加入 Build Settings）")]
    public string targetSceneName = "Chapter1";

    [Header("点击检测")]
    [Tooltip("玩家最远能从多远点击（米）")]
    public float maxClickDistance = 100f;

    [Header("过渡设置")]
    [Tooltip("点击后等待多久再切场景（秒），留点时间给音效/光效")]
    public float transitionDelay = 0.8f;

    [Header("音效（可选）")]
    [Tooltip("穿越时播放的音效")]
    public AudioClip portalSound;

    [Header("光效（可选）")]
    [Tooltip("点击时光源会增强，拖入洞口的 Point Light")]
    public Light portalLight;
    [Tooltip("点击瞬间光强冲到多少")]
    public float flashIntensity = 12f;

    // ── 内部状态 ──
    private bool isTriggered = false;
    private Camera playerCamera;
    private AudioSource audioSource;

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogWarning("[传送门] 没找到主摄像机！请确认场景里有 Tag=MainCamera 的摄像机。");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (isTriggered) return;

        if (IsLeftMouseClicked())
        {
            TryClickPortal();
        }
    }

    // ─────────────────────────────────────────────
    // 鼠标点击检测（兼容新旧输入系统）
    // ─────────────────────────────────────────────
    private bool IsLeftMouseClicked()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Mouse.current != null
            && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    // ─────────────────────────────────────────────
    // 从屏幕中央发射射线，判断是否点中了传送门
    // ─────────────────────────────────────────────
    private void TryClickPortal()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                StartCoroutine(DoTransition());
            }
        }
    }

    // ─────────────────────────────────────────────
    // 穿越流程
    // ─────────────────────────────────────────────
    private IEnumerator DoTransition()
    {
        isTriggered = true;
        Debug.Log("[传送门] 穿越到：" + targetSceneName);

        // 播放穿越音效
        if (portalSound != null)
            audioSource.PlayOneShot(portalSound);

        // 光源增强效果（光芒大盛，吞没画面）
        if (portalLight != null)
            StartCoroutine(FlashLight());

        // 等待过渡时间
        yield return new WaitForSeconds(transitionDelay);

        // 切换场景
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>点击瞬间光源由当前强度冲到最亮</summary>
    private IEnumerator FlashLight()
    {
        float startIntensity = portalLight.intensity;
        float elapsed = 0f;

        while (elapsed < transitionDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDelay;
            portalLight.intensity = Mathf.Lerp(startIntensity, flashIntensity, t);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────
    // Scene 视图标出传送门位置（青色框）
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
