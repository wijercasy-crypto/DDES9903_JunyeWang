using UnityEngine;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// 引导球（平滑曲线 + 动态速度版）
/// 玩家靠得越近，球滚得越快；玩家越远球越慢，太远则停下。
/// 转弯处用 Catmull-Rom 样条曲线，圆滑不急转。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在皮球 PP_Beach_Ball_01 上
/// 2. 在 Waypoints 列表里按剧情顺序拖入游乐设施（不要留空格子）
/// 3. 玩家 Tag 设为 "Player"
/// 4. 调速度区间 minMoveSpeed / maxMoveSpeed
/// </summary>
/// 
public class GuideBall : MonoBehaviour
{
    [Header("引导路径点（按剧情顺序拖入设施）")]
    [Tooltip("球会依次滚向这些位置。顺序就是引导顺序。不要留 None 空格子！")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("触发设置")]
    [Tooltip("玩家离球多近时，球开始滚向下一个设施（米）")]
    public float triggerDistance = 4f;

    [Tooltip("玩家离球多远时，球停止滚动（米）。应比 triggerDistance 大一些")]
    public float stopDistance = 8f;

    [Header("动态速度设置")]
    [Tooltip("球的最慢速度（玩家在停止距离边缘时）米/秒")]
    public float minMoveSpeed = 1f;

    [Tooltip("球的最快速度（玩家贴得很近时）米/秒")]
    public float maxMoveSpeed = 5f;

    [Tooltip("玩家贴多近时球达到最快速度（米）。小于这个距离都是最快")]
    public float fullSpeedDistance = 2f;

    [Tooltip("速度变化的平滑度，越大反应越平缓（防止忽快忽慢）")]
    public float speedSmoothing = 5f;

    [Header("登场弹跳（吸引注意）")]
    [Tooltip("玩家第一次靠近时，球先原地弹跳几下引起注意")]
    public bool bounceOnFirstApproach = true;

    [Tooltip("玩家离球多近时触发弹跳（米）。独立于滚动的触发距离，可设得比它大，让球更早蹦起来吸引注意")]
    public float bounceTriggerDistance = 10f;   // ← 新增，默认比 triggerDistance 大

    [Tooltip("弹跳次数")]
    public int bounceCount = 3;

    [Tooltip("每次弹跳的高度（米）")]
    public float bounceHeight = 1.2f;

    [Tooltip("每次弹跳的时长（秒），越小弹得越急促")]
    public float bounceDuration = 0.4f;
    [Header("滚动外观")]
    [Tooltip("球的半径（米），让旋转和移动匹配")]
    public float ballRadius = 0.5f;

    [Header("曲线平滑度")]
    [Tooltip("曲线分段数，越大越圆滑（建议 20）")]
    public int curveResolution = 20;

    [Header("调试")]
    public bool showDebugLog = true;

    // ── 内部状态 ──
    private bool hasBounced = false;   // 是否已经弹跳过（只弹一次）
    private bool isBouncing = false;   // 正在弹跳中
    private float groundY;             // 球的地面高度，弹跳落地基准
    private int currentWaypointIndex = -1;
    private bool isMoving = false;
    private Transform player;
    private float currentSpeed = 0f;   // 当前实际速度（平滑过渡用）

    private List<Vector3> currentPath = new List<Vector3>();
    private int pathStepIndex = 0;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("[引导球] 没找到 Tag=Player 的玩家！");

