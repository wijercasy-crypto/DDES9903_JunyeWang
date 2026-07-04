using UnityEngine;
using System.Collections;

/// <summary>
/// 被事件调用才显现的物体（如过山车登顶时出现的小女孩）。
/// 平时隐藏，调用 Reveal() 渐显/出现，调用 Hide() 隐去。
/// 挂在一个不会被隐藏的管理物体上，用 targetObject 指定要显隐的小女孩。
/// </summary>
public class RevealOnCall : MonoBehaviour
{
    [Header("要显隐的对象")]
    [Tooltip("小女孩物体（会被 SetActive 显隐）")]
    public GameObject targetObject;

    [Header("显现方式")]
    [Tooltip("勾选=渐显（需要物体材质支持透明）；不勾=直接出现")]
    public bool useFade = false;

    [Tooltip("渐显时长（仅 useFade 时有效）")]
    public float fadeDuration = 2f;

    [Header("自动隐去（可选）")]
    [Tooltip("显现后过几秒自动隐去。0 = 不自动隐，需手动 Hide")]
    public float autoHideAfter = 0f;

    private void Start()
    {
        // 初始隐藏
        if (targetObject != null) targetObject.SetActive(false);
    }

    /// <summary>显现小女孩（过山车登顶时调用）</summary>
    public void Reveal()
    {
        if (targetObject == null) return;
        targetObject.SetActive(true);

        if (autoHideAfter > 0f)
        {
            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), autoHideAfter);
        }
    }

    /// <summary>隐去小女孩</summary>
    public void Hide()
    {
        if (targetObject == null) return;
        targetObject.SetActive(false);
    }
}