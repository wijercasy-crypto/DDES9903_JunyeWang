using UnityEngine;

// 挂在水面物体上，让材质的法线贴图缓慢滚动，模拟波纹流动。
// 不需要Shader Graph，直接用URP Lit材质自带的贴图偏移(Offset)属性实现。
public class WaterScroll : MonoBehaviour
{
    [Tooltip("水面滚动方向和速度，比如(0.02, 0.01)表示斜着缓慢流动")]
    public Vector2 scrollSpeed = new Vector2(0.02f, 0.01f);

    private Renderer rend;
    private Vector2 offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        offset += scrollSpeed * Time.deltaTime;
        rend.material.SetTextureOffset("_BaseMap", offset);
    }
}
