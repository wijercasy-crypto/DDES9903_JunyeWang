using UnityEngine;

/// <summary>
/// 过山车乘坐系统。玩家点座位坐上 → 车厢沿轨道跑一圈 → 跑完自动下车。
/// 挂在【玩家】物体上（和 CarouselRider 同一个玩家，或单独的玩家物体）。
/// </summary>
public class CoasterRider : MonoBehaviour
{
    [Header("过山车音乐")]
    [Tooltip("过山车的 CarouselMusic（挂在 RollerCoaster_Wagon 上）")]
    public CarouselMusic coasterMusic;
    [Header("坐车时禁用的碰撞（防止被车厢挤出）")]
    [Tooltip("玩家的 CharacterController 或 Collider，坐车时临时禁用")]
    public Collider playerCollider;
    public CharacterController playerController;
    [Header("过山车")]
    [Tooltip("整列车厢 RollerCoaster_Wagon（玩家会 SetParent 到它，跟着跑）")]
    public Transform wagon;

    [Tooltip("车厢移动脚本 CoasterTrain（挂在 wagon 上）")]
    public CoasterTrain cart;

    [Header("座位")]
    [Tooltip("玩家坐上后待的位置（车厢里的空物体）")]
    public Transform seatPoint;

    [Tooltip("玩家相对座位的高度偏移")]
    public float seatHeightOffset = 0f;

    // ── 内部状态 ──
    public bool isRiding = false;
    private Vector3 mountWorldPos;
    private Quaternion mountWorldRot;

    private MonoBehaviour fpController;
    private float savedMoveSpeed, savedSprintSpeed;

    private void Start()
    {
        // 找到第一人称控制器（坐下时锁移动）
        foreach (var s in GetComponents<MonoBehaviour>())
        {
            if (s.GetType().Name == "FirstPersonController")
            {
                fpController = s;
                break;
            }
        }

        // 车厢跑完时自动下车（订阅 CoasterCart 的 onFinished 事件）
        if (cart != null)
            cart.onFinished.AddListener(OnCoasterFinished);
    }
    private Transform currentSeat;   // 当前坐的座位
    private void Update()
    {
        
        // 坐着时按 Esc 强制下车
        if (isRiding && Input.GetKeyDown(KeyCode.Q))
        {
            Dismount();
            return;
        }

        // 坐着时，每帧把玩家钉死在座位位置（锁住走动，防止被 WASD 推走或掉下去）
        if (isRiding && currentSeat != null)
        {
            transform.position = currentSeat.position + Vector3.up * seatHeightOffset;
            // 位置钉死，但不锁摄像机旋转 —— 玩家仍可鼠标扭头看风景
        }
    }

    /// <summary>由座位的 Interactable General 调用，参数传入被点击的座位</summary>
    public void RideCoaster(Transform seat)
    {
        if (isRiding) { Dismount(); return; }
        Mount(seat);
    }

    private void Mount(Transform seat)
    {
        if (coasterMusic != null) coasterMusic.FadeIn();
        // 用反射设开关（因为 FirstPersonController 在 StarterAssets 命名空间，CoasterRider 里直接引用要加 using）
        if (fpController != null)
        {
            var f = fpController.GetType().GetField("movementLocked");
            if (f != null) f.SetValue(fpController, true);
        }
        if (seat == null) return;

        isRiding = true;
        currentSeat = seat;          // ← 记住座位
        mountWorldPos = transform.position;
        mountWorldRot = transform.rotation;

        LockMovement(true);
        transform.SetParent(seat, true);
   
        if (playerCollider != null) playerCollider.enabled = false;
        if (playerController != null) playerController.enabled = false;
        transform.position = seat.position + Vector3.up * seatHeightOffset;
        transform.rotation = seat.rotation;

        if (cart != null) cart.StartRunning();
        Debug.Log("[过山车] 坐上座位：" + seat.name + "，出发！");
    }
    private void Dismount()
    {
        if (!isRiding) return;
        if (coasterMusic != null) coasterMusic.FadeOut();
        // 玩家脱离车厢，传回上车点
        transform.SetParent(null, true);
        transform.position = mountWorldPos;
        transform.rotation = mountWorldRot;

        // 解锁移动（movementLocked 开关 + 旧的 LockMovement 都解，确保能动）
        if (fpController != null)
        {
            var f = fpController.GetType().GetField("movementLocked");
            if (f != null) f.SetValue(fpController, false);
        }
        LockMovement(false);

        // 恢复碰撞
        if (playerCollider != null) playerCollider.enabled = true;
        if (playerController != null) playerController.enabled = true;

        isRiding = false;
        currentSeat = null;
        Debug.Log("[过山车] 玩家下车，车继续跑向终点");
    }
    private void OnCoasterFinished()
    {
        if (isRiding)
            Dismount();
    }

    private void LockMovement(bool locked)
    {
        if (fpController == null) return;
        var type = fpController.GetType();
        var moveField = type.GetField("MoveSpeed");
        var sprintField = type.GetField("SprintSpeed");

        if (locked)
        {
            if (moveField != null) { savedMoveSpeed = (float)moveField.GetValue(fpController); moveField.SetValue(fpController, 0f); }
            if (sprintField != null) { savedSprintSpeed = (float)sprintField.GetValue(fpController); sprintField.SetValue(fpController, 0f); }
        }
        else
        {
            if (moveField != null) moveField.SetValue(fpController, savedMoveSpeed);
            if (sprintField != null) sprintField.SetValue(fpController, savedSprintSpeed);
        }
    }
}