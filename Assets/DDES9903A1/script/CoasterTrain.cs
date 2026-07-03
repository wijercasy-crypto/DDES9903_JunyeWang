using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 过山车/火车式多节车厢沿路径移动。
/// 每节车厢在同一条路径上，各自朝向自己所在段的方向，过弯时整列车自然弯曲。
/// 每节车厢的间距可单独设置（carOffsets），适应车头比其他节长的情况。
/// </summary>
public class CoasterTrain : MonoBehaviour
{
    [Header("路径点（按顺序）")]
    public List<Transform> pathPoints = new List<Transform>();

    [Header("车厢（从车头到车尾顺序拖入）")]
    [Tooltip("每节车厢，第一个是车头")]
    public List<Transform> cars = new List<Transform>();

    [Header("每节车厢到车头的距离")]
    [Tooltip("和 Cars 一一对应。Element 0（车头）填 0，之后每节填它距车头的累计距离。" +
             "例：车头长5、其余各长3 → 填 0, 5, 8, 11")]
    public List<float> carOffsets = new List<float>();

    [Tooltip("carOffsets 没填时用的默认统一间距")]
    public float defaultSpacing = 3f;

    [Header("车厢朝向修正")]
    [Tooltip("若车横着/倒着，调这里。倒着填(0,180,0)，横着朝X填(0,90,0)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("速度")]
    public float speed = 8f;
    public bool loop = false;

    [Header("状态")]
    public bool isRunning = false;

    [Header("跑完回调")]
    public UnityEngine.Events.UnityEvent onFinished;

    private float headDistance = 0f;
    private float totalLength = 0f;
    private List<float> cumulative = new List<float>();

    private void Start()
    {
        BuildLengthTable();
    }

    private void BuildLengthTable()
    {
        cumulative.Clear();
        totalLength = 0f;
        cumulative.Add(0f);
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            if (pathPoints[i] == null || pathPoints[i + 1] == null) { cumulative.Add(totalLength); continue; }
            totalLength += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
            cumulative.Add(totalLength);
        }
    }

    public void StartRunning()
    {
        headDistance = 0f;
        isRunning = true;
    }

    public void StopRunning()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (pathPoints.Count < 2 || cars.Count == 0) return;

        headDistance += speed * Time.deltaTime;

        for (int c = 0; c < cars.Count; c++)
        {
            if (cars[c] == null) continue;

            // 每节车厢的间距：优先用 carOffsets，没填就用默认统一间距
            float offset = (c < carOffsets.Count) ? carOffsets[c] : c * defaultSpacing;
            float carDist = headDistance - offset;
            if (carDist < 0f) carDist = 0f;

            PlaceCarAtDistance(cars[c], carDist);
        }

        if (headDistance >= totalLength)
        {
            if (loop)
                headDistance = 0f;
            else
            {
                isRunning = false;
                onFinished?.Invoke();
                Debug.Log("[过山车] 跑完，停下");
            }
        }
    }

    private void PlaceCarAtDistance(Transform car, float dist)
    {
        dist = Mathf.Clamp(dist, 0f, totalLength);

        Vector3 pos = GetPositionAtDistance(dist);
        car.position = pos;

        // 朝向：看向前方一小段距离的点，让朝向连续平滑，不在过点时跳变
        float lookAhead = 3f;   // 往前看多远（米），越大越平滑但转弯越"迟钝"
        Vector3 aheadPos = GetPositionAtDistance(Mathf.Min(dist + lookAhead, totalLength));
        Vector3 dir = aheadPos - pos;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);
            car.rotation = Quaternion.Slerp(car.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    /// <summary>按路径上的距离取位置</summary>
    private Vector3 GetPositionAtDistance(float dist)
    {
        dist = Mathf.Clamp(dist, 0f, totalLength);

        int seg = 0;
        for (int i = 0; i < cumulative.Count - 1; i++)
        {
            if (dist >= cumulative[i] && dist <= cumulative[i + 1])
            {
                seg = i;
                break;
            }
        }
        if (seg >= pathPoints.Count - 1) seg = pathPoints.Count - 2;
        if (pathPoints[seg] == null || pathPoints[seg + 1] == null) return transform.position;

        float segStart = cumulative[seg];
        float segEnd = cumulative[seg + 1];
        float t = (segEnd - segStart > 0.001f) ? (dist - segStart) / (segEnd - segStart) : 0f;

        return Vector3.Lerp(pathPoints[seg].position, pathPoints[seg + 1].position, t);
    }
    private void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < pathPoints.Count - 1; i++)
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
    }
}