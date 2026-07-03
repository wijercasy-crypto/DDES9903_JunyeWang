using UnityEngine;

/// <summary>
/// 通用乘坐系统（配合 EZPZ Interactable General）
/// 玩家坐上任意设施的座位，自动控制"那个设施"的旋转 / 音乐 / 回忆。
/// 组件都从座位往上查找，所以木马、火箭、以及以后同结构的设施都能共用这一个脚本。
/// 
/// 设施要求：座位是"旋转层"的子物体，旋转层上挂 SimpleRotate（可选 CarouselMusic / CarouselMemory）。
/// 配置：每个座位加 Interactable General，On Primary Interact 调用 玩家.RideThisSeat(该座位)。
/// </summary>
public class CarouselRider : MonoBehaviour
{
    [Header("坐上时触发的字幕（可选）")]
    [Tooltip("坐上这个设施时，显示这条字幕（比如出现在前一个设施上）")]
    public TriggeredSubtitle subtitleOnMount;
    [Header("木马照片墙的灯（坐上熄灭，只亮一次）")]
    public OneTimeLight photoWallLight;
    [Header("坐上时禁用的碰撞（防止被挤出）")]
    public Collider playerCollider;
    public CharacterController playerController;
    [Header("乘坐设置")]
    [Tooltip("坐上去后，玩家相对座位的高度偏移（米）")]
    public float seatHeightOffset = -0.05f;

    [Tooltip("坐上/起身时玩家移动的平滑速度")]
    public float moveSmoothSpeed = 8f;

    // ── 内部状态 ──
    public bool isRiding = false;
    private Transform currentSeat;
    private Vector3 seatLocalTarget;
    private bool isMovingToSeat = false;

    private Vector3 mountWorldPos;
    private Quaternion mountWorldRot;

    private MonoBehaviour fpController;
    private float savedMoveSpeed;
    private float savedSprintSpeed;

    // 当前所坐设施的组件（坐上时从座位查找）
    private SimpleRotate currentRotate;
    private CarouselMusic currentMusic;
    private CarouselMemory currentMemory;

    private void Start()
    {
        // 找到 First Person Controller 脚本（坐下时锁移动）
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
        // 坐着时按 Esc 起身
        if (isRiding && Input.GetKeyDown(KeyCode.Escape))
        {
            Dismount();
            return;
        }

        if (isRiding)
        {
            if (isMovingToSeat)
                SmoothMoveToSeat();
            else if (currentSeat != null)
                transform.localPosition = seatLocalTarget;
        }
    }

    public void RideThisSeat(Transform seat)
    {
        if (seat == null) return;

        if (!isRiding)
            MountSeat(seat);
        else
            Dismount();
    }

    /// <summary>坐上座位</summary>
    private void MountSeat(Transform seat)
    {
        if (subtitleOnMount != null) subtitleOnMount.Show();
        if (photoWallLight != null) photoWallLight.TurnOffForever();
        currentSeat = seat;
        isRiding = true;

        mountWorldPos = transform.position;
        mountWorldRot = transform.rotation;

        LockMovement(true);

        // 玩家变成座位的子物体 → 设施转，玩家跟转
        transform.SetParent(seat, true);

        Vector3 seatWorldPos = seat.position + Vector3.up * seatHeightOffset;
        seatLocalTarget = seat.InverseTransformPoint(seatWorldPos);
        isMovingToSeat = true;

        Debug.Log("[乘坐系统] 坐上了座位：" + seat.name);

        // 从这个座位往上找它所属设施的各组件（木马/火箭通用）
        currentRotate = seat.GetComponentInParent<SimpleRotate>();
        currentMusic = seat.GetComponentInParent<CarouselMusic>();
        currentMemory = seat.GetComponentInParent<CarouselMemory>();
        // MountSeat 里，锁移动那部分加：
        if (fpController != null)
        {
            var f = fpController.GetType().GetField("movementLocked");
            if (f != null) f.SetValue(fpController, true);   // 停止 Move()，不再报 inactive 错
        }
        if (playerCollider != null) playerCollider.enabled = false;
        if (playerController != null) playerController.enabled = false;
        // 启动该设施的旋转 / 回忆 / 音乐（都在 GuideManager 判断之外，保证一定执行）
        if (currentRotate != null) currentRotate.StartRotating();
        if (currentMemory != null) currentMemory.BeginMemory();
        if (currentMusic != null) currentMusic.FadeIn();

        // 通知引导总管（如果有）
        if (GuideManager.Instance != null)
            GuideManager.Instance.CompleteCurrent(seat);
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
            Debug.Log("[乘坐系统] 坐稳了，开始跟着转");
        }
    }

    /// <summary>起身离开，回到上车的地方</summary>
    private void Dismount()
    {
        if (currentSeat == null) return;

        transform.SetParent(null, true);
        transform.position = mountWorldPos;
        transform.rotation = mountWorldRot;

        LockMovement(false);
        // Dismount 里，解锁移动那部分加：
        if (fpController != null)
        {
            var f = fpController.GetType().GetField("movementLocked");
            if (f != null) f.SetValue(fpController, false);
        }
        // 停止该设施的旋转 / 回忆 / 音乐
        if (currentRotate != null) currentRotate.StopRotating();
        if (currentMemory != null) currentMemory.EndMemory();
        if (currentMusic != null) currentMusic.FadeOut();

        if (playerCollider != null) playerCollider.enabled = true;
        if (playerController != null) playerController.enabled = true;
        isRiding = false;
        isMovingToSeat = false;
        currentSeat = null;
        currentRotate = null;
        currentMusic = null;
        currentMemory = null;

        Debug.Log("[乘坐系统] 起身，回到了上车的地方");
    }

    /// <summary>供外部调用的强制下车（转满一圈后自动下车）</summary>
    public void ForceDismount()
    {
        if (isRiding)
            Dismount();
    }

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