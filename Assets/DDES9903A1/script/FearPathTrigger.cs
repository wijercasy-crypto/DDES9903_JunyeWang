using UnityEngine;
using UnityEngine.Events;

// 挂在走廊分岔口"其中一条路"的入口上，加一个Trigger Collider。
// 玩家走进这条路，会根据 isCorrectPath 触发对应事件。
// 一个分岔口通常放两个（或更多）这样的物体，一个correct一个wrong。
public class FearPathTrigger : MonoBehaviour
{
    [Tooltip("这条路是不是正确的路")]
    public bool isCorrectPath = true;

    public UnityEvent onCorrectPathTaken;
    public UnityEvent onWrongPathTaken;

    public bool showDebugLogs = true;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (showDebugLogs)
            Debug.Log($"[FearPathTrigger] 玩家走进了{(isCorrectPath ? "正确" : "错误")}的路：{gameObject.name}");

        if (isCorrectPath) onCorrectPathTaken.Invoke();
        else onWrongPathTaken.Invoke();
    }
}
