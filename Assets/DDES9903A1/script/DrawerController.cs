using UnityEngine;
using System.Collections;

/// <summary>
/// 抽屉控制。点击时抽屉滑出/滑回，平滑过渡。
/// 挂在每个抽屉物体上（TableDrawer_01 等）。
/// </summary>
public class DrawerController : MonoBehaviour
{
    [Header("拉开设置")]
    [Tooltip("抽屉拉开的方向（本地坐标）。通常是抽屉正面朝外的方向")]
    public Vector3 openDirection = Vector3.forward;

    [Tooltip("拉开的距离（米）")]
    public float openDistance = 0.4f;

    [Tooltip("开关抽屉的速度")]
    public float slideSpeed = 3f;

    // 内部状态
    private Vector3 closedLocalPos;   // 关闭时的本地位置
    private Vector3 openLocalPos;     // 打开时的本地位置
    private bool isOpen = false;
    private Coroutine slideRoutine;

    private void Start()
    {
        closedLocalPos = transform.localPosition;
        // 打开位置 = 关闭位置 + 方向 * 距离（用本地坐标）
        openLocalPos = closedLocalPos + openDirection.normalized * openDistance;
    }

    /// <summary>切换抽屉开/关（由 Interactable 点击调用）</summary>
    public void ToggleDrawer()
    {
        isOpen = !isOpen;
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(Slide(isOpen ? openLocalPos : closedLocalPos));
    }

    private IEnumerator Slide(Vector3 target)
    {
        while (Vector3.Distance(transform.localPosition, target) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, slideSpeed * Time.deltaTime);
            yield return null;
        }
        transform.localPosition = target;
    }
}