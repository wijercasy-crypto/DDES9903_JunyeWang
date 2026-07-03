using UnityEngine;

/// <summary>
/// 摩天轮座舱保持水平。轮子转动时，座舱始终保持底部朝下（像真实摩天轮吊舱），
/// 玩家不会被转得头下脚上。挂在每个座舱上。
/// </summary>
public class FerrisCabinLevel : MonoBehaviour
{
    // 记录初始的世界旋转（水平姿态）
    private Quaternion fixedWorldRotation;

    private void Start()
    {
        fixedWorldRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        // 不管父级（轮子）怎么转，座舱始终保持初始的世界朝向 → 保持水平
        transform.rotation = fixedWorldRotation;
    }
}