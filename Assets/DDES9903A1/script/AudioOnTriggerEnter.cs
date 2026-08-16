using UnityEngine;

// 挂在一个有 Trigger Collider 的物体上（Collider勾选 Is Trigger）。
// 玩家进入这个区域时播放一次配好的语音。
// 跟 FloatingSubtitle 的文字淡入淡出逻辑完全独立，互不干扰，
// 建议把这个物体的 Collider 范围放在跟 FloatingSubtitle 的
// showDistance/fullDistance 差不多的位置，这样声音和文字出现的时机能对上。
public class AudioOnTriggerEnter : MonoBehaviour
{
    [Tooltip("留空会自动在这个物体上添加一个 AudioSource")]
    public AudioSource audioSource;
    public AudioClip clip;

    [Tooltip("勾选=只播放一次；取消勾选=每次玩家进入区域都会重新播放")]
    public bool playOnce = true;

    private bool hasPlayed = false;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnce && hasPlayed) return;

        hasPlayed = true;
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
