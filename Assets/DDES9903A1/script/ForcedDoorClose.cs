using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// 强制关门事件。玩家靠近到一定距离时，门被强制关上（可配合动画/旋转/滑动），并锁死。
/// 挂在门的管理物体上。
/// </summary>
public class ForcedDoorClose : MonoBehaviour
{
    [Header("玩家")]
    public Transform player;

    [Header("触发距离")]
    [Tooltip("玩家靠近到这个距离时触发关门")]
    public float triggerDistance = 3f;

    [Header("门")]
    [Tooltip("要关上的门物体（会旋转/移动到关闭状态）")]
    public Transform door;

    [Tooltip("门关闭时的目标旋转（欧拉角）。如果门是旋转开合的")]
    public Vector3 closedRotation = Vector3.zero;

    [Tooltip("关门速度（越大越快、越猛）")]
    public float closeSpeed = 8f;

    [Header("触发时的效果")]
    [Tooltip("关门瞬间触发的事件（音效、字幕、震动等）")]
    public UnityEvent onForceClose;

    private bool triggered = false;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (triggered || player == null || door == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= triggerDistance)
        {
            triggered = true;
            StartCoroutine(SlamShut());
            onForceClose?.Invoke();   // 触发音效、字幕等
        }
    }

    private IEnumerator SlamShut()
    {
        Quaternion target = Quaternion.Euler(closedRotation);
        // 快速猛地关上
        while (Quaternion.Angle(door.localRotation, target) > 0.5f)
        {
            door.localRotation = Quaternion.Slerp(door.localRotation, target, closeSpeed * Time.deltaTime);
            yield return null;
        }
        door.localRotation = target;
        // 关上后锁死——triggered 保持 true，不再响应，门也没有可交互组件让玩家开
    }
}