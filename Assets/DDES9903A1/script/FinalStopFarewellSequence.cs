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
    [Tooltip("玩家离最后一组还有多远时，自动触发告别（字幕+语音）。想要\"快靠近时就触发\"就把这个调大一点，不用等真正走到跟前")]
    public float touchDistance = 6f;

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
    [Tooltip("已不再用于自动等待，保留字段仅作兼容，可忽略")]
    public float farewellDuration = 6f;
    public GameObject door;

    [Header("摸手之后")]
    [Tooltip("摸手后，等这么多秒人物才开始消失（消失过程本身的时长由下面的Particle Effect Duration控制）")]
    public float characterFadeDelay = 5f;
    [Tooltip("人物消失完成后，再等这么多秒门才出现")]
    public float doorAppearDelay = 0f;
    [Tooltip("门出现后，再等这么多秒才触发\"进门提示词+旁白\"")]
    public float entryPromptDelay = 2f;

    [Header("启动缓冲")]
    [Tooltip("场景开始后，等这么多秒再开始检测触发，避免游戏刚开始头几帧场景还没初始化完就误触发")]
    public float startDelay = 1.5f;

    [Header("事件钩子（可在Inspector里挂字幕/音效/动画）")]
    public UnityEvent onFigureRevealed;
    [Tooltip("画面切换到倒数第二组时触发一次")]
    public UnityEvent onSecondToLastReached;
    [Tooltip("第二组消失(第三组出现)那一瞬间触发一次，用来切BGM")]
    public UnityEvent onGroup2Disappeared;
    [Tooltip("最后一组(第四组)真正消失的那一刻触发一次(摸手后的消失流程里)，用来切轻巧版BGM")]
    public UnityEvent onGroup4Appeared;
    [Tooltip("玩家靠近最后一组时触发一次，挂字幕/语音的Show()")]
    public UnityEvent onFinalTouch;
    [Tooltip("语音全部播完后，摸妻子的手触发。挂在这里：火车启动脚本")]
    public UnityEvent onHandTouched;
    [Tooltip("摸手后、人物淡出完成、再等doorAppearDelay秒后，门出现的瞬间触发")]
    public UnityEvent onFarewellComplete;
    [Tooltip("门出现后再等entryPromptDelay秒，触发进门提示词+旁白")]
    public UnityEvent onEntryPromptReady;

    [Header("调试")]
    public bool showDebugLogs = true;

    private int currentIndex = 0;
    private bool busy = false;
    private bool sequenceActive = true;
    private bool voiceLineComplete = false;
    private bool handTouched = false;

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
            // 快靠近时就自动触发，不需要手部点击（手部EZPZ交互仍然保留，作为备用触发方式也可以）
            if (dist <= touchDistance)
            {
                TriggerFinalTouch();
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

        // 第二组(index1)消失、第三组(index2)出现的瞬间 —— 切BGM用
        if (currentIndex == 2)
        {
            if (showDebugLogs) Debug.Log("[FarewellSequence] 第二组消失，触发 onGroup2Disappeared");
            onGroup2Disappeared.Invoke();
        }

        // 注：第四组(最后一组)不会经过这里的swap流程，它是靠摸手后的消失流程离场的，
        // onGroup4Appeared 改在 FadeOutAndShowDoor() 里真正消失的那一刻触发，见下方。

        // 等风滚草滚完剩余的路程，视线恢复
        yield return new WaitForSeconds(rollDuration * (1f - occlusionTiming));

        onFigureRevealed.Invoke();
        busy = false;
    }

    // 玩家靠近最后一组时自动调用（Update里触发），只负责播字幕/语音
    public void TriggerFinalTouch()
    {
        if (!sequenceActive || busy) return;
        if (currentIndex != posedGroups.Length - 1)
        {
            if (showDebugLogs) Debug.LogWarning("[FarewellSequence] 还没走到最后一组，暂时不能触发");
            return;
        }

        sequenceActive = false; // 防止重复触发
        onFinalTouch.Invoke();
    }

    // 把这个方法挂在 TriggeredSubtitle 的 onSequenceComplete 事件上，
    // 告诉这个脚本"语音已经讲完了，摸手才会生效"
    public void MarkVoiceLineComplete()
    {
        voiceLineComplete = true;
        if (showDebugLogs) Debug.Log("[FarewellSequence] 语音已播完，现在摸手才会生效");
    }

    // 挂在妻子手上的 EZPZ Interactable General 的 OnPrimaryInteract() 事件里调用这个方法
    public void OnHandTouch()
    {
        if (handTouched) return;
        if (!voiceLineComplete)
        {
            if (showDebugLogs) Debug.LogWarning("[FarewellSequence] 语音还没讲完，摸手暂时不生效");
            return;
        }

        handTouched = true;
        if (showDebugLogs) Debug.Log("[FarewellSequence] 摸到手了，触发列车启动+人物消失倒计时");

        onHandTouched.Invoke(); // 挂 TrainDeparture.StartDeparture()
        StartCoroutine(FadeOutAndShowDoor());
    }

    [Header("消失方式（不依赖材质透明度）")]
    [Tooltip("人物消失时同步播放的粒子特效物体（挂了TimedParticleEffect的那个）。会在这里被直接触发，不用再单独挂On Hand Touched()")]
    public TimedParticleEffect vanishParticleEffect;
    [Tooltip("粒子开始播放后，等多少秒人物消失")]
    public float characterVanishDelayAfterParticleStart = 2.5f;
    [Tooltip("消失前是否先往上飘一小段，营造离开的感觉（不缩放，纯位移，不会显得假）")]
    public bool floatUpWhileDisappearing = true;
    [Tooltip("往上飘多高")]
    public float floatUpDistance = 1f;
    [Tooltip("消失过程中光斑（如果有）额外提亮多少倍")]
    public Light glowLight;
    public float glowIntensityBoost = 2f;

    [Header("可选：额外闪光遮盖（不是必须，粒子特效本身也能盖住消失瞬间）")]
    [Tooltip("一块贴在摄像机前方的白色Quad/Plane的Renderer。不填就不会有额外闪光，只靠粒子特效盖住消失")]
    public Renderer flashQuadRenderer;
    public float flashInDuration = 0.15f;
    public float flashOutDuration = 0.35f;

    IEnumerator FadeOutAndShowDoor()
    {
        yield return new WaitForSeconds(characterFadeDelay);

        GameObject finalGroup = posedGroups[posedGroups.Length - 1];

        if (showDebugLogs)
            Debug.Log($"[FarewellSequence] 即将淡出的物体是「{finalGroup.name}」，activeSelf={finalGroup.activeSelf}");

        Vector3 startPos = finalGroup.transform.position;
        float startGlow = glowLight != null ? glowLight.intensity : 0f;

        // 人物开始消失 和 粒子特效 同一帧一起启动
        if (vanishParticleEffect != null)
        {
            if (showDebugLogs) Debug.Log("[FarewellSequence] 粒子特效与人物消失同步启动");
            vanishParticleEffect.PlayEffect();
        }

        float vanishTime = characterVanishDelayAfterParticleStart;
        float t = 0f;
        while (t < vanishTime)
        {
            t += Time.deltaTime;
            float pct = t / vanishTime;

            if (floatUpWhileDisappearing)
            {
                float yOffset = Mathf.Lerp(0f, floatUpDistance, pct);
                finalGroup.transform.position = startPos + Vector3.up * yOffset;
            }
            if (glowLight != null)
            {
                glowLight.intensity = Mathf.Lerp(startGlow, startGlow * glowIntensityBoost, pct);
            }
            yield return null;
        }

        // 粒子播放到一半，人物在这一刻消失
        if (showDebugLogs) Debug.Log($"[FarewellSequence] 粒子播放{vanishTime:F2}秒后，人物消失，触发 onGroup4Appeared");

        if (flashQuadRenderer != null)
        {
            flashQuadRenderer.gameObject.SetActive(true);
            yield return FadeRenderer(flashQuadRenderer, 0f, 1f, flashInDuration);

            finalGroup.SetActive(false);
            onGroup4Appeared.Invoke();
            if (glowLight != null) glowLight.intensity = 0f;

            yield return FadeRenderer(flashQuadRenderer, 1f, 0f, flashOutDuration);
            flashQuadRenderer.gameObject.SetActive(false);
        }
        else
        {
            finalGroup.SetActive(false);
            onGroup4Appeared.Invoke();
            if (glowLight != null) glowLight.intensity = 0f;
        }

        if (doorAppearDelay > 0f)
            yield return new WaitForSeconds(doorAppearDelay);

        ShowDoor();
    }

    IEnumerator FadeRenderer(Renderer r, float from, float to, float duration)
    {
        float t = 0f;
        Color c = r.material.color;
        c.a = from;
        r.material.color = c;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            r.material.color = c;
            yield return null;
        }
        c.a = to;
        r.material.color = c;
    }

    public void ShowDoor()
    {
        if (door != null) door.SetActive(true);
        onFarewellComplete.Invoke();
        StartCoroutine(EntryPromptAfterDelay());
    }

    IEnumerator EntryPromptAfterDelay()
    {
        yield return new WaitForSeconds(entryPromptDelay);
        if (showDebugLogs) Debug.Log("[FarewellSequence] 触发进门提示+旁白");
        onEntryPromptReady.Invoke();
    }
}
