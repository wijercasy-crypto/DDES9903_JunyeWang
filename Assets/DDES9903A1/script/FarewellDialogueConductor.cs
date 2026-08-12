using UnityEngine;
using UnityEngine.Events;
using System.Collections;

// 挂在场景里一个空物体上(比如叫 "FarewellDialogueConductor")。
// 负责让 Claire 和 Lily 两个各自独立的 TriggeredSubtitle 文本物体，
// 按对话真实的先后顺序轮流播放，不会互相重叠。
//
// 用法：
// 1. 在 sequence 数组里，按对话真实顺序，每一步指定"哪个字幕物体"+"它的第几句"
//    比如：Lily(第0句) -> Claire(第0句) -> Claire(第1句) -> Lily(第1句) -> Claire(第2句)
// 2. 把这个物体的 PlayAll() 挂到 FinalStopFarewellSequence 的 On Final Touch() 上
//    (取代原来直接挂某个 TriggeredSubtitle.Show() 的做法)
// 3. 把 onAllComplete 挂到 FinalStopFarewellSequence 的 MarkVoiceLineComplete() 上
public class FarewellDialogueConductor : MonoBehaviour
{
    [System.Serializable]
    public class DialogueStep
    {
        [Tooltip("这一步是谁在说话：拖Claire的字幕物体或者Lily的字幕物体")]
        public TriggeredSubtitle speaker;
        [Tooltip("说speaker身上Lines列表里的第几句，从0开始数")]
        public int lineIndex;
    }

    [Header("按对话真实顺序排列")]
    public DialogueStep[] sequence;

    [Header("每一步之间的额外停顿")]
    public float gapBetweenSteps = 0.4f;

    [Header("调试")]
    public bool showDebugLogs = true;

    [Header("全部播完后触发")]
    public UnityEvent onAllComplete;

    private bool playing = false;

    // 挂在 FinalStopFarewellSequence 的 On Final Touch() 上
    public void PlayAll()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        for (int i = 0; i < sequence.Length; i++)
        {
            var step = sequence[i];
            if (step.speaker == null)
            {
                Debug.LogWarning($"[FarewellDialogueConductor] 第{i}步没有指定speaker，跳过");
                continue;
            }

            if (showDebugLogs) Debug.Log($"[FarewellDialogueConductor] 第{i}步：{step.speaker.name} 的第{step.lineIndex}句");

            yield return StartCoroutine(step.speaker.PlayLineRoutine(step.lineIndex));
            yield return new WaitForSeconds(gapBetweenSteps);
        }

        if (showDebugLogs) Debug.Log("[FarewellDialogueConductor] 全部播完");
        onAllComplete.Invoke();
        playing = false;
    }
}
