using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 挂在"事故报告"物体上。桌面模式专用（照抄 HandsetPickup 的拿取方式，不用任何 XR 组件）。
///
/// 玩家点击报告（走 Interactable General 的 On Primary Interact 事件）：
/// 1. 报告飞到摄像机前固定位置，像"举起来看"一样，然后跟随视角
/// 2. 举起来之后有一个 putDownWindowDuration 的窗口期，这段时间内玩家可以
///    再次点击报告把它"放下"（飞回原位，重新可以拿起）
/// 3. 窗口期内没有点击"放下"，就自动开始走廊渐显：走廊（墙体/画/门）材质
///    透明度从 0 到 1，像旋转木马发光那样"缓缓出现"，玩家全程能看见，不需要黑幕遮挡
/// 4. 走廊完全变实（不透明）之后，此时已经把房间挡住了，
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

    [Header("放下选项")]
    [Tooltip("放下时飞回原位的动画时长（秒）")]
    public float putDownAnimationDuration = 0.6f;
    [Tooltip("举着报告的时候，按这个键直接放下（不依赖再次点击报告本身，因为报告贴脸太近时点击射线经常检测不到）")]
    public KeyCode putDownKey = KeyCode.Mouse0;
    [Tooltip("拿起后，这么短的时间内不响应放下按键，避免同一次点击被同时判定成\"拿起\"又\"放下\"")]
    public float putDownInputCooldown = 0.3f;

    private float heldSince = 0f;

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

    [Header("事件")]
    [Tooltip("走廊开始渐显的那一刻触发一次，比如挂BGM播放")]
    public UnityEvent onCorridorRevealStart;

    private enum ReportState { Idle, Held }
    private ReportState state = ReportState.Idle;

    private bool followingCamera = false;
    private bool corridorTriggered = false; // 走廊只在第一次拿起时触发一次，之后拿放不再影响它

    // 拿起前的原始状态，放下时要还原
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Coroutine activeRoutine;

    private void Update()
    {
        // 举着的时候按键直接放下，不依赖点击检测（贴脸物体点击射线经常检测不到）
        // 冷却期内不响应，避免拿起那一下点击被同时误判成放下
        if (state == ReportState.Held
            && Time.time - heldSince > putDownInputCooldown
            && Input.GetKeyDown(putDownKey))
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(PutDown());
        }
    }

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
    // 自由切换：Idle时点击=拿起，Held时点击=放下，可以反复拿放
    public void OnReportInteracted()
    {
        if (state == ReportState.Idle)
        {
            originalParent = transform.parent;
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;

            state = ReportState.Held;
            heldSince = Time.time;
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(PickUp());

            // 走廊渐显只在第一次拿起时触发一次，跟后续拿放无关
            if (!corridorTriggered)
            {
                corridorTriggered = true;
                StartCoroutine(FadeInCorridor());
            }
        }
        else if (state == ReportState.Held)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(PutDown());
        }
    }

    private IEnumerator PickUp()
    {
        Camera cam = GetCamera();
        if (cam == null)
        {
            Debug.LogWarning("[报告] 找不到有效摄像机，无法举起报告！");
            state = ReportState.Idle;
            yield break;
        }

        // 拿起时脱离原来的父物体（抽屉），这样能自由飞到摄像机前
        transform.SetParent(null, true);

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        // 注意：这里不关闭 Collider —— isKinematic=true 已经能防止物理力把它撞飞，
        // 留着 Collider 启用状态是为了让 EZPZ 的点击交互（靠Collider做射线检测）
        // 在报告举在手上的时候还能被再次点击到，才能触发"放下"

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

        followingCamera = true;
        Debug.Log("[报告] 已举到视野前");
    }

    private IEnumerator PutDown()
    {
        Debug.Log("[报告] 玩家选择放下");
        followingCamera = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetWorldPos = originalParent != null
            ? originalParent.TransformPoint(originalLocalPosition)
            : startPos;
        Quaternion targetWorldRot = originalParent != null
            ? originalParent.rotation * originalLocalRotation
            : startRot;

        float elapsed = 0f;
        while (elapsed < putDownAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / putDownAnimationDuration);
            transform.position = Vector3.Lerp(startPos, targetWorldPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetWorldRot, t);
            yield return null;
        }

        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        state = ReportState.Idle;
        Debug.Log("[报告] 已放回原位，可以再次拿起");
    }

    private IEnumerator FadeInCorridor()
    {
        onCorridorRevealStart.Invoke();

        if (corridorRoot != null) corridorRoot.SetActive(true);

        if (extraObjectsToActivate != null)
        {
            foreach (var obj in extraObjectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        StartCoroutine(FadeRenderersAlpha(corridorRenderers, corridorFadeDuration));
        StartCoroutine(FadeRenderersAlpha(gateRenderers, gateFadeDuration));

        float waitTime = Mathf.Max(corridorFadeDuration, gateFadeDuration);
        yield return new WaitForSeconds(waitTime);

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
            mats[i] = renderers[i].material;
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
