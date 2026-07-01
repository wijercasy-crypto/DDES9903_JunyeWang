using UnityEngine;
using System.Collections;

/// <summary>
/// 双开门控制器 —— 点击时两扇门同时向相反方向打开
/// 
/// 挂载步骤：
/// 1. 在两扇门的父物体 "Gates" 上挂这个脚本
/// 2. 把两扇门分别拖到下面的 leftDoor 和 rightDoor 槽位
/// 3. 确认两扇门都有 Collider（Mesh Collider 即可），Is Trigger 不勾选
/// 4. 调 leftHingeOffset / rightHingeOffset 让黄球落在各自合页边
/// 5. 确认场景里有 Tag=MainCamera 的摄像机
/// </summary>
public class DoubleDoorController : MonoBehaviour
{
    [Header("两扇门")]
    [Tooltip("左边那扇门")]
    public Transform leftDoor;
    [Tooltip("右边那扇门")]
    public Transform rightDoor;

    [Header("合页位置（相对各自门中心的偏移）")]
    [Tooltip("左门合页偏移，黄球要落在左门外侧边缘")]
    public Vector3 leftHingeOffset = new Vector3(-0.5f, 0f, 0f);
    [Tooltip("右门合页偏移，黄球要落在右门外侧边缘")]
    public Vector3 rightHingeOffset = new Vector3(0.5f, 0f, 0f);

    [Header("开门设置")]
    [Tooltip("门打开的角度（两扇门会自动反向）")]
    public float openAngle = 90f;

    [Tooltip("开关门动画时长（秒）")]
    public float animationDuration = 1.2f;

    [Header("点击检测")]
    [Tooltip("玩家最远能从多远点击（米）")]
    public float maxClickDistance = 100f;

    [Header("音效（可选）")]
    public AudioClip openSound;
    public AudioClip closeSound;

    // ── 内部状态 ──
    private bool isOpen = false;
    private bool isAnimating = false;
    private Camera playerCamera;
    private AudioSource audioSource;

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogWarning("[双开门] 没找到主摄像机！请确认场景里有 Tag=MainCamera 的摄像机。");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (isAnimating) return;

        if (IsLeftMouseClicked())
        {
            TryClickDoor();
        }
    }

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
    // 准星射线检测：是否点中了任意一扇门
    // ─────────────────────────────────────────────
    private void TryClickDoor()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance))
        {
            // 点中的是不是左门或右门？
            Transform hitObj = hit.collider.transform;
            if (hitObj == leftDoor || hitObj == rightDoor
                || hitObj.IsChildOf(leftDoor) || hitObj.IsChildOf(rightDoor))
            {
                ToggleDoors();
            }
        }
    }

    // ─────────────────────────────────────────────
    public void ToggleDoors()
    {
        StopAllCoroutines();

        // 播放音效
        AudioClip clip = isOpen ? closeSound : openSound;
        if (clip != null) audioSource.PlayOneShot(clip);

        // 目标角度：开门 = openAngle，关门 = 0
        float target = isOpen ? 0f : openAngle;
        float start = isOpen ? openAngle : 0f;

        // 两扇门同时转，方向相反（左门 +，右门 -）
        StartCoroutine(RotateDoor(leftDoor, leftHingeOffset, start, target, true));
        StartCoroutine(RotateDoor(rightDoor, rightHingeOffset, start, target, false));

        // 用一个协程管理动画状态和最终状态切换
        StartCoroutine(ManageAnimationState());
    }

    /// <summary>旋转单扇门，绕它自己的合页边</summary>
    private IEnumerator RotateDoor(Transform door, Vector3 hingeOffset,
                                   float fromAngle, float toAngle, bool isLeft)
    {
        if (door == null) yield break;

        // 左门正向转，右门反向转
        float sign = isLeft ? 1f : -1f;

        Vector3 hingeWorldPos = door.position + door.TransformVector(hingeOffset);
        Vector3 axis = Vector3.up;

        float elapsed = 0f;
        float lastAngle = fromAngle;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            float currentAngle = Mathf.Lerp(fromAngle, toAngle, t);

            float deltaAngle = (currentAngle - lastAngle) * sign;
            door.RotateAround(hingeWorldPos, axis, deltaAngle);
            lastAngle = currentAngle;

            yield return null;
        }

        // 补齐到精确角度
        float finalDelta = (toAngle - lastAngle) * sign;
        door.RotateAround(hingeWorldPos, axis, finalDelta);
    }

    /// <summary>等动画播完，切换开/关状态</summary>
    private IEnumerator ManageAnimationState()
    {
        isAnimating = true;
        yield return new WaitForSeconds(animationDuration);
        isOpen = !isOpen;
        isAnimating = false;
        Debug.Log(isOpen ? "大门已打开" : "大门已关闭");
    }

    // ─────────────────────────────────────────────
    // Scene 视图显示两个合页位置（黄球 = 左门，青球 = 右门）
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (leftDoor != null)
        {
            Vector3 leftHinge = leftDoor.position + leftDoor.TransformVector(leftHingeOffset);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(leftHinge, 0.15f);
            Gizmos.DrawLine(leftHinge + Vector3.up * 2f, leftHinge - Vector3.up * 2f);
        }

        if (rightDoor != null)
        {
            Vector3 rightHinge = rightDoor.position + rightDoor.TransformVector(rightHingeOffset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(rightHinge, 0.15f);
            Gizmos.DrawLine(rightHinge + Vector3.up * 2f, rightHinge - Vector3.up * 2f);
        }
    }
}
