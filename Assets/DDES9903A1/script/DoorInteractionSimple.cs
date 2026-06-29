using UnityEngine;
using System.Collections;

/// <summary>
/// 门开关交互（鼠标点击版，完全独立）
/// 玩家用屏幕中央准星看着门，点鼠标左键即可开/关门。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在门物体 SM_DoorBlackGrey_01 上
/// 2. 门必须有 Collider（Mesh Collider 或 Box Collider 都行）
///    —— 射线要靠 Collider 才能"打中"门
///    —— 注意：用于点击的 Collider 不要勾 Is Trigger
/// 3. 调 hingeOffset 把旋转轴推到门边缘（看黄色 Gizmo 球）
/// 4. 确认场景里有一个 Tag 为 "MainCamera" 的摄像机（第一人称的眼睛）
/// </summary>
public class DoorInteractionSimple : MonoBehaviour
{
    [Header("合页位置")]
    [Tooltip("旋转轴相对门中心的偏移。门沿X轴摆放时填 (0.5,0,0) 或 (-0.5,0,0)")]
    public Vector3 hingeOffset = new Vector3(0.5f, 0f, 0f);

    [Header("开门设置")]
    [Tooltip("门打开的角度，试 90 或 -90")]
    public float openAngle = 90f;

    [Tooltip("开关门动画时长（秒）")]
    public float animationDuration = 0.8f;

    [Header("点击检测")]
    [Tooltip("玩家最远能从多远点击开门（米）。设很大=任何距离都能点")]
    public float maxClickDistance = 100f;

    [Header("音效（可选）")]
    public AudioClip openSound;
    public AudioClip closeSound;

    // ── 内部状态 ──
    private bool isOpen = false;
    private bool isAnimating = false;
    private AudioSource audioSource;
    private Camera playerCamera;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 找到主摄像机（第一人称的眼睛）
        playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogWarning("[门] 没找到主摄像机！请确认场景里有 Tag=MainCamera 的摄像机。");
    }

    private void Update()
    {
        // 检测鼠标左键点击（兼容新旧输入系统）
        if (IsLeftMouseClicked())
        {
            TryClickDoor();
        }
    }

    // ─────────────────────────────────────────────
    // 鼠标点击检测（同时兼容新旧输入系统）
    // ─────────────────────────────────────────────
    private bool IsLeftMouseClicked()
    {
#if ENABLE_INPUT_SYSTEM
        // 新输入系统
        return UnityEngine.InputSystem.Mouse.current != null
            && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
        // 旧输入系统
        return Input.GetMouseButtonDown(0);
#endif
    }

    // ─────────────────────────────────────────────
    // 从屏幕中央发射射线，判断是否点中了这扇门
    // ─────────────────────────────────────────────
    private void TryClickDoor()
    {
        if (isAnimating) return;
        if (playerCamera == null) return;

        // 从屏幕正中央（准星位置）发射射线
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance))
        {
            // 射线打中的是不是"这扇门自己"？
            if (hit.collider.gameObject == this.gameObject)
            {
                ToggleDoor();
            }
        }
    }

    // ─────────────────────────────────────────────
    private void ToggleDoor()
    {
        float target = isOpen ? 0f : openAngle;
        float start = isOpen ? openAngle : 0f;
        StartCoroutine(RotateAroundHinge(start, target));
    }

    /// <summary>核心：绕"合页边"旋转门</summary>
    private IEnumerator RotateAroundHinge(float fromAngle, float toAngle)
    {
        isAnimating = true;

        AudioClip clip = isOpen ? closeSound : openSound;
        if (clip != null) audioSource.PlayOneShot(clip);

        Vector3 hingeWorldPos = transform.position + transform.TransformVector(hingeOffset);
        Vector3 axis = Vector3.up;

        float elapsed = 0f;
        float lastAngle = fromAngle;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            float currentAngle = Mathf.Lerp(fromAngle, toAngle, t);

            float deltaAngle = currentAngle - lastAngle;
            transform.RotateAround(hingeWorldPos, axis, deltaAngle);
            lastAngle = currentAngle;

            yield return null;
        }

        float finalDelta = toAngle - lastAngle;
        transform.RotateAround(hingeWorldPos, axis, finalDelta);

        isOpen = !isOpen;
        isAnimating = false;

        Debug.Log(isOpen ? "门已打开" : "门已关闭");
    }

    // ─────────────────────────────────────────────
    // Scene 视图显示合页位置（黄球）和旋转轴（绿线）
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Vector3 hingeWorldPos = transform.position + transform.TransformVector(hingeOffset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hingeWorldPos, 0.06f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(hingeWorldPos + Vector3.up * 1f, hingeWorldPos - Vector3.up * 1f);
    }
}
