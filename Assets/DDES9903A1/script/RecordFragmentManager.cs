using UnityEngine;
using UnityEngine.Events;

// 挂在场景里一个空物体上（比如叫 "RecordFragmentManager"）。
// 追踪玩家收集了几个唱片碎片，集齐后触发后续的 Meet Claire 演出。
//
// 关键点：第一句/第二句字幕物体是"共享"的，不属于任何一块具体碎片。
// 每次触发时，会自动把对应的字幕物体挪到"刚被捡起的那块碎片"的位置再显示，
// 所以不管玩家先捡哪一块，字幕永远出现在玩家当前所在的位置。
public class RecordFragmentManager : MonoBehaviour
{
    [Tooltip("总共需要收集几个碎片")]
    public int totalFragments = 3;

    [Header("共享字幕（不属于任何具体碎片）")]
    [Tooltip("收集到第1个时显示这句（不管是哪一块碎片）")]
    public TriggeredSubtitle firstLineSubtitle;
    [Tooltip("收集到第2个时显示这句（不管是哪一块碎片）")]
    public TriggeredSubtitle secondLineSubtitle;
    [Tooltip("收集到第3个(集齐)的那一瞬间显示这句Reed自己的反应，然后Claire才出现")]
    public TriggeredSubtitle thirdLineSubtitle;
    [Tooltip("字幕相对碎片位置的偏移，比如往上抬一点，别跟碎片本身位置重叠")]
    public Vector3 subtitleOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("集齐所有碎片后触发一次")]
    public UnityEvent onAllFragmentsCollected;

    [Tooltip("每收集到一个就触发一次（可选，比如挂拾取音效）")]
    public UnityEvent onFragmentCollected;

    public bool showDebugLogs = true;

    private int collectedCount = 0;

    // 由 RecordFragment.Collect() 调用，传入这块碎片的位置
    public void OnFragmentCollected(Vector3 fragmentPosition)
    {
        collectedCount++;
        if (showDebugLogs)
            Debug.Log($"[RecordFragmentManager] 已收集 {collectedCount}/{totalFragments}，位置{fragmentPosition}");

        onFragmentCollected.Invoke();

        if (collectedCount == 1 && firstLineSubtitle != null)
        {
            firstLineSubtitle.transform.position = fragmentPosition + subtitleOffset;
            firstLineSubtitle.Show();
        }
        else if (collectedCount == 2 && secondLineSubtitle != null)
        {
            secondLineSubtitle.transform.position = fragmentPosition + subtitleOffset;
            secondLineSubtitle.Show();
        }
        else if (collectedCount >= totalFragments)
        {
            if (thirdLineSubtitle != null)
            {
                thirdLineSubtitle.transform.position = fragmentPosition + subtitleOffset;
                thirdLineSubtitle.Show();
            }

            if (showDebugLogs) Debug.Log("[RecordFragmentManager] 全部收集完成！触发 onAllFragmentsCollected");
            onAllFragmentsCollected.Invoke();
        }
    }
}
