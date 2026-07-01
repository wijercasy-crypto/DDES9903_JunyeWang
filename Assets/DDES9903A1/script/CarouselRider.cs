using UnityEngine;

/// <summary>
/// 旋转木马乘坐系统（保留视角旋转版）
/// 玩家点击木马坐上去跟着转，坐着时仍可用鼠标环顾四周，再点击起身。
/// 
/// 适配 Unity StarterAssets 的 First Person Controller：
/// 该脚本移动和视角是一体的，所以坐下时不禁用它，而是把它的移动速度临时清零，
/// 这样玩家走不动（不会从木马上走开），但鼠标仍能转动视角。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在【玩家】物体上（EZPZ Player Flat Screen WASD）
/// 2. 确认场景里有 Tag=MainCamera 的摄像机
/// 3. 给所有木马座位打 Tag = "Rideable"
/// 4. 不需要再往 Movement Scripts 里拖东西了（脚本自动处理移动速度）
/// </summary>
public class CarouselRider : MonoBehaviour
{
    [Header("可乘坐物体识别")]
    [Tooltip("木马的 Tag。点中带这个 Tag 的物体就能坐上去")]
    public string rideableTag = "Rideable";

    [Header("乘坐设置")]
    [Tooltip("点击检测的最远距离（米）")]
    public float clickDistance = 8f;

    [Tooltip("坐上去后，玩家相对座位的高度偏移（米）")]
    public float seatHeightOffset = 1.0f;

    [Tooltip("坐上/起身时玩家移动的平滑速度")]
    public float moveSmoothSpeed = 8f;

    [Header("起身设置")]
    [Tooltip("起身后玩家退到木马旁边多远（米）")]
    public float dismountDistance = 2f;

    // ── 内部状态 ──
    private bool isRiding = false;
    private Camera playerCamera;

    private Transform currentHorse;
    private Vector3 seatLocalTarget;
    private bool isMovingToSeat = false;

    // First Person Controller 相关（用反射或直接引用控制移动速度）
    private MonoBehaviour fpController;       // First Person Controller 脚本
    private CharacterController charController;
    private float savedMoveSpeed;
    private float savedSprintSpeed;

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogWarning("[乘坐系统] 没找到主摄像机！请确认场景里有 Tag=MainCamera 的摄像机。");

        // 自动找到 First Person Controller 脚本
        // StarterAssets 的脚本类名是 "FirstPersonController"
        var allScripts = GetComponents<MonoBehaviour>();
        foreach (var s in allScripts)
        {
            if (s.GetType().Name == "FirstPersonController")
            {
                fpController = s;
                break;
            }
        }

        charController = GetComponent<CharacterController>();

        if (fpController == null)
            Debug.LogWarning("[乘坐系统] 没找到 FirstPersonController 脚本，坐下时可能锁不住移动。");
    }

    private void Update()
    {
        if (isRiding)
        {
            if (isMovingToSeat)
                SmoothMoveToSeat();
            else
                // 已坐稳：每帧锁定局部坐标，防止重力让玩家下沉/漂移
                transform.localPosition = seatLocalTarget;
        }

        if (IsLeftMouseClicked())
        {
            if (!isRiding)
                TryMount();
            else
                Dismount();
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
    private void TryMount()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, clickDistance))
        {
            Transform horse = FindRideableParent(hit.collider.transform);
            if (horse != null)
            {
                MountHorse(horse, hit.point);
            }
        }
    }

    private Transform FindRideableParent(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.gameObject.tag == rideableTag)
                return current;
            current = current.parent;
        }
        return null;
    }

    private void MountHorse(Transform horse, Vector3 hitPoint)
    {
        currentHorse = horse;
        isRiding = true;

        // 锁住移动（速度清零），但保留脚本启用 → 视角还能转
        LockMovement(true);

        // 玩家变成木马子物体 → 木马转，玩家跟转
        transform.SetParent(horse, true);

        Vector3 seatWorldPos = hitPoint + Vector3.up * seatHeightOffset;
        seatLocalTarget = horse.InverseTransformPoint(seatWorldPos);

        isMovingToSeat = true;

        Debug.Log("[乘坐系统] 坐上了木马：" + horse.name + "（再次点击起身，鼠标可转视角）");

        // 通知引导总管：玩家完成了这个设施的体验（触发熄灭+下一个发光）
        if (GuideManager.Instance != null)
            GuideManager.Instance.CompleteCurrent(horse);
    }

    private void SmoothMoveToSeat()
    {
        if (currentHorse == null) return;

        Vector3 targetWorld = currentHorse.TransformPoint(seatLocalTarget);
        transform.position = Vector3.Lerp(transform.position, targetWorld, moveSmoothSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorld) < 0.05f)
        {
            transform.localPosition = seatLocalTarget;
            isMovingToSeat = false;
            Debug.Log("[乘坐系统] 坐稳了，开始跟着木马转");
        }
    }

    /// <summary>坐稳后，每帧把玩家强制锁回座位的局部位置，覆盖 FPS 脚本的移动</summary>
    private void LateUpdate()
    {
        // 已经坐稳（不在移动过程中）时，强制保持在座位局部坐标
        if (isRiding && !isMovingToSeat && currentHorse != null)
        {
            transform.localPosition = seatLocalTarget;
        }
    }

    // ─────────────────────────────────────────────
    private void Dismount()
    {
        if (currentHorse == null) return;

        Vector3 awayDir = (transform.position - currentHorse.position);
        awayDir.y = 0;
        if (awayDir.sqrMagnitude < 0.01f) awayDir = -transform.forward;
        awayDir.Normalize();

        Vector3 dismountPos = transform.position + awayDir * dismountDistance;

        // 解除父子关系
        transform.SetParent(null, true);

        dismountPos.y = transform.position.y;
        transform.position = dismountPos;

        // 解锁移动
        LockMovement(false);

        isRiding = false;
        isMovingToSeat = false;
        currentHorse = null;

        Debug.Log("[乘坐系统] 起身离开了木马");
    }

    // ─────────────────────────────────────────────
    /// <summary>
    /// 锁住/解锁玩家移动（通过把 FirstPersonController 的速度设为0实现）
    /// 这样脚本保持启用，鼠标视角旋转不受影响
    /// </summary>
    private void LockMovement(bool locked)
    {
        if (fpController == null) return;

        var type = fpController.GetType();

        // FirstPersonController 里有 MoveSpeed 和 SprintSpeed 两个 public 字段
        var moveSpeedField = type.GetField("MoveSpeed");
        var sprintSpeedField = type.GetField("SprintSpeed");

        if (locked)
        {
            // 保存原速度，然后清零
            if (moveSpeedField != null)
            {
                savedMoveSpeed = (float)moveSpeedField.GetValue(fpController);
                moveSpeedField.SetValue(fpController, 0f);
            }
            if (sprintSpeedField != null)
            {
                savedSprintSpeed = (float)sprintSpeedField.GetValue(fpController);
                sprintSpeedField.SetValue(fpController, 0f);
            }
        }
        else
        {
            // 恢复原速度
            if (moveSpeedField != null)
                moveSpeedField.SetValue(fpController, savedMoveSpeed);
            if (sprintSpeedField != null)
                sprintSpeedField.SetValue(fpController, savedSprintSpeed);
        }
    }
}
