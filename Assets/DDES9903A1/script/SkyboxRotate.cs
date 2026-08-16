using UnityEngine;

// 挂在场景里任意一个物体上（比如一个空物体）。
// 让当前场景的天空盒缓缓旋转，制造云在飘动的感觉。
public class SkyboxRotate : MonoBehaviour
{
    [Tooltip("旋转速度，单位：度/秒。数值越小转得越慢，越梦幻")]
    public float rotationSpeed = 0.5f;

    void Update()
    {
        if (RenderSettings.skybox == null) return;
        if (!RenderSettings.skybox.HasProperty("_Rotation")) return;

        float currentRotation = RenderSettings.skybox.GetFloat("_Rotation");
        currentRotation += rotationSpeed * Time.deltaTime;
        if (currentRotation > 360f) currentRotation -= 360f;

        RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
    }
}
