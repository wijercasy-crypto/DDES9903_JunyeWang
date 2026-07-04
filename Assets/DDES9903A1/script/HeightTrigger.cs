using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 高度触发事件。玩家（或指定物体）高度超过阈值时触发一次事件，
/// 用于摩天轮升到高处时显示字幕、触发小女孩等。
/// </summary>
public class HeightTrigger : MonoBehaviour
{
    [Header("检测对象")]
    [Tooltip("要检测高度的物体，通常是玩家。不填自动找 Tag=Player")]
    public Transform target;

    [Header("触发高度")]
    [Tooltip("target 的 Y 超过这个值时触发")]
    public float triggerHeight = 25f;

    [Header("触发事件")]
    [Tooltip("到达高度时触发（连字幕的 Show）")]
    public UnityEvent onReachHeight;

    [Tooltip("降回高度以下时触发（可选，连字幕隐藏）")]
    public UnityEvent onLeaveHeight;

    [Header("重复触发")]
    [Tooltip("勾选=每次升上去都触发；不勾=只触发一次")]
    public bool repeatable = true;

    private bool triggered = false;

    private void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    private void Update()
    {
        if (target == null) return;

        bool above = target.position.y >= triggerHeight;

        if (above && !triggered)
        {
            triggered = true;
            onReachHeight?.Invoke();   // 升到高处，触发字幕显示
        }
        else if (!above && triggered)
        {
            triggered = false;
            onLeaveHeight?.Invoke();    // 降下来
            if (!repeatable) enabled = false;   // 不重复的话，触发过就停用
        }
    }
}