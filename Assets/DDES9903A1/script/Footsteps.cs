using UnityEngine;
using StarterAssets;   // 引入命名空间

public class Footsteps : MonoBehaviour
{
    [Header("脚步声")]
    public AudioClip[] footstepClips;
    public AudioSource audioSource;

    [Header("节奏")]
    public float stepInterval = 0.5f;

    [Header("音量")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private CharacterController controller;
    private StarterAssetsInputs input;
    private float stepTimer = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (audioSource == null || input == null) return;

        bool isMoving = input.move.magnitude > 0.1f;
        bool isGrounded = controller == null || controller.isGrounded;

        if (isMoving && isGrounded)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = footstepClips[0];
                audioSource.loop = true;
                audioSource.volume = volume;
                audioSource.Play();
            }
            // 跑步时音调加快(1.4倍速),走路正常(1倍)
            audioSource.pitch = input.sprint ? 1.4f : 1f;
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }
    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip, volume);
    }
}