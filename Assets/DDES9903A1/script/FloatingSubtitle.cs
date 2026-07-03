using UnityEngine;
using TMPro;

/// <summary>
/// 漂浮字幕（艾迪芬奇风格）—— 文字固定在 3D 空间，玩家靠近时渐显，走远渐隐。
/// 用于指引和叙事提示。挂在带 TextMeshPro (3D) 的物体上。
/// 做很多条：复制这个物体，改文字和位置即可。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class FloatingSubtitle : MonoBehaviour
{
    [Header("玩家")]
    [Tooltip("不填自动找 Tag=Player")]
    public Transform player;

    [Header("触发距离")]
    [Tooltip("玩家离这么近时，字幕开始显现（米）")]
    public float showDistance = 8f;

    [Tooltip("完全清晰的距离。比 showDistance 小")]
    public float fullDistance = 4f;

    [Header("渐变")]
    [Tooltip("渐显/渐隐速度")]
    public float fadeSpeed = 2f;

    [Tooltip("最大不透明度")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("朝向玩家（billboard）")]
    [Tooltip("勾选则文字始终转向摄像机；不勾则保持摆放时的固定朝向")]
    public bool faceCamera = false;

    private TMP_Text tmp;
    private float currentAlpha = 0f;
    private Camera cam;

    private void Start()
    {
        tmp = GetComponent<TMP_Text>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        cam = Camera.main;
        SetAlpha(0f);
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 距离 → 目标透明度：近了显现，远了隐去
        float targetAlpha;
        if (dist <= fullDistance) targetAlpha = maxAlpha;
        else if (dist >= showDistance) targetAlpha = 0f;
        else targetAlpha = Mathf.InverseLerp(showDistance, fullDistance, dist) * maxAlpha;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(currentAlpha);

        // 朝向玩家（可选）
        if (faceCamera && cam != null && currentAlpha > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    private void SetAlpha(float a)
    {
        if (tmp == null) return;
        Color c = tmp.color;
        c.a = a;
        tmp.color = c;
    }
}