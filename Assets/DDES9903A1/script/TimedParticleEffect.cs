using UnityEngine;
using System.Collections;

// 挂在粒子特效物体上。外部调用 PlayEffect() 触发播放，
// 播放 duration 秒后自动停止并隐藏。
// 可以直接挂在任何 UnityEvent 上，比如 FinalStopFarewellSequence 的 On Hand Touched ()。
public class TimedParticleEffect : MonoBehaviour
{
    public ParticleSystem effect;
    [Tooltip("播放后等多少秒自动消失")]
    public float duration = 3f;

    // 挂在 UnityEvent 上调用这个方法
    public void PlayEffect()
    {
        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        gameObject.SetActive(true);
        if (effect != null) effect.Play();

        yield return new WaitForSeconds(duration);

        if (effect != null) effect.Stop();
        gameObject.SetActive(false);
    }
}
