using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 匀速旋转，可设定转满几圈后自动停下并触发事件。
/// </summary>
public class SimpleRotate : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转速度（度/秒）")]
    public float rotateSpeed = 20f;

    [Tooltip("绕哪个轴转，默认 Y 轴")]
    public Vector3 axis = Vector3.up;

    [Header("自动停止")]
    [Tooltip("转满多少圈后自动停下。1 = 转一圈。设 0 或负数表示不自动停、一直转")]
    public float autoStopTurns = 1f;

    [Header("状态")]
    public bool isRotating = false;

    [Header("转完回调")]
    [Tooltip("转满设定圈数后触发（比如让黑幕消失）")]
    public UnityEvent onFinished;

    private float accumulatedAngle = 0f;   // 已累计转过的角度

    private void Update()
    {
        if (!isRotating) return;

        float delta = rotateSpeed * Time.deltaTime;
        transform.Rotate(axis, delta, Space.Self);

        // 累计角度（用绝对值，防止负速度算不对）
        accumulatedAngle += Mathf.Abs(delta);

        // 如果设了自动停，且转够了 → 停下并触发回调
        if (autoStopTurns > 0f && accumulatedAngle >= autoStopTurns * 360f)
        {
            isRotating = false;
            accumulatedAngle = 0f;
            onFinished?.Invoke();
            Debug.Log("[旋转] 转满，自动停止");
        }
    }

    public void StartRotating()
    {
        accumulatedAngle = 0f;   // 每次开始重新计数
        isRotating = true;
    }

    public void StopRotating()
    {
        isRotating = false;
    }
}