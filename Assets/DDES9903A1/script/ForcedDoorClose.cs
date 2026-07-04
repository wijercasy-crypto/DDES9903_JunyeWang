using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// 强制关门。玩家走进触发区时,门被强制关上并锁死。
/// 把这个脚本挂在触发区物体上(带 Is Trigger 的 BoxCollider)。
/// </summary>
public class ForcedDoorClose : MonoBehaviour
{
    [Header("门")]
    [Tooltip("要关上的门物体")]
    public Transform door;

    [Tooltip("门关闭时的旋转(欧拉角)")]
    public Vector3 closedRotation = Vector3.zero;

    [Tooltip("关门速度")]
    public float closeSpeed = 8f;

    [Header("触发时的效果")]
    public UnityEvent onForceClose;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // 只对玩家触发(用 Tag 判断)
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(SlamShut());
        onForceClose?.Invoke();
    }

    private IEnumerator SlamShut()
    {
        if (door == null) yield break;
        Quaternion target = Quaternion.Euler(closedRotation);
        while (Quaternion.Angle(door.localRotation, target) > 0.5f)
        {
            door.localRotation = Quaternion.Slerp(door.localRotation, target, closeSpeed * Time.deltaTime);
            yield return null;
        }
        door.localRotation = target;
    }
}