using UnityEngine;

/// <summary>
/// 一次性灯光。可被外部调用熄灭，熄灭后不再亮起（只亮这一次）。
/// 挂在 Point Light 物体上，由 CarouselRider 在玩家坐上时调用 TurnOffForever()。
/// </summary>
public class OneTimeLight : MonoBehaviour
{
    [Header("要控制的灯")]
    [Tooltip("不填则用本物体上的 Light")]
    public Light targetLight;

    [Header("渐灭")]
    [Tooltip("熄灭的渐暗时长（秒）。0 = 瞬间熄灭")]
    public float fadeOutDuration = 1.5f;

    private bool used = false;          // 是否已经用过（熄灭过）
    private float initialIntensity;
    private bool fading = false;
    private float fadeTimer = 0f;

    private void Start()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
        if (targetLight != null) initialIntensity = targetLight.intensity;
    }

    private void Update()
    {
        if (!fading || targetLight == null) return;

        fadeTimer += Time.deltaTime;
        float t = fadeOutDuration > 0.01f ? fadeTimer / fadeOutDuration : 1f;
        targetLight.intensity = Mathf.Lerp(initialIntensity, 0f, t);

        if (t >= 1f)
        {
            targetLight.intensity = 0f;
            targetLight.enabled = false;   // 彻底关掉
            fading = false;
        }
    }

    /// <summary>熄灭这盏灯，永久不再亮（只亮一次）。外部调用。</summary>
    public void TurnOffForever()
    {
        if (used) return;   // 已经熄灭过，不再响应
        used = true;
        fading = true;
        fadeTimer = 0f;
    }

    /// <summary>查询是否已经用掉了</summary>
    public bool IsUsed() { return used; }
}