using UnityEngine;

// 挂在球体上（需要挂 Rigidbody，脚本会自动加）。
// 玩家靠近到 triggerDistance 范围内时，给球一个向右的推力，
// 球开始物理滚动，滚到边缘后靠重力自然坠落——用来诱导玩家跟着走错方向。
public class BallLureTrap : MonoBehaviour
{
    [Tooltip("留空自动按Tag查找")]
    public Transform player;
    [Tooltip("玩家进入这个距离内，球开始滚动")]
    public float triggerDistance = 5f;
    [Tooltip("滚动方向，世界坐标系。默认往右，根据场景实际朝向调整")]
    public Vector3 rollDirection = Vector3.right;
    [Tooltip("推力大小，越大滚得越快/越远")]
    public float rollForce = 3f;

    private Rigidbody rb;
    private bool triggered = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
    }

    void Update()
    {
        if (triggered) return;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= triggerDistance)
        {
            triggered = true;
            rb.AddForce(rollDirection.normalized * rollForce, ForceMode.VelocityChange);
        }
    }
}
