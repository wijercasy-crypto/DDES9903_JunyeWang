using UnityEngine;
using System.Collections;

// 挂在门物体上（比如 SM_DoorBlackGrey_01）。
// 调用 Open() 让门绕Y轴缓缓转动指定角度，模拟开门。
public class DoorOpen : MonoBehaviour
{
    [Tooltip("开门转动多少度")]
    public float openAngle = 120f;
    [Tooltip("开门动画持续多久")]
    public float openDuration = 1.5f;
    [Tooltip("绕哪个轴转，默认Y轴（竖直方向的门轴）")]
    public Vector3 rotationAxis = Vector3.up;

    private bool opened = false;

    // 挂在 FearEyeManager 某个阶段的 On Stage Triggered() 上
    public void Open()
    {
        if (opened) return;
        opened = true;
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.AngleAxis(openAngle, rotationAxis);

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.SmoothStep(0f, 1f, t / openDuration);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, pct);
            yield return null;
        }
        transform.rotation = targetRot;
    }
}
