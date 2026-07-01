using UnityEngine;
using System.Collections;

/// <summary>
/// 听筒拿起动画（第一视角贴耳版）
/// 玩家点击电话 → 听筒飞到摄像机前方固定位置（像举着电话贴耳）→ 跟随视角。
/// 再次点击 → 听筒飞回座机原位挂断。
/// 
/// 注意：听筒飞到脸前时离座机很远，电话线会被拉长。配合 PhoneCordFollow 的
/// 平滑跟随，线会尽量自然，但大幅移动时线较长属于正常。
/// 
/// 挂载步骤：
/// 1. 挂在电话 Phone_Rigged 上
/// 2. Handset：拖入听筒 Mesh_0.001
/// 3. Player Camera：拖入主摄像机（MainCamera），不填会自动找 Camera.main
/// 4. 调 Camera Local Position / Rotation 设置听筒在视野里的位置和角度
/// 5. Interactable General 的 On Primary Interact () → HandsetPickup.ToggleHandset()
/// 6. 放入铃声、通话语音
/// </summary>
public class HandsetPickup : MonoBehaviour
{
    [Header("听筒")]
    public Transform handset;

    [Header("摄像机")]
    [Tooltip("主摄像机（玩家的眼睛）。不填自动用 Camera.main")]
    public Camera playerCamera;

    [Header("听筒在视野里的位置（相对摄像机）")]
    [Tooltip("听筒相对摄像机的位置。X=左右, Y=上下, Z=前后(正数在前方)。" +
             "例如 (0.15, -0.1, 0.35) = 稍微偏右下、在前方35厘米。慢慢调到贴耳的感觉")]
    public Vector3 cameraLocalPosition = new Vector3(0.15f, -0.05f, 0.35f);

    [Tooltip("听筒相对摄像机的旋转角度（欧拉角）。调整话筒朝向，让它像贴着耳朵")]
    public Vector3 cameraLocalRotation = new Vector3(0f, 90f, 100f);

    [Header("动画")]
    [Tooltip("听筒飞起/飞回的时长（秒）")]
    public float animationDuration = 1f;

    [Header("音效")]
    [Tooltip("响铃音源。在 Inspector 里手动加一个 AudioSource 拖进来，就能可视化调 3D 范围")]
    public AudioSource ringSource;      // ← 改成 public，从 Inspector 拖入

    public AudioClip ringSound;
    public AudioClip callSound;
    public bool ringOnStart = true;

    [Header("字幕（可选）")]
    [TextArea(2, 4)]
    public string subtitleText = "……爸爸……你还记得我们吗……";

    [Header("剧情触发（可选）")]
    public float afterCallDelay = 2f;

    // ── 内部状态 ──
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool isLifted = false;
    private bool isAnimating = false;
    private bool hasAnswered = false;
    private bool followingCamera = false;   // 是否正在跟随摄像机

  
    private AudioSource callSource;

    private void Start()
    {
        if (handset == null)
        {
            Debug.LogWarning("[听筒] 没有设置听筒 Handset！");
            return;
        }

        // 记录听筒初始的父物体、局部位置和旋转
        originalParent = handset.parent;
        originalLocalPos = handset.localPosition;
        originalLocalRot = handset.localRotation;

        // 音源设置
        // ↓ 删掉原来 ringSource = gameObject.AddComponent<AudioSource>() 那一整段
        // 改成：只把 clip 赋上，其它 3D 参数交给你在 Inspector 里调
        if (ringSource != null)
        {
            ringSource.clip = ringSound;
            ringSource.loop = true;
            ringSource.playOnAwake = false;
        }

        // callSource 保持原样，还是动态创建
        callSource = gameObject.AddComponent<AudioSource>();
        callSource.clip = callSound;
        callSource.loop = false;
        callSource.spatialBlend = 0.3f;
        callSource.minDistance = 2f;
        callSource.maxDistance = 15f;

        if (ringOnStart && ringSound != null)
        {
            ringSource.Play();
            Debug.Log("[电话] 开始响铃……");
        }
    }

