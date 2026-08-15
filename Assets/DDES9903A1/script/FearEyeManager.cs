using UnityEngine;

// 挂在场景里一个空物体上（比如叫 "FearEyeManager"）。
// 沿走廊分几个阶段，玩家走到每个阶段的触发点时，该阶段对应的一批眼睛会被激活。
// 越往前走，激活的眼睛越多，营造"越来越多眼睛在看你"的压迫感。
public class FearEyeManager : MonoBehaviour
{
    [System.Serializable]
    public class EyeStage
    {
        [Tooltip("玩家走到这个点附近，下面这些眼睛就会出现")]
        public Transform triggerPoint;
        [Tooltip("这个阶段要激活的眼睛们（提前摆好位置，初始设为不激活）")]
        public GameObject[] eyesToActivate;
    }

    [Tooltip("玩家Transform，留空自动按Tag查找")]
    public Transform player;

    [Tooltip("按玩家会经过的顺序，从少到多排列")]
    public EyeStage[] stages;

    [Tooltip("玩家进入这个触发点多近时，激活对应这批眼睛")]
    public float triggerRadius = 3f;

    public bool showDebugLogs = true;

    private bool[] triggered;

    void Start()
    {
        triggered = new bool[stages.Length];

        foreach (var stage in stages)
            foreach (var eye in stage.eyesToActivate)
                if (eye != null) eye.SetActive(false);
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (player == null) return;
        }

        for (int i = 0; i < stages.Length; i++)
        {
            if (triggered[i]) continue;
            if (stages[i].triggerPoint == null) continue;

            float dist = Vector3.Distance(player.position, stages[i].triggerPoint.position);
            if (dist <= triggerRadius)
            {
                triggered[i] = true;
                foreach (var eye in stages[i].eyesToActivate)
                    if (eye != null) eye.SetActive(true);

                if (showDebugLogs)
                    Debug.Log($"[FearEyeManager] 第{i}阶段触发，激活了{stages[i].eyesToActivate.Length}只眼睛");
            }
        }
    }
}
