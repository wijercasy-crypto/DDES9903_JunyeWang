using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在"事故报告"物体上。桌面模式专用（照抄 HandsetPickup 的拿取方式，不用任何 XR 组件）。
///
/// 玩家点击报告（走 Interactable General 的 On Primary Interact 事件）：
/// 1. 报告飞到摄像机前固定位置，像"举起来看"一样，然后跟随视角
/// 2. 报告到位后，自动开始走廊渐显：走廊（墙体/画/门）材质透明度从 0 到 1，
///    像旋转木马发光那样"缓缓出现"，玩家全程能看见，不需要黑幕遮挡
/// 3. 走廊完全变实（不透明）之后，此时已经把房间挡住了，
///    顺手把房间其他部分关掉（玩家看不出来，因为已经被挡住了）
/// 走廊尽头怎么走由物理墙体限制，到达发光门后交给你现有的 PortalOnTouch 处理。
///
/// 重要：corridorRenderers 里的物体材质必须支持透明度渐变
/// （URP 用 Lit/Unlit 且 Surface Type 选 Transparent；内置管线用 Standard 的 Rendering Mode 选 Transparent/Fade）。
/// 不支持透明的材质做不出"缓缓出现"的效果，会直接一下子全部显示。
///
/// 挂载步骤：
/// 1. 挂在 report 物体上
/// 2. Player Camera：拖入主摄像机（不填会自动找 Camera.main）
/// 3. 调 Camera Local Position / Rotation，把报告调到视野里想要的位置和角度（像举着看）
/// 4. Corridor Root / Corridor Renderers / Room Elements To Hide 按之前说的配好
/// 5. Interactable General 的 On Primary Interact () → ReportMemorySequence.OnReportInteracted()
/// </summary>
public class ReportMemorySequence : MonoBehaviour
{
    [Header("摄像机（举起报告用）")]
    [Tooltip("主摄像机（玩家的眼睛）。不填自动用 Camera.main")]
    public Camera playerCamera;

    [Tooltip("报告相对摄像机的位置。X=左右, Y=上下, Z=前后(正数在前方)。" +
             "例如 (0.15, -0.1, 0.4) = 稍微偏右下、在前方40厘米")]
    public Vector3 cameraLocalPosition = new Vector3(0.15f, -0.1f, 0.4f);

    [Tooltip("报告相对摄像机的旋转角度（欧拉角），调整报告朝向，让它像被举起来正对着看")]
    public Vector3 cameraLocalRotation = new Vector3(0f, 0f, 0f);

    [Tooltip("报告飞到视野前的动画时长（秒）")]
    public float liftAnimationDuration = 1f;

    [Header("走廊渐显")]
    [Tooltip("走廊的根物体（corridor 父物体），触发时会先 SetActive(true)，" +
             "但材质透明度从 0 开始，靠下面的渐变动画慢慢显现")]
    public GameObject corridorRoot;

    [Tooltip("走廊上所有需要渐显的 Renderer（墙、地板、门等），" +
             "可以用 corridorRoot.GetComponentsInChildren<Renderer>() 自动收集，" +
             "或者手动把每一个拖进来")]
    public Renderer[] corridorRenderers;

    [Tooltip("不方便挪进 corridor 层级下面的物体（比如 Gate，挪进去会因为父物体非等比缩放导致歪斜），" +
             "但同样需要跟着走廊一起被激活显示。这里的物体会在走廊渐显开始时一起 SetActive(true)")]
    public GameObject[] extraObjectsToActivate;

    [Header("门单独的渐显节奏（比墙慢）")]
    [Tooltip("门（Gate）上需要渐显的 Renderer，不要再放进 Corridor Renderers 里，" +
             "这里单独控制，可以比墙渐显得更久")]
    public Renderer[] gateRenderers;

    [Tooltip("门渐显耗时，建议比 Corridor Fade Duration 更长，比如墙 2.5 秒、门给 4~5 秒")]
    public float gateFadeDuration = 4.5f;

