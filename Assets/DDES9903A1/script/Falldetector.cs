using UnityEngine;

// 挂在走廊下方一个很大的、贯穿走廊长度的 Trigger Collider 上。
// 玩家一旦从走廊边缘掉下去、掉进这个区域，直接触发虚空结局。
public class FallDetector : MonoBehaviour
{
    [Tooltip("拖 FearOutcomeManager 进来")]
    public FearOutcomeManager outcomeManager;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        Debug.Log("[FallDetector] 玩家掉落，触发虚空结局");

        if (outcomeManager != null)
            outcomeManager.TriggerVoidEnding();
    }
}