    private void LateUpdate()
    {
        if (handset == null) return;

        // 听筒跟随摄像机（飞到位后，每帧保持在摄像机前的固定位置）
        if (followingCamera)
        {
            Camera cam = GetCamera();
            if (cam == null) return;  // 摄像机无效则跳过，不报错

            Vector3 targetWorldPos = cam.transform.TransformPoint(cameraLocalPosition);
            Quaternion targetWorldRot = cam.transform.rotation * Quaternion.Euler(cameraLocalRotation);

            handset.position = targetWorldPos;
            handset.rotation = targetWorldRot;
        }
    }

    /// <summary>安全获取当前有效的摄像机（避免用到已销毁的引用）</summary>
    private Camera GetCamera()
    {
        // 如果手动指定的摄像机还有效，就用它
        if (playerCamera != null)
            return playerCamera;
        // 否则实时获取主摄像机
        return Camera.main;
    }

    /// <summary>拿起/放下听筒。挂到 Interactable General 事件。</summary>
    public void ToggleHandset()
    {
        if (isAnimating) return;
        if (handset == null) return;

        if (!isLifted)
            StartCoroutine(LiftToCamera());
        else
            StartCoroutine(LowerToBase());
    }

    /// <summary>听筒飞到摄像机前</summary>
    private IEnumerator LiftToCamera()
    {
        // 先确认有有效摄像机，没有就不执行（避免卡死）
        Camera cam = GetCamera();
        if (cam == null)
        {
            Debug.LogWarning("[听筒] 找不到有效摄像机，无法举起听筒！");
            yield break;
        }

        isAnimating = true;
        if (ringSource != null) ringSource.Stop();

        // 把听筒从座机层级中脱离（这样它能自由飞到摄像机前，不受座机变换影响）
        handset.SetParent(null, true);

        // 动画：从当前位置飞到摄像机前的目标位置
        Vector3 startPos = handset.position;
        Quaternion startRot = handset.rotation;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);

            // 每帧重新获取摄像机，避免用到失效引用
            Camera c = GetCamera();
            if (c == null) yield break;

            Vector3 targetPos = c.transform.TransformPoint(cameraLocalPosition);
            Quaternion targetRot = c.transform.rotation * Quaternion.Euler(cameraLocalRotation);

            handset.position = Vector3.Lerp(startPos, targetPos, t);
            handset.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // 到位后开启"每帧跟随摄像机"
        followingCamera = true;
        isLifted = true;
        isAnimating = false;

        Debug.Log("[电话] 听筒已举到耳边");

        if (!hasAnswered)
        {
            hasAnswered = true;
            StartCoroutine(PlayCall());
        }
    }

    /// <summary>听筒飞回座机</summary>
    private IEnumerator LowerToBase()
    {
        isAnimating = true;
        followingCamera = false;  // 停止跟随摄像机

        Vector3 startPos = handset.position;
        Quaternion startRot = handset.rotation;

        // 目标：回到座机上的原始位置
        // 先临时把听筒放回原父物体来计算原始世界位置
        handset.SetParent(originalParent, true);
        handset.localPosition = originalLocalPos;
        handset.localRotation = originalLocalRot;
        Vector3 basePos = handset.position;
        Quaternion baseRot = handset.rotation;
        // 再飞回去（先移回起点，动画过去）
        handset.position = startPos;
        handset.rotation = startRot;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            handset.position = Vector3.Lerp(startPos, basePos, t);
            handset.rotation = Quaternion.Slerp(startRot, baseRot, t);
            yield return null;
        }

        // 精确归位
        handset.localPosition = originalLocalPos;
        handset.localRotation = originalLocalRot;

        isLifted = false;
        isAnimating = false;
        if (callSource != null) callSource.Stop();

        Debug.Log("[电话] 听筒已放回");
    }

    private IEnumerator PlayCall()
    {
        if (!string.IsNullOrEmpty(subtitleText))
            Debug.Log("[电话字幕] " + subtitleText);

        if (callSource != null && callSound != null)
        {
            callSource.Play();
            Debug.Log("[电话] 播放通话语音……");
            yield return new WaitForSeconds(callSound.length);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        Debug.Log("[电话] 通话结束");
        yield return new WaitForSeconds(afterCallDelay);
        OnCallFinished();
    }

    private void OnCallFinished()
    {
        Debug.Log("[电话] 触发后续剧情（记忆恢复等）");
        // TODO: 第三章后续逻辑
    }
}
