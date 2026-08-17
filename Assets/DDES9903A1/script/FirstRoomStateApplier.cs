using UnityEngine;

// 挂在"最初的房间"场景里一个空物体上。
// 场景加载时读取 RoomProgressState，决定皮球/留声机要不要显示出来。
public class FirstRoomStateApplier : MonoBehaviour
{
    public GameObject ballObject;
    public GameObject gramophoneObject;

    void Start()
    {
        if (ballObject != null)
            ballObject.SetActive(RoomProgressState.hasUnlockedBall);

        if (gramophoneObject != null)
            gramophoneObject.SetActive(RoomProgressState.hasUnlockedGramophone);
    }
}
