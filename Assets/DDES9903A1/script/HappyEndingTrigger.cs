using UnityEngine;

// 挂在场景里任意一个物体上（比如 FarewellSequenceManager 自己）。
// 因为 RoomProgressState 是纯静态类，没法直接在 UnityEvent 里选它的方法，
// 这个脚本包一层，让你能在 Inspector 里正常挂载调用。
public class HappyEndingTrigger : MonoBehaviour
{
    // 挂在 On Farewell Complete() 上
    public void MarkHappyEndingComplete()
    {
        RoomProgressState.CompleteHappyEnding();
    }
}
