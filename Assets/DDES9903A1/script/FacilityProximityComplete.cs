using UnityEngine;

/// <summary>
/// 设施靠近完成器 —— 玩家走近这个设施就算"完成体验"，通知引导总管。
/// 
/// 用途：
/// 给还没做专门交互（如旋转木马乘坐）的设施临时使用，让你能先测试整套发光引导流程。
/// 之后做好专门交互后，可以替换成交互触发。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在设施根物体上（与 GlowController 同一个物体）
/// 2. 调 completeDistance 设置玩家多近算完成
/// 3. 确认玩家 Tag = "Player"
/// 
/// 注意：只有当这个设施是"当前发光引导目标"时，靠近才会触发完成。
/// </summary>
public class FacilityProximityComplete : MonoBehaviour
{
    [Header("完成设置")]
    [Tooltip("玩家走到多近算完成体验（米）")]
    public float completeDistance = 4f;

    [Tooltip("只有这个设施正在发光时，靠近才算完成（推荐开启，符合引导顺序）")]
    public bool onlyWhenGlowing = true;

    // ── 内部 ──
    private Transform player;
    private GlowController glow;
    private bool hasCompleted = false;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        glow = GetComponent<GlowController>();
    }

    private void Update()
    {
        if (hasCompleted) return;
        if (player == null) return;

        // 如果要求"只在发光时"，但当前没发光，就不检测
        if (onlyWhenGlowing && glow != null && !glow.IsGlowing())
            return;

        // 计算水平距离
        Vector3 a = transform.position; a.y = 0;
        Vector3 b = player.position; b.y = 0;
        float dist = Vector3.Distance(a, b);

        if (dist <= completeDistance)
        {
            hasCompleted = true;
            Debug.Log($"[靠近完成] 玩家靠近了 {gameObject.name}，算作完成");

            if (GuideManager.Instance != null)
                GuideManager.Instance.CompleteCurrent(transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        int segments = 32;
        Vector3 prev = transform.position + new Vector3(completeDistance, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float ang = i * Mathf.PI * 2f / segments;
            Vector3 next = transform.position + new Vector3(Mathf.Cos(ang) * completeDistance, 0, Mathf.Sin(ang) * completeDistance);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
