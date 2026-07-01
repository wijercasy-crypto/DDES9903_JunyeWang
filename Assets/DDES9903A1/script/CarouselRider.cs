using UnityEngine;

/// <summary>
/// 旋转木马乘坐系统（配合 EZPZ Interactable General 使用版）
/// 不再自己检测鼠标点击，而是提供公开方法 RideThisSeat(Transform)，
/// 由每个座位上的 Interactable General 组件的 "On Primary Interact ()" 事件调用。
/// 
/// 【重要区别】：
/// 之前的版本挂在"玩家"身上，自己发射线检测。
/// 这个新版本挂在"玩家"身上，但由座位的 Interactable General 主动通知它"坐哪个座位"。
/// 
/// 挂载与配置步骤：
/// 1. 此脚本挂在【玩家】物体上（EZPZ Player Flat Screen WASD）—— 保持不变
/// 2. 每个座位（seat1 ~ seat1(13)）上，确保有 EZPZ 的 Interactable General 组件
///    （如果座位本来就有 Interactable General 就用现成的；没有就 Add Component 加一个）
/// 3. 在每个座位的 Interactable General 的 "On Primary Interact ()" 事件里：
///    - Object 槽位：拖入【玩家】物体（挂着这个 CarouselRider 的那个）
///    - 函数下拉：选 CarouselRider → RideThisSeat (Transform)
///    - 函数下面会多出一个 Transform 参数槽位：把【这个座位自己】拖进去
/// 4. 起身：坐着时再点任意座位（或用下面说的按键），会起身
/// 
/// 提示：14 个座位逐个配置有点繁琐，但只需做一次。
/// </summary>
public class CarouselRider : MonoBehaviour
{
    [Header("乘坐设置")]
    [Tooltip("坐上去后，玩家相对座位的高度偏移（米）。坐太高调小，陷进去调大")]
    public float seatHeightOffset = -0.05f;

    [Tooltip("坐上/起身时玩家移动的平滑速度")]
    public float moveSmoothSpeed = 8f;

    [Header("起身设置")]
    [Tooltip("起身后玩家退到座位旁边多远（米）")]
    public float dismountDistance = 2f;

    // ── 内部状态 ──
    private bool isRiding = false;
    private Transform currentSeat;
    private Vector3 seatLocalTarget;
    private bool isMovingToSeat = false;

    private MonoBehaviour fpController;
    private float savedMoveSpeed;
    private float savedSprintSpeed;

    private void Start()
    {
        // 自动找到 First Person Controller 脚本（用于坐下时锁移动、保留视角）
        var allScripts = GetComponents<MonoBehaviour>();
        foreach (var s in allScripts)
        {
            if (s.GetType().Name == "FirstPersonController")
            {
                fpController = s;
                break;
            }
        }
    }

    private void Update()
    {
        // 坐稳后每帧锁定在座位局部坐标（因为木马在转，要跟着转且不下沉）
        if (isRiding)
        {
            if (isMovingToSeat)
                SmoothMoveToSeat();
            else if (currentSeat != null)
                transform.localPosition = seatLocalTarget;
        }
    }

    // ─────────────────────────────────────────────
    // 公开方法：由座位的 Interactable General 调用
    // ─────────────────────────────────────────────

    /// <summary>
    /// 坐上指定的座位。挂到座位 Interactable General 的
    /// "On Primary Interact ()" 事件上，参数传入该座位自己的 Transform。
    /// 如果已经坐着，再次调用则起身。
    /// </summary>
    public void RideThisSeat(Transform seat)
    {
        if (seat == null) return;

        if (!isRiding)
        {
            MountSeat(seat);
        }
        else
        {
            // 已经在坐了 → 起身
            Dismount();
        }
    }

    /// <summary>坐上座位</summary>
    private void MountSeat(Transform seat)
    {
        currentSeat = seat;
        isRiding = true;

        // 锁移动（速度清零），保留视角旋转
        LockMovement(true);

        // 玩家变成座位的子物体 → 木马转，玩家跟转
        transform.SetParent(seat, true);

        // 目标位置：座位位置 + 高度偏移
        Vector3 seatWorldPos = seat.position + Vector3.up * seatHeightOffset;
        seatLocalTarget = seat.InverseTransformPoint(seatWorldPos);

        isMovingToSeat = true;

        Debug.Log("[乘坐系统] 坐上了座位：" + seat.name + "（再次点击座位起身，鼠标可转视角）");

        // 通知引导总管：玩家完成了旋转木马的体验（触发当前设施熄灭 + 下一个点亮）
        if (GuideManager.Instance != null)
        {
            // 座位是 Carousel_Rotate 的子物体，往上找到引导总管里登记的设施
            GuideManager.Instance.CompleteCurrent(seat);
        }
    }

    private void SmoothMoveToSeat()
    {
        if (currentSeat == null) return;

        Vector3 targetWorld = currentSeat.TransformPoint(seatLocalTarget);
        transform.position = Vector3.Lerp(transform.position, targetWorld, moveSmoothSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorld) < 0.05f)
        {
            transform.localPosition = seatLocalTarget;
            isMovingToSeat = false;
            Debug.Log("[乘坐系统] 坐稳了，开始跟着木马转");
        }
    }

    /// <summary>起身离开</summary>
    private void Dismount()
    {
        if (currentSeat == null) return;

        Vector3 awayDir = (transform.position - currentSeat.position);
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
        currentSeat = null;

        Debug.Log("[乘坐系统] 起身离开了木马");
    }

    // ─────────────────────────────────────────────
    /// <summary>锁住/解锁玩家移动（清零速度，保留视角旋转）</summary>
    private void LockMovement(bool locked)
    {
        if (fpController == null) return;

        var type = fpController.GetType();
        var moveSpeedField = type.GetField("MoveSpeed");
        var sprintSpeedField = type.GetField("SprintSpeed");

        if (locked)
        {
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
            if (moveSpeedField != null)
                moveSpeedField.SetValue(fpController, savedMoveSpeed);
            if (sprintSpeedField != null)
                sprintSpeedField.SetValue(fpController, savedSprintSpeed);
        }
    }
}
