using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 引导总管 —— 控制游乐设施的发光顺序，引导玩家逐个体验。
/// 
/// 流程：
/// 游戏开始 → 第一个设施发光 → 玩家完成交互 → 它熄灭、下一个发光 → ...
/// 每个设施只发光一次，完成后永久熄灭。
/// 
/// 挂载步骤：
/// 1. 在场景里创建空物体，命名 "GuideManager"，挂上此脚本
/// 2. 在 Guide Sequence 列表里，按引导顺序拖入每个设施
///    （顺序：旋转木马 → 旋转火箭 → 过山车 → 摩天轮）
///    —— 每个设施必须挂有 GlowController 脚本
/// 3. 各设施的交互脚本在玩家完成体验时，调用：
///    GuideManager.Instance.CompleteCurrent(这个设施的Transform);
/// </summary>
public class GuideManager : MonoBehaviour
{
    // 单例，方便其他脚本调用
    public static GuideManager Instance { get; private set; }

    [Header("引导顺序（按剧情顺序拖入设施）")]
    [Tooltip("旋转木马 → 旋转火箭 → 过山车 → 摩天轮。每个设施需挂 GlowController。")]
    public List<GlowController> guideSequence = new List<GlowController>();

    [Header("开始设置")]
    [Tooltip("游戏开始后多久点亮第一个设施（秒）")]
    public float startDelay = 1f;

    // ── 内部状态 ──
    private int currentIndex = -1;   // 当前发光的设施索引

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (guideSequence.Count == 0)
        {
            Debug.LogWarning("[引导总管] Guide Sequence 列表是空的！请拖入设施。");
            return;
        }

        // 延迟点亮第一个设施
        Invoke(nameof(LightNext), startDelay);
    }

    // ─────────────────────────────────────────────
    // 公开方法
    // ─────────────────────────────────────────────

    /// <summary>
    /// 玩家完成了某个设施的体验，调用此方法。
    /// 会熄灭当前设施，点亮下一个。
    /// 用法（在设施交互脚本里）：
    ///   GuideManager.Instance.CompleteCurrent(transform);
    /// </summary>
    public void CompleteCurrent(Transform facility)
    {
        // 检查是不是当前正在发光的那个设施
        if (currentIndex < 0 || currentIndex >= guideSequence.Count) return;

        GlowController current = guideSequence[currentIndex];
        if (current == null) return;

        // 确认完成的是当前该完成的设施（防止玩家跳关）
        // 双向检查：完成物体是目标本身、目标的子物体、或目标是完成物体的子物体都算
        bool isMatch = facility == current.transform
                    || facility.IsChildOf(current.transform)
                    || current.transform.IsChildOf(facility);
        if (!isMatch)
        {
            Debug.Log($"[引导总管] {facility.name} 不是当前引导目标（当前目标：{current.gameObject.name}），忽略");
            return;
        }

        Debug.Log($"[引导总管] 完成了第 {currentIndex + 1} 个设施：{current.gameObject.name}");

        // 熄灭当前设施
        current.StopGlow();

        // 点亮下一个
        LightNext();
    }

    /// <summary>点亮序列中的下一个设施</summary>
    private void LightNext()
    {
        currentIndex++;

        if (currentIndex >= guideSequence.Count)
        {
            // 所有设施都完成了
            Debug.Log("[引导总管] 所有设施都已完成引导！");
            OnAllCompleted();
            return;
        }

        GlowController next = guideSequence[currentIndex];
        if (next != null)
        {
            next.StartGlow();
            Debug.Log($"[引导总管] 现在引导玩家前往第 {currentIndex + 1} 个设施：{next.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[引导总管] 第 {currentIndex + 1} 个设施是空的，跳过");
            LightNext(); // 跳过空的，继续下一个
        }
    }

    /// <summary>
    /// 所有设施都完成后触发（你可以在这里加后续逻辑）
    /// 例如：让皮球出现引导玩家走向白门、播放字幕等
    /// </summary>
    private void OnAllCompleted()
    {
        // TODO: 所有回忆唤起后，引导玩家走向白门的逻辑
    }

    /// <summary>获取当前正在引导的设施（供其他脚本查询）</summary>
    public Transform GetCurrentTarget()
    {
        if (currentIndex < 0 || currentIndex >= guideSequence.Count) return null;
        if (guideSequence[currentIndex] == null) return null;
        return guideSequence[currentIndex].transform;
    }
}
