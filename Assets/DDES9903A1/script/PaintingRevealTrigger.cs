using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在走廊地板上一个个的触发区域（BoxCollider, Is Trigger 勾选）。
/// 玩家走到这个区域时，对应的画面（一张或多张）淡入显示。
/// 画面渐显后会一直留在墙上（像美术馆一路挂过去，逐渐拼出完整故事），不会消失。
/// </summary>
public class PaintingRevealTrigger : MonoBehaviour
{
    [Tooltip("这个触发区要点亮的画面（挂了 Renderer 的 Quad/Plane），可以放多张同时渐显")]
    public Renderer[] paintingsToReveal;

    [Tooltip("渐显耗时（秒）")]
    public float fadeDuration = 1.5f;

    [Tooltip("触发一次后是否禁用（防止玩家来回走反复触发）")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        // 场景一开始就把这些画设为完全透明，不用手动去材质里调 Alpha
        foreach (var r in paintingsToReveal)
        {
            if (r == null) continue;
            Material mat = r.material;
            Color c = mat.color;
            c.a = 0f;
            mat.color = c;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 建议给玩家的 Character Controller / XR Origin 打上 "Player" Tag
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        foreach (var r in paintingsToReveal)
        {
            if (r != null)
                StartCoroutine(FadeInRenderer(r, fadeDuration));
        }
    }

    private IEnumerator FadeInRenderer(Renderer r, float duration)
    {
        // 要求画面材质用支持透明的 Shader（比如 Unlit/Transparent 或 URP 的 Lit 开 Transparent 模式）
        Material mat = r.material; // 用 .material 会自动实例化，不影响其他共用同材质的物体
        float startAlpha = mat.color.a; // Start() 里已经设成 0 了，这里直接读当前值继续往上渐变

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            Color c = mat.color;
            c.a = Mathf.Lerp(startAlpha, 1f, t / duration);
            mat.color = c;
            yield return null;
        }
        Color finalC = mat.color;
        finalC.a = 1f;
        mat.color = finalC;
    }
}
