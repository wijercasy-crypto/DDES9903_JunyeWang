using UnityEngine;

// 挂在每一个唱片碎片物体上，配合EZPZ的Interactable General使用。
// 玩家点击拾取时调用 Collect()：把自己的位置报告给管理器（决定字幕该出现在哪），
// 通知管理器+1，碎片消失。
public class RecordFragment : MonoBehaviour
{
    [Tooltip("拖场景里的 RecordFragmentManager 进来")]
    public RecordFragmentManager manager;

    private bool collected = false;

    // 挂在 EZPZ Interactable General 的 OnPrimaryInteract() 上
    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (manager != null) manager.OnFragmentCollected(transform.position);

        gameObject.SetActive(false);
    }
}
