using UnityEngine;
using UnityEngine.Events;
using System.Collections;

// 挂在场景里一个空物体上（比如叫 "ClaireReunionSequence"）。
// 挂在 RecordFragmentManager 的 onAllFragmentsCollected 事件上触发。
//
// 流程：
// 停顿 -> Claire完整身影出现，说台词(带"要不要跟我走"的台词) 
// -> 转身走向光圈，停在那等 -> 同时监测两件事：
//    a) 玩家走进光圈 -> 触发结局(On Reached Light)
//    b) 玩家走远超过 walkAwayDistance -> Claire和光圈直接消失，不触发任何结局，主线不受影响
public class ClaireReunionSequence : MonoBehaviour
{
    [Header("玩家")]
    [Tooltip("留空自动按Tag查找")]
    public Transform player;

    [Header("Claire 完整身影")]
    [Tooltip("Claire完整版模型，初始设为不激活")]
    public GameObject claireFullFigure;
    [Tooltip("集齐碎片后，等多久Claire才出现")]
    public float appearDelay = 2f;

    [Header("台词")]
    [Tooltip("Claire出现时说的台词（\"你还有更重要的事要做...你真的要跟我走吗？\"）")]
    public TriggeredSubtitle appearSubtitle;

    [Header("转身走向光圈")]
    [Tooltip("Claire出现后，等多久开始走")]
    public float walkStartDelay = 3f;
    [Tooltip("Claire走向的目标点（光圈的位置）")]
    public Transform lightDestination;
    [Tooltip("光圈的视觉效果物体，Claire走远后消失的时候会一起隐藏")]
    public GameObject lightCircleVisual;
    public float walkSpeed = 1.2f;

    [Header("玩家走远判定")]
    [Tooltip("玩家离光圈超过这个距离，Claire和光圈直接消失，不触发结局")]
    public float walkAwayDistance = 15f;

    [Header("结局触发")]
    [Tooltip("玩家走进光圈时触发（配合 ReunionEndingTrigger.cs 使用）")]
    public UnityEvent onReachedLight;

    public bool showDebugLogs = true;

    private bool sequenceStarted = false;
    private bool waitingForPlayerChoice = false;
    private bool resolved = false;

    // 挂在 RecordFragmentManager 的 onAllFragmentsCollected() 上
    public void StartSequence()
    {
        if (sequenceStarted) return;
        sequenceStarted = true;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        StartCoroutine(SequenceRoutine());
    }

    IEnumerator SequenceRoutine()
    {
        if (showDebugLogs) Debug.Log("[ClaireReunion] 序列开始");

        yield return new WaitForSeconds(appearDelay);

        if (claireFullFigure != null)
        {
            claireFullFigure.SetActive(true);
            if (showDebugLogs) Debug.Log("[ClaireReunion] Claire出现");
        }

        if (appearSubtitle != null)
            appearSubtitle.Show();

        yield return new WaitForSeconds(walkStartDelay);

        // 转身走向光圈
        if (claireFullFigure != null && lightDestination != null)
        {
            if (showDebugLogs) Debug.Log("[ClaireReunion] Claire开始走向光圈");

            Vector3 dir = (lightDestination.position - claireFullFigure.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                claireFullFigure.transform.rotation = Quaternion.LookRotation(dir.normalized);

            while (Vector3.Distance(claireFullFigure.transform.position, lightDestination.position) > 0.3f)
            {
                claireFullFigure.transform.position = Vector3.MoveTowards(
                    claireFullFigure.transform.position,
                    lightDestination.position,
                    walkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        if (lightCircleVisual != null) lightCircleVisual.SetActive(true);

        if (showDebugLogs) Debug.Log("[ClaireReunion] Claire在光圈等待玩家选择");
        waitingForPlayerChoice = true;
    }

    void Update()
    {
        if (!waitingForPlayerChoice || resolved) return;
        if (player == null || lightDestination == null) return;

        float dist = Vector3.Distance(player.position, lightDestination.position);
        if (dist >= walkAwayDistance)
        {
            resolved = true;
            waitingForPlayerChoice = false;

            if (showDebugLogs) Debug.Log("[ClaireReunion] 玩家走远了，Claire和光圈消失");

            if (claireFullFigure != null) claireFullFigure.SetActive(false);
            if (lightCircleVisual != null) lightCircleVisual.SetActive(false);
        }
    }

    // 挂在光圈位置的Trigger Collider的OnTriggerEnter里调用（见 ReunionEndingTrigger.cs）
    public void PlayerReachedLight()
    {
        if (!waitingForPlayerChoice || resolved) return;

        resolved = true;
        waitingForPlayerChoice = false;

        if (showDebugLogs) Debug.Log("[ClaireReunion] 玩家走进光圈，触发结局");
        onReachedLight.Invoke();
    }
}