        if (waypoints.Count == 0)
            Debug.LogWarning("[引导球] Waypoints 列表是空的！请拖入游乐设施。");
        groundY = transform.position.y;
    }

    private void Update()
    {
        if (player == null) return;
        if (waypoints.Count == 0) return;

        Vector3 ballPos = transform.position; ballPos.y = 0;
        Vector3 playerPos = player.position; playerPos.y = 0;
        float distanceToPlayer = Vector3.Distance(ballPos, playerPos);

        if (isMoving)
        {
            if (distanceToPlayer > stopDistance)
            {
                isMoving = false;
                currentSpeed = 0f;
                if (showDebugLog) Debug.Log("[引导球] 玩家太远，球停下了");
                return;
            }

            // 根据玩家距离计算目标速度
            float targetSpeed = CalculateTargetSpeed(distanceToPlayer);

            // 平滑过渡到目标速度，避免忽快忽慢
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmoothing * Time.deltaTime);

            FollowPath();
        }
        else
        {
            if (isBouncing) return;   // 正在弹跳时，先别做别的

            // ① 弹跳判定：用独立的 bounceTriggerDistance，只弹一次
            if (bounceOnFirstApproach && !hasBounced
                && distanceToPlayer <= bounceTriggerDistance
                && currentWaypointIndex < waypoints.Count - 1)
            {
                hasBounced = true;
                StartCoroutine(BounceThenStart());
                return;
            }

            // ② 滚动判定：用原来的 triggerDistance
            if (distanceToPlayer <= triggerDistance && currentWaypointIndex < waypoints.Count - 1)
            {
                currentWaypointIndex++;
                BuildSmoothPath();
                isMoving = true;
                pathStepIndex = 0;

                if (showDebugLog && waypoints[currentWaypointIndex] != null)
                    Debug.Log($"[引导球] 球滚向第 {currentWaypointIndex + 1} 个设施：{waypoints[currentWaypointIndex].name}");
            }
        }
    }
        
    /// <summary>先弹跳几下吸引注意，弹完再开始滚向第一个设施</summary>
    private IEnumerator BounceThenStart()
    {
        isBouncing = true;
        if (showDebugLog) Debug.Log("[引导球] 登场弹跳，吸引玩家注意！");

        yield return StartCoroutine(DoBounces());

        isBouncing = false;

        // 弹完立刻开始滚向第一个设施
        currentWaypointIndex++;
        BuildSmoothPath();
        isMoving = true;
        pathStepIndex = 0;
    }

    /// <summary>执行 bounceCount 次弹跳，每次用 sin 曲线做出上抛落地的手感</summary>
    private IEnumerator DoBounces()
    {
        for (int b = 0; b < bounceCount; b++)
        {
            // 每次弹跳高度递减一点，更自然（第一下最高，后面越来越低）
            float thisHeight = bounceHeight * (1f - b * 0.2f);
            if (thisHeight < 0.1f) thisHeight = 0.1f;

            float elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;

                // sin(0→π) 形成一个上去再下来的弧线，t=0.5 时最高
                float height = Mathf.Sin(t * Mathf.PI) * thisHeight;

                Vector3 pos = transform.position;
                pos.y = groundY + height;
                transform.position = pos;

                // 顺便让球转一点，看着更活泼（可选）
                transform.Rotate(Vector3.right, 360f * Time.deltaTime, Space.World);

                yield return null;
            }
        }

        // 确保最后精确落回地面
        Vector3 finalPos = transform.position;
        finalPos.y = groundY;
        transform.position = finalPos;
    }
    /// <summary>
    /// 根据玩家距离计算球应有的速度
    /// 玩家越近 → 越快；玩家越远 → 越慢
    /// </summary>
    private float CalculateTargetSpeed(float distanceToPlayer)
    {
        // 把距离映射到 0~1：
        // distanceToPlayer = fullSpeedDistance（很近）→ t=0 → 最快
        // distanceToPlayer = stopDistance（很远）   → t=1 → 最慢
        float t = Mathf.InverseLerp(fullSpeedDistance, stopDistance, distanceToPlayer);

        // t=0 时用 maxMoveSpeed，t=1 时用 minMoveSpeed
        float speed = Mathf.Lerp(maxMoveSpeed, minMoveSpeed, t);
        return speed;
    }

    private void BuildSmoothPath()
    {
        currentPath.Clear();

        Vector3 p0, p1, p2, p3;
        p1 = transform.position;
        p2 = GetWaypointPos(currentWaypointIndex);

        if (currentWaypointIndex - 1 >= 0)
            p0 = GetWaypointPos(currentWaypointIndex - 1);
        else
            p0 = p1 + (p1 - p2).normalized * 2f;

        if (currentWaypointIndex + 1 < waypoints.Count)
            p3 = GetWaypointPos(currentWaypointIndex + 1);
        else
            p3 = p2 + (p2 - p1).normalized * 2f;

        for (int i = 0; i <= curveResolution; i++)
        {
            float t = i / (float)curveResolution;
            currentPath.Add(CatmullRom(p0, p1, p2, p3, t));
        }
    }

    private void FollowPath()
    {
        if (currentPath.Count == 0) { isMoving = false; return; }

        Vector3 currentPos = transform.position;
        Vector3 nextPoint = currentPath[pathStepIndex];
        nextPoint.y = currentPos.y;

        Vector3 direction = (nextPoint - currentPos);
        float step = currentSpeed * Time.deltaTime;   // 用动态速度

        if (direction.magnitude > 0.001f)
        {
            Vector3 dirNorm = direction.normalized;
            transform.position = Vector3.MoveTowards(currentPos, nextPoint, step);

            if (ballRadius > 0.01f)
            {
                Vector3 rotationAxis = Vector3.Cross(Vector3.up, dirNorm);
                float rotationAngle = (step / ballRadius) * Mathf.Rad2Deg;
                transform.Rotate(rotationAxis, rotationAngle, Space.World);
            }
        }

        Vector3 flatCur = transform.position; flatCur.y = 0;
        Vector3 flatNext = currentPath[pathStepIndex]; flatNext.y = 0;
        if (Vector3.Distance(flatCur, flatNext) <= 0.15f)
        {
            pathStepIndex++;
            if (pathStepIndex >= currentPath.Count)
            {
                isMoving = false;
                currentSpeed = 0f;
                if (showDebugLog)
                    Debug.Log($"[引导球] 已到达第 {currentWaypointIndex + 1} 个设施，等待玩家");

                if (currentWaypointIndex >= waypoints.Count - 1)
                {
                    if (showDebugLog) Debug.Log("[引导球] 已到达最后一个设施！");
                    OnReachedFinalWaypoint();
                }
            }
        }
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 GetWaypointPos(int index)
    {
        if (index < 0 || index >= waypoints.Count) return transform.position;
        if (waypoints[index] == null)
        {
            Debug.LogWarning($"[引导球] 第 {index + 1} 个 Waypoint 是空的！请检查 Inspector。");
            return transform.position;
        }
        return waypoints[index].position;
    }

    private void OnReachedFinalWaypoint()
    {
        // TODO: 皮球停在白门前的逻辑
    }

    // ─────────────────────────────────────────────
    // Scene 视图可视化
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        DrawCircle(transform.position, triggerDistance);
        Gizmos.color = Color.red;
        DrawCircle(transform.position, stopDistance);

        // 蓝圈：贴到这个距离内球达到最快速度
        Gizmos.color = Color.cyan;
        DrawCircle(transform.position, fullSpeedDistance);

        if (waypoints != null && waypoints.Count >= 1)
        {
            Gizmos.color = Color.yellow;
            List<Vector3> previewPoints = new List<Vector3>();
            previewPoints.Add(transform.position);
            for (int i = 0; i < waypoints.Count; i++)
                if (waypoints[i] != null) previewPoints.Add(waypoints[i].position);

            for (int i = 0; i < previewPoints.Count - 1; i++)
            {
                Vector3 p0 = (i - 1 >= 0) ? previewPoints[i - 1] : previewPoints[i];
                Vector3 p1 = previewPoints[i];
                Vector3 p2 = previewPoints[i + 1];
                Vector3 p3 = (i + 2 < previewPoints.Count) ? previewPoints[i + 2] : previewPoints[i + 1];

                Vector3 prev = p1;
                for (int s = 1; s <= 15; s++)
                {
                    float t = s / 15f;
                    Vector3 pt = CatmullRom(p0, p1, p2, p3, t);
                    Gizmos.DrawLine(prev, pt);
                    prev = pt;
                }
            }

            Gizmos.color = new Color(1f, 0.6f, 0f);
            for (int i = 0; i < waypoints.Count; i++)
                if (waypoints[i] != null) Gizmos.DrawSphere(waypoints[i].position, 0.3f);
        }
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        int segments = 32;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
