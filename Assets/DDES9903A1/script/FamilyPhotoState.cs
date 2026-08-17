using UnityEngine;

// 挂在全家福物体的父级（或者单独一个管理物体）上。
// 场景加载时根据 RoomProgressState.hasCompletedHappyEnding 决定
// 显示涂鸦版还是恢复干净版的全家福。
public class FamilyPhotoState : MonoBehaviour
{
    [Tooltip("被涂黑的全家福物体")]
    public GameObject scribbledVersion;
    [Tooltip("恢复正常的全家福物体（初始设为不激活）")]
    public GameObject cleanVersion;

    void Start()
    {
        bool happy = RoomProgressState.hasCompletedHappyEnding;

        if (scribbledVersion != null) scribbledVersion.SetActive(!happy);
        if (cleanVersion != null) cleanVersion.SetActive(happy);
    }
}
