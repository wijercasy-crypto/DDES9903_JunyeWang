using UnityEngine;
using System.Collections;

// 挂在每一个眼睛模型上。眼睛会转向盯着玩家，并且按随机间隔眨眼。
// 眨眼是用缩放Y轴模拟的(压扁再复原)，不需要眼睛模型本身有眨眼动画。
public class WatchingEye : MonoBehaviour
{
    [Tooltip("留空自动按Tag查找玩家")]
    public Transform player;
    [Tooltip("要转向玩家的部分，留空就是整个物体自己转")]
    public Transform eyeball;

    [Header("眨眼")]
    public float blinkIntervalMin = 2f;
    public float blinkIntervalMax = 6f;
    public float blinkDuration = 0.15f;

    [Header("转向速度")]
    [Tooltip("每秒转向玩家的插值速度，越大转得越快越生硬，越小越迟缓诡异")]
    public float lookSpeed = 3f;

    private Vector3 baseScale;
    private float nextBlinkTime;

    void OnEnable()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        Transform t = eyeball != null ? eyeball : transform;
        baseScale = t.localScale;
        ScheduleNextBlink();
    }

    void Update()
    {
        Transform t = eyeball != null ? eyeball : transform;

        if (player != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(player.position - t.position);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, lookSpeed * Time.deltaTime);
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
        Transform t = eyeball != null ? eyeball : transform;
        float half = blinkDuration * 0.5f;
        float time = 0f;

        while (time < half)
        {
            time += Time.deltaTime;
            float pct = time / half;
            t.localScale = new Vector3(baseScale.x, Mathf.Lerp(baseScale.y, baseScale.y * 0.05f, pct), baseScale.z);
            yield return null;
        }
        time = 0f;
        while (time < half)
        {
            time += Time.deltaTime;
            float pct = time / half;
            t.localScale = new Vector3(baseScale.x, Mathf.Lerp(baseScale.y * 0.05f, baseScale.y, pct), baseScale.z);
            yield return null;
        }
        t.localScale = baseScale;
    }
}
