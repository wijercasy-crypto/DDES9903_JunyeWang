using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

// 挂在终点站场景的一个空物体上(比如 "FarewellSequenceManager")。
//
// 用法：
// 1. 沿站台从近到远摆好几组 Claire+Lily 模型，每组自己摆一个不同的姿势，
//    放在它该出现的位置，按顺序拖进 posedGroups 数组。
// 2. 除了第一组，其余组会在 Start() 里自动隐藏。
//
// 流程(这一版的关键改动)：
// - 玩家进入 approachDistance 范围(此时离残影其实还有一段距离) → 风滚草开始滚动
// - 风滚草滚到"正好挡住视线"的那一刻(由 occlusionTiming 控制，默认滚到一半) → 
//   当前这组直接隐藏、下一组直接显示，因为被风滚草挡着，玩家看不到切换的瞬间
// - 风滚草滚完剩余路程，视线恢复，下一组已经稳稳地站在那里了
// - 全程不需要淡入淡出，也不需要模型材质支持alpha
public class FinalStopFarewellSequence : MonoBehaviour
{
    [Header("场景引用")]
    [Tooltip("玩家Transform，留空则每帧自动按Tag查找")]
    public Transform player;
    [Tooltip("玩家摄像机，留空则自动用Camera.main。风滚草会固定出现在这个摄像机当前朝向的正前方，不管玩家转向哪边")]
    public Camera playerCamera;
    [Tooltip("依次出现的、已经摆好姿势和位置的 Claire+Lily 模型组，按从近到远排列")]
    public GameObject[] posedGroups;
    public TumbleweedRoller tumbleweed;

    [Header("触发距离")]
    [Tooltip("玩家离当前这组还有多远时，风滚草就开始滚过来（可以设大一点，让切换发生在玩家还没走到跟前的时候）")]
    public float approachDistance = 12f;
    public float touchDistance = 2f;

    [Header("风滚草遮挡时机")]
    [Tooltip("0~1，风滚草滚动过程中，滚到百分之多少的时候完成切换。0.5就是滚到正中间(视线被挡得最死)的那一刻切换")]
    [Range(0f, 1f)]
    public float occlusionTiming = 0.5f;
    [Tooltip("风滚草出现的位置，在玩家与当前残影连线上的比例，0.4表示偏玩家这侧、比较靠近玩家脚下")]
    [Range(0.1f, 0.8f)]
    public float tumbleweedCrossPointRatio = 0.4f;

    [Tooltip("风滚草出现在摄像机前方多远处")]
    public float tumbleweedAheadDistance = 6f;

    [Header("最终告别")]
    public float farewellDuration = 6f;
    public GameObject door;

    [Header("启动缓冲")]
    [Tooltip("场景开始后，等这么多秒再开始检测触发，避免游戏刚开始头几帧场景还没初始化完就误触发")]
    public float startDelay = 1.5f;

    [Header("事件钩子（可在Inspector里挂字幕/音效/动画）")]
    public UnityEvent onFigureRevealed;
    [Tooltip("画面切换到倒数第二组时触发一次，比如挂火车启动脚本")]
    public UnityEvent onSecondToLastReached;
    public UnityEvent onFinalTouch;
    public UnityEvent onFarewellComplete;

    [Header("调试")]
    public bool showDebugLogs = true;

    private int currentIndex = 0;
    private bool busy = false;
    private bool sequenceActive = true;

    void Start()
    {
        if (posedGroups.Length == 0)
        {
            Debug.LogWarning("FinalStopFarewellSequence: 没有设置 posedGroups");
            enabled = false;
            return;
        }

        for (int i = 0; i < posedGroups.Length; i++)
        {
            if (posedGroups[i] != null)
                posedGroups[i].SetActive(i == 0);
        }

        if (door != null) door.SetActive(false);

        sequenceActive = false;
        StartCoroutine(EnableAfterDelay());
    }

    IEnumerator EnableAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        sequenceActive = true;
        if (showDebugLogs) Debug.Log("[FarewellSequence] 启动缓冲结束，开始检测触发");
    }

    void Update()
    {
        if (!sequenceActive || busy) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (player == null)
            {
                if (showDebugLogs) Debug.LogWarning("FinalStopFarewellSequence: 找不到Player，检查player字段是否手动拖入，或者玩家物体Tag是否设为Player");
                return;
            }
        }

        GameObject current = posedGroups[currentIndex];
        float dist = Vector3.Distance(player.position, current.transform.position);
        bool isFinal = currentIndex == posedGroups.Length - 1;

        if (showDebugLogs) Debug.Log($"[FarewellSequence] 当前第{currentIndex}组，距离玩家 {dist:F1}m，触发距离 {approachDistance}m");

        if (isFinal)
        {
            if (dist <= touchDistance)
            {
                sequenceActive = false;
                StartCoroutine(FinalTouchSequence());
            }
        }
        else if (dist <= approachDistance)
        {
            StartCoroutine(SwapDuringOcclusion());
        }
    }

    IEnumerator SwapDuringOcclusion()
    {
        busy = true;

        GameObject current = posedGroups[currentIndex];
        GameObject next = posedGroups[currentIndex + 1];

        if (playerCamera == null) playerCamera = Camera.main;

        Vector3 crossPoint;
        Vector3 viewDir;
        if (playerCamera != null)
        {
            // 固定出现在"摄像机当前朝向"的正前方，不管玩家脸朝哪边都能看到
            viewDir = playerCamera.transform.forward;
            crossPoint = playerCamera.transform.position + viewDir * tumbleweedAheadDistance;
        }
        else
        {
            // 兜底：找不到摄像机时，退回原来的玩家->残影连线算法
            crossPoint = Vector3.Lerp(player.position, current.transform.position, tumbleweedCrossPointRatio);
            viewDir = current.transform.position - player.position;
        }

        float rollDuration = tumbleweed != null ? tumbleweed.rollDuration : 1.4f;

        if (showDebugLogs)
        {
            if (tumbleweed == null) Debug.LogWarning("[FarewellSequence] tumbleweed字段是空的，风滚草不会出现，但切换本身仍会执行");
            else Debug.Log("[FarewellSequence] 触发切换，风滚草开始滚动");
        }

        // 风滚草开始滚动（不在这里等它滚完，让它自己跑）
        if (tumbleweed != null)
            StartCoroutine(tumbleweed.RollAcross(crossPoint, viewDir, tumbleweed.transform.position.y));

        // 等到风滚草滚到"挡住视线最厉害"的那一刻
        yield return new WaitForSeconds(rollDuration * occlusionTiming);

        // 被挡住的瞬间直接切换，玩家看不到这一下
        current.SetActive(false);
        currentIndex++;
        next.SetActive(true);

        // 如果刚好切换到了倒数第二组，触发一次专用事件（比如让火车开始行进）
        if (currentIndex == posedGroups.Length - 2)
        {
            if (showDebugLogs) Debug.Log("[FarewellSequence] 到达倒数第二组，触发 onSecondToLastReached");
            onSecondToLastReached.Invoke();
        }

        // 等风滚草滚完剩余的路程，视线恢复
        yield return new WaitForSeconds(rollDuration * (1f - occlusionTiming));

        onFigureRevealed.Invoke();
        busy = false;
    }

    IEnumerator FinalTouchSequence()
    {
        onFinalTouch.Invoke();
        yield return new WaitForSeconds(farewellDuration);
        ShowDoor();
    }

    public void ShowDoor()
    {
        if (door != null) door.SetActive(true);
        onFarewellComplete.Invoke();
    }
}
