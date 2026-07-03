using UnityEngine;

public class HeightRevealer : MonoBehaviour
{
    public Transform player;
    public float revealHeight = 25f;
    public GameObject targetObject;   // 要显隐的小女孩（Child Girl 父物体）

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null || targetObject == null) return;
        bool show = player.position.y >= revealHeight;
        if (targetObject.activeSelf != show)
            targetObject.SetActive(show);
    }
}