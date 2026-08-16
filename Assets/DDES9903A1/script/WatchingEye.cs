using UnityEngine;
using System.Collections;

// 挂在眼睛的父物体上（eyeball + upperEyelid + lowerEyelid 的共同父级）。
// 眼球转向盯着玩家；眨眼时上下眼皮转动闭合盖住眼球，不再是缩放整个眼球模拟。
public class WatchingEye : MonoBehaviour
{
    [Header("组成部件（拖三个子物体进来）")]
    public Transform eyeball;
    public Transform upperEyelid;
    public Transform lowerEyelid;

    [Header("玩家")]
    [Tooltip("留空自动按Tag查找")]
    public Transform player;

    [Header("眨眼闭合角度")]
    [Tooltip("上眼皮闭眼时的局部旋转角度。先在Scene里手动转upperEyelid到\"闭眼\"效果，把Inspector里当时的Rotation数值抄过来填这里")]
    public Vector3 upperEyelidClosedRotation;
    [Tooltip("下眼皮闭眼时的局部旋转角度，同上")]
    public Vector3 lowerEyelidClosedRotation;

    [Header("眨眼节奏")]
    public float blinkIntervalMin = 2f;
    public float blinkIntervalMax = 6f;
    public float blinkDuration = 0.15f;

    [Header("眼球转向速度")]
    [Tooltip("越大转得越快越生硬，越小越迟缓诡异")]
    public float lookSpeed = 3f;

    private Vector3 upperEyelidOpenRotation;
    private Vector3 lowerEyelidOpenRotation;
    private float nextBlinkTime;

    void OnEnable()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 记住"睁眼"时的初始角度，眨完眼要转回来
        if (upperEyelid != null) upperEyelidOpenRotation = upperEyelid.localEulerAngles;
        if (lowerEyelid != null) lowerEyelidOpenRotation = lowerEyelid.localEulerAngles;

        ScheduleNextBlink();
    }

    void Update()
    {
        if (eyeball != null && player != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(player.position - eyeball.position);
            eyeball.rotation = Quaternion.Slerp(eyeball.rotation, targetRot, lookSpeed * Time.deltaTime);
        }

        if (Time.time >= nextBlinkTime)
        {
            StartCoroutine(Blink());
            ScheduleNextBlink();
        }
    }

    void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
    }

    IEnumerator Blink()
    {
        float half = blinkDuration * 0.5f;
        float t = 0f;

        // 闭眼
        while (t < half)
        {
            t += Time.deltaTime;
            float pct = t / half;
            if (upperEyelid != null)
                upperEyelid.localEulerAngles = Vector3.Lerp(upperEyelidOpenRotation, upperEyelidClosedRotation, pct);
            if (lowerEyelid != null)
                lowerEyelid.localEulerAngles = Vector3.Lerp(lowerEyelidOpenRotation, lowerEyelidClosedRotation, pct);
            yield return null;
        }

        // 睁开
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float pct = t / half;
            if (upperEyelid != null)
                upperEyelid.localEulerAngles = Vector3.Lerp(upperEyelidClosedRotation, upperEyelidOpenRotation, pct);
            if (lowerEyelid != null)
                lowerEyelid.localEulerAngles = Vector3.Lerp(lowerEyelidClosedRotation, lowerEyelidOpenRotation, pct);
            yield return null;
        }

        if (upperEyelid != null) upperEyelid.localEulerAngles = upperEyelidOpenRotation;
        if (lowerEyelid != null) lowerEyelid.localEulerAngles = lowerEyelidOpenRotation;
    }
}
