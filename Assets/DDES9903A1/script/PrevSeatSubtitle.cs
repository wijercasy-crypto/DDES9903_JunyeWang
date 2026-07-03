using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 前一个座位字幕。玩家坐上某座位时，把字幕移到"前一个座位"的位置显示。
/// 挂在一个管理物体上。字幕物体用 TriggeredSubtitle 控制显隐。
/// </summary>
public class PrevSeatSubtitle : MonoBehaviour
{
    [Header("座位列表（按顺序，前后相邻）")]
    [Tooltip("按火箭座位的顺序拖入，seat0, seat1, seat2, seat3...")]
    public List<Transform> seats = new List<Transform>();

    [Header("字幕")]
    [Tooltip("要显示的字幕物体（挂 TriggeredSubtitle）")]
    public TriggeredSubtitle subtitle;

    [Tooltip("字幕相对座位的偏移（比如抬高一点）")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    /// <summary>玩家坐上某座位时调用，参数是玩家坐的座位</summary>
    public void OnSeatTaken(Transform takenSeat)
    {
        int index = seats.IndexOf(takenSeat);
        if (index < 0) return;   // 这个座位不在列表里

        // 前一个座位（index-1）。如果坐的是第一个，前一个就绕到最后一个
        int prevIndex = (index + 1) % seats.Count;
        Transform prevSeat = seats[prevIndex];
        if (prevSeat == null || subtitle == null) return;

        // 把字幕移到前一个座位的位置
        subtitle.transform.position = prevSeat.position + offset;
        // 让字幕作为前一个座位的子物体，跟着转
        subtitle.transform.SetParent(prevSeat, true);

        // 显示字幕
        subtitle.Show();
    }
}