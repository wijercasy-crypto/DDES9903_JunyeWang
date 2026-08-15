using UnityEngine;

// 挂在每个"分岔选择"按钮上，配合EZPZ的Interactable General使用。
// 点击后打开/显现对应的实体路径（门、通道、光效等），
// 玩家还需要自己走过去才真正进入那条线——这个脚本只负责"打开"，不负责"进入"。
public class PathRevealButton : MonoBehaviour
{
    [Tooltip("按下这个按钮要打开/显现的物体们（门、通道、光效等，可以拖多个）")]
    public GameObject[] objectsToReveal;

    [Tooltip("打开后，这个按钮本身要不要跟着隐藏/禁用（比如避免重复点击）")]
    public bool hideButtonAfterUse = true;

    private bool activated = false;

    // 挂在 EZPZ Interactable General 的 OnPrimaryInteract() 上
    public void RevealPath()
    {
        if (activated) return;
        activated = true;

        foreach (var obj in objectsToReveal)
        {
            if (obj != null) obj.SetActive(true);
        }

        if (hideButtonAfterUse) gameObject.SetActive(false);
    }
}
