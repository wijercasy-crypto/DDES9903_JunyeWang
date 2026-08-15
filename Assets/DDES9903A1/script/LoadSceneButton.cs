using UnityEngine;
using UnityEngine.SceneManagement;

// 挂在任意按钮物体上，点击后加载指定场景。
// 接到 EZPZ Interactable General 的 On Primary Interact() 事件上即可。
public class LoadSceneButton : MonoBehaviour
{
    [Tooltip("要加载的场景名，需要跟 Build Settings 里注册的名字完全一致")]
    public string sceneName = "FirstRoom";

    // 挂在按钮的 On Primary Interact() 上
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
