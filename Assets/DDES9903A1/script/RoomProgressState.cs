using UnityEngine;

// 静态类，不用挂在任何物体上，直接在代码里调用即可。
// 状态只在本局游戏运行期间有效（跨场景保留，但关闭游戏重开会重置，不是存档文件）。
public static class RoomProgressState
{
    public static bool hasUnlockedBall = false;
    public static bool hasUnlockedGramophone = false;
    public static bool hasCompletedHappyEnding = false;

    public static void UnlockBall()
    {
        if (!hasUnlockedBall)
        {
            hasUnlockedBall = true;
            Debug.Log("[RoomProgressState] 皮球已解锁");
        }
    }

    public static void UnlockGramophone()
    {
        if (!hasUnlockedGramophone)
        {
            hasUnlockedGramophone = true;
            Debug.Log("[RoomProgressState] 留声机已解锁");
        }
    }

    public static void CompleteHappyEnding()
    {
        hasCompletedHappyEnding = true;
        Debug.Log("[RoomProgressState] Happy Ending已达成，全家福涂鸦将消失");
    }
}