    [Tooltip("走廊材质完全变实之后，要隐藏的房间其他部分（墙壁、地板、其他家具等）。" +
             "注意：桌子/抽屉这一套不要放进来，它们要保留在原地作为走廊的'锚点'")]
    public GameObject[] roomElementsToHide;

    [Header("时间设置")]
    [Tooltip("走廊渐显耗时，调大一点更有'缓缓浮现'的感觉，比如 2~3 秒")]
    public float corridorFadeDuration = 2.5f;

    private bool triggered = false;
    private bool followingCamera = false;

    private void LateUpdate()
    {
        // 报告到位后，每帧保持在摄像机前的固定位置（跟随视角转动）
        if (followingCamera)
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            transform.position = cam.transform.TransformPoint(cameraLocalPosition);
            transform.rotation = cam.transform.rotation * Quaternion.Euler(cameraLocalRotation);
        }
    }

    private Camera GetCamera()
    {
        if (playerCamera != null)
            return playerCamera;
        return Camera.main;
    }

    // 接到 Interactable General 的 On Primary Interact () 事件上
    public void OnReportInteracted()
    {
        if (triggered) return; // 防止重复触发
        triggered = true;

        StartCoroutine(PickUpThenRevealCorridor());
    }

    private IEnumerator PickUpThenRevealCorridor()
    {
        Camera cam = GetCamera();
        if (cam == null)
        {
            Debug.LogWarning("[报告] 找不到有效摄像机，无法举起报告！");
            yield break;
        }

        // 拿起时脱离原来的父物体（抽屉），这样能自由飞到摄像机前
        transform.SetParent(null, true);

        // 关掉物理碰撞，避免飞的过程中被物理干扰
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;
        while (elapsed < liftAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / liftAnimationDuration);

            Camera c = GetCamera();
            if (c == null) yield break;

            Vector3 targetPos = c.transform.TransformPoint(cameraLocalPosition);
            Quaternion targetRot = c.transform.rotation * Quaternion.Euler(cameraLocalRotation);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // 到位后开启"每帧跟随摄像机"
        followingCamera = true;
        Debug.Log("[报告] 已举到视野前");

        // 报告拿稳了，开始走廊渐显
        yield return StartCoroutine(FadeInCorridor());
    }

    private IEnumerator FadeInCorridor()
    {
        if (corridorRoot != null) corridorRoot.SetActive(true);

        // 激活那些不方便嵌套进 corridor 层级、但也需要一起显示的物体（比如 Gate）
        if (extraObjectsToActivate != null)
        {
            foreach (var obj in extraObjectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // 墙/地板等：按 corridorFadeDuration 渐显
        StartCoroutine(FadeRenderersAlpha(corridorRenderers, corridorFadeDuration));

        // 门：按 gateFadeDuration 渐显（和墙同时开始，但耗时更久，显得更慢）
        StartCoroutine(FadeRenderersAlpha(gateRenderers, gateFadeDuration));

        // 等两边都渐显完（取较长的那个时间），再隐藏房间其他部分
        float waitTime = Mathf.Max(corridorFadeDuration, gateFadeDuration);
        yield return new WaitForSeconds(waitTime);

        // 走廊（和门）已经完全变实、挡住了房间，这时候关掉房间其他部分，玩家不会察觉
        if (roomElementsToHide != null)
        {
            foreach (var obj in roomElementsToHide)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    /// <summary>通用渐显：把一组 Renderer 的材质 alpha 从 0 渐变到 1</summary>
    private IEnumerator FadeRenderersAlpha(Renderer[] renderers, float duration)
    {
        if (renderers == null || renderers.Length == 0)
            yield break;

        Material[] mats = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            mats[i] = renderers[i].material; // .material 会自动实例化，不影响其他共用同材质的物体
            Color c = mats[i].color;
            c.a = 0f;
            mats[i].color = c;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            for (int i = 0; i < mats.Length; i++)
            {
                Color c = mats[i].color;
                c.a = alpha;
                mats[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            Color c = mats[i].color;
            c.a = 1f;
            mats[i].color = c;
        }
    }
}
