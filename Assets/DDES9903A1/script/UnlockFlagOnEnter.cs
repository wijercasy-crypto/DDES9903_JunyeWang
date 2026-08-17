using UnityEngine;

// 挂在场景里一个空物体上（比如游乐园场景挂一个、沼泽场景挂一个）。
// 场景一加载就自动解锁对应的标记，不需要判断具体走了哪个SE。
public class UnlockFlagOnEnter : MonoBehaviour
{
    public enum FlagType { Ball, Gramophone }

    [Tooltip("这个场景要解锁的标记类型：游乐园场景选Ball，沼泽场景选Gramophone")]
    public FlagType flagToUnlock;

    void Start()
    {
        if (flagToUnlock == FlagType.Ball)
            RoomProgressState.UnlockBall();
        else
            RoomProgressState.UnlockGramophone();
    }
}
