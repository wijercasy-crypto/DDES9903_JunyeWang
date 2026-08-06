using UnityEngine;
using System.Collections;

// 挂在风滚草模型上。由 FinalStopFarewellSequence 在合适的时机调用 RollAcross()。
// 风滚草会在"玩家与残影连线"的某个点上，沿垂直于视线的方向滚过去，制造遮挡感。
public class TumbleweedRoller : MonoBehaviour
{
    [Header("滚动参数")]
    public float rollDuration = 1.4f;   // 滚过去用多久
    public float crossWidth = 6f;       // 滚动路径左右各延伸多远
    public float spinSpeed = 480f;      // 自转速度(度/秒)，做出滚动感

    public bool showDebugLogs = true;

    private Renderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        gameObject.SetActive(false);
    }

    public IEnumerator RollAcross(Vector3 crossPoint, Vector3 viewDir, float groundY)
    {
        if (showDebugLogs) Debug.Log($"[Tumbleweed] RollAcross开始执行，crossPoint={crossPoint}");

        viewDir.y = 0f;
        if (viewDir.sqrMagnitude < 0.001f) viewDir = Vector3.forward;
        Vector3 perpendicular = Vector3.Cross(viewDir.normalized, Vector3.up);

        Vector3 start = crossPoint - perpendicular * crossWidth;
        Vector3 end = crossPoint + perpendicular * crossWidth;
        start.y = groundY;
        end.y = groundY;

        transform.position = start;
        gameObject.SetActive(true);

        if (showDebugLogs) Debug.Log($"[Tumbleweed] 已SetActive(true)，activeSelf={gameObject.activeSelf}，activeInHierarchy={gameObject.activeInHierarchy}，start={start}，end={end}");

        float t = 0f;
        while (t < rollDuration)
        {
            t += Time.deltaTime;
            float pct = t / rollDuration;
            transform.position = Vector3.Lerp(start, end, pct);
            transform.Rotate(perpendicular, spinSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        gameObject.SetActive(false);
        if (showDebugLogs) Debug.Log("[Tumbleweed] RollAcross结束，已隐藏");
    }
}
