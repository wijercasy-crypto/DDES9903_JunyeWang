using UnityEngine;

// 挂在留声机上，配合EZPZ Interactable General使用。
// 玩家点击后开始播放音乐。
public class GramophonePlay : MonoBehaviour
{
    [Tooltip("留空会自动在这个物体上添加一个 AudioSource")]
    public AudioSource audioSource;
    public AudioClip musicClip;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    // 挂在 EZPZ Interactable General 的 OnPrimaryInteract() 上
    // 点一下播放，再点一下关闭（toggle）
    public void PlayMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            return;
        }

        if (musicClip == null) return;
        audioSource.clip = musicClip;
        audioSource.Play();
    }
}
