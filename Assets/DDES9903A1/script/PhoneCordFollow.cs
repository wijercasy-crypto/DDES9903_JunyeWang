using UnityEngine;

/// <summary>
/// 电话线跟随（多骨骼平滑版）—— 整条骨骼链按比例跟随听筒，让电话线平滑弯曲拉伸。
/// 
/// 原理：
/// 不只驱动末端一节骨头，而是让链上每节骨头按"离座机的距离比例"跟随听筒。
/// 越靠近听筒的骨头跟随越多，越靠近座机的跟随越少，形成平滑过渡的曲线。
/// 
/// 挂载步骤：
/// 1. 把此脚本挂在 Phone_Rigged 上
/// 2. Handset：拖入听筒 Mesh_0.001
/// 3. Cord Attach Point：拖入听筒上的连接点 CordAttachPoint
/// 4. Bones 列表：按【从座机端到听筒端】的顺序，依次拖入所有骨头
///    - 元素0：骨骼（座机端，第一节）
///    - 元素1：骨骼.001
///    - 元素2：骨骼.002
///    - 元素3：骨骼.003
///    - 元素4：骨骼.004（听筒端，最后一节）
///    顺序很重要！从座机到听筒依次排列。
/// </summary>
public class PhoneCordFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("听筒物体（Mesh_0.001）")]
    public Transform handset;

    [Tooltip("听筒上电话线连接的点（CordAttachPoint）")]
    public Transform cordAttachPoint;

    [Header("骨骼链（从座机端到听筒端，按顺序拖入）")]
    [Tooltip("元素0=座机端第一节，最后一个元素=听筒端。顺序必须正确！")]
    public Transform[] bones;

    [Header("跟随设置")]
    [Tooltip("跟随的平滑程度。0=瞬间，越大越柔和有惯性")]
    [Range(0f, 20f)]
    public float followSmoothing = 10f;

    [Tooltip("弯曲的分布曲线。默认让靠听筒端弯曲更多，更自然")]
    public AnimationCurve influenceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ── 内部状态 ──
    private Vector3[] boneOriginalLocalPos;   // 每节骨头的初始局部位置
    private Vector3[] boneRestWorldPos;        // 每节骨头的初始世界位置（静止状态）
    private bool initialized = false;

    private void Start()
    {
        if (handset == null || bones == null || bones.Length < 2)
        {
            Debug.LogWarning("[电话线] 请设置听筒，并按顺序拖入至少2节骨头！");
            return;
        }

        // 记录每节骨头的初始世界位置（作为"静止形状"的基准）
        boneRestWorldPos = new Vector3[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
                boneRestWorldPos[i] = bones[i].position;
        }

        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized) return;

        // 听筒连接点的当前位置（目标）
        Transform target = cordAttachPoint != null ? cordAttachPoint : handset;
        Vector3 targetPos = target.position;

        // 末端骨头（听筒端）应该到达的位置 = 听筒连接点
        // 计算末端骨头从"静止位置"到"听筒位置"的偏移量
        int lastIndex = bones.Length - 1;
        Vector3 endOffset = targetPos - boneRestWorldPos[lastIndex];

        // 对每节骨头，按比例施加偏移
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;

            // 计算这节骨头的影响比例（0=座机端不动，1=听筒端全跟随）
            float t = (float)i / lastIndex;           // 0 到 1 线性分布
            float influence = influenceCurve.Evaluate(t);  // 用曲线让分布更自然

            // 这节骨头的目标位置 = 静止位置 + 按比例的偏移
            Vector3 desiredPos = boneRestWorldPos[i] + endOffset * influence;

            // 平滑移动过去
            if (followSmoothing <= 0.01f)
                bones[i].position = desiredPos;
            else
                bones[i].position = Vector3.Lerp(bones[i].position, desiredPos, followSmoothing * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────
    // Scene 视图画出骨骼链和连接点（调试用）
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (bones == null || bones.Length == 0) return;

        // 画骨骼链
        Gizmos.color = Color.green;
        for (int i = 0; i < bones.Length - 1; i++)
        {
            if (bones[i] != null && bones[i + 1] != null)
                Gizmos.DrawLine(bones[i].position, bones[i + 1].position);
        }
        // 每节骨头画个点
        Gizmos.color = Color.yellow;
        foreach (var b in bones)
            if (b != null) Gizmos.DrawSphere(b.position, 0.015f);

        // 画听筒连接点
        if (handset != null)
        {
            Transform target = cordAttachPoint != null ? cordAttachPoint : handset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(target.position, 0.025f);
        }
    }
}
