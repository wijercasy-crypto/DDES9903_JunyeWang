using UnityEngine;

/// <summary>
/// 灯塔灯光控制。调用 TurnOn() 时点亮并开始旋转扫射。
/// 挂在灯塔的 Spot Light 上（同物体需有 Light 和 SimpleRotate）。
/// </summary>
public class LighthouseLight : MonoBehaviour
{
    public Light spotLight;           // 灯塔聚光灯
    public SimpleRotate rotator;      // 旋转脚本

    private void Start()
    {
        if (spotLight == null) spotLight = GetComponent<Light>();
        if (rotator == null) rotator = GetComponent<SimpleRotate>();
        // 初始：灯灭、不转
        if (spotLight != null) spotLight.enabled = false;
        if (rotator != null) rotator.StopRotating();
    }

    /// <summary>点亮灯塔并开始旋转（高度触发时调用）</summary>
    public void TurnOn()
    {
        if (spotLight != null) spotLight.enabled = true;
        if (rotator != null) rotator.StartRotating();
    }

    /// <summary>熄灭（可选，降下来时）</summary>
    public void TurnOff()
    {
        if (spotLight != null) spotLight.enabled = false;
        if (rotator != null) rotator.StopRotating();
    }
}