using UnityEngine;
using System.Collections;

// 挂在火车物体(Train)上。
// 在 FinalStopFarewellSequence 的 onSecondToLastReached 事件里拖上这个物体，
// 选这个脚本的 StartDeparture() 方法即可，不用改代码。
//
// 流程：鸣笛 -> 等一小段时间 -> 缓缓加速开动 -> 匀速离开
public class TrainDeparture : MonoBehaviour
{
    [Header("鸣笛")]
    public AudioSource whistleAudio;
    [Tooltip("鸣笛开始后，等多久火车才开始动")]
    public float delayBeforeMoving = 2f;

    [Header("行进")]
    [Tooltip("火车前进方向，世界坐标。一般是沿铁轨方向的单位向量，比如(0,0,1)或(1,0,0)，看你的火车朝向")]
    public Vector3 moveDirection = Vector3.forward;
    public float maxSpeed = 3f;
    [Tooltip("从静止加速到最大速度用多久，数值越大越有\"缓缓开动\"的感觉")]
    public float accelerationTime = 8f;

    [Header("可选：开出多远后自动停止")]
    [Tooltip("小于等于0表示不自动停，一直开下去")]
    public float autoStopDistance = 0f;

    private bool departing = false;

    // 挂到 UnityEvent 上直接调用这个
    public void StartDeparture()
    {
        if (departing) return;
        departing = true;
        StartCoroutine(DepartureRoutine());
    }

    IEnumerator DepartureRoutine()
    {
        if (whistleAudio != null) whistleAudio.Play();

        yield return new WaitForSeconds(delayBeforeMoving);

        Vector3 dir = moveDirection.normalized;
        Vector3 startPos = transform.position;
        float traveled = 0f;
        float speed = 0f;
        float t = 0f;

        // 缓缓加速阶段
        while (t < accelerationTime)
        {
            t += Time.deltaTime;
            speed = Mathf.Lerp(0f, maxSpeed, t / accelerationTime);
            float step = speed * Time.deltaTime;
            transform.position += dir * step;
            traveled += step;

            if (autoStopDistance > 0f && traveled >= autoStopDistance) yield break;
            yield return null;
        }

        // 匀速阶段
        while (true)
        {
            float step = maxSpeed * Time.deltaTime;
            transform.position += dir * step;
            traveled += step;

            if (autoStopDistance > 0f && traveled >= autoStopDistance) yield break;
            yield return null;
        }
    }
}
