using UnityEngine;
using System.Collections;

// 挂在皮球上，配合EZPZ Interactable General使用。
// 玩家点击后，球弹跳两下，弹完自动归位、恢复Kinematic，不会真的掉出世界。
//
// 重要：球自己的 Collider 必须取消勾选 "Is Trigger"，
// 否则球不会真正被地板挡住/托住，会直接穿过去。
public class BallBounce : MonoBehaviour
{
    [Tooltip("弹跳力度")]
    public float bounceForce = 5f;
    [Tooltip("保险：弹跳的最长等待时间上限")]
    public float maxWaitPerBounce = 3f;

    private Rigidbody rb;
    private bool bouncing = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // 强制开启重力，不管这个物体原本的Rigidbody是不是关着重力
        rb.useGravity = true;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // 默认保持Kinematic，不受重力/物理影响，避免没有Collider或者地板没碰撞体时球掉飞
        rb.isKinematic = true;
    }

    // 挂在 EZPZ Interactable General 的 OnPrimaryInteract() 上
    public void Bounce()
    {
        if (bouncing) return;
        rb.isKinematic = false; // 点击的这一刻才真正启用物理，开始弹跳
        StartCoroutine(BounceTwice());
    }

    IEnumerator BounceTwice()
    {
        bouncing = true;

        // 用物理公式直接算一次完整"起跳->落地"要花多久，
        // 不再用"测速度是否接近0"这种会被抛物线顶点骗到的方法
        float gravity = Mathf.Abs(Physics.gravity.y);
        float singleBounceDuration = gravity > 0.01f ? (2f * bounceForce / gravity) : maxWaitPerBounce;
        singleBounceDuration = Mathf.Min(singleBounceDuration, maxWaitPerBounce);

        rb.linearVelocity = Vector3.up * bounceForce;
        yield return new WaitForSeconds(singleBounceDuration);

        rb.linearVelocity = Vector3.up * bounceForce;
        yield return new WaitForSeconds(singleBounceDuration);

        // 弹完了，不管物理最后把球带去了哪，强制归位，避免掉出世界/滚跑
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        rb.isKinematic = true;

        bouncing = false;
    }
}
