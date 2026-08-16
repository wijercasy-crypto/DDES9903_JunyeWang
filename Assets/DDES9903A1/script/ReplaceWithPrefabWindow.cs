using UnityEngine;
using UnityEditor;

// 编辑器工具，不是运行时脚本。必须放在名叫 "Editor" 的文件夹下才能生效
// （随便哪个位置的 Editor 文件夹都行，比如 Assets/DDES9903A1/Editor/）
//
// 用法：
// 1. 顶部菜单栏 Tools -> 替换选中物体为Prefab，打开这个小窗口
// 2. 把目标 Prefab 拖进窗口里的 Prefab 字段
// 3. 在 Hierarchy 里框选所有要被替换的物体
// 4. 点窗口里的"替换选中的物体"按钮
// 每个被选中的物体会被替换成 Prefab 的实例，位置/旋转/缩放/名字都会保留，父物体也不变
public class ReplaceWithPrefabWindow : EditorWindow
{
    private GameObject prefab;

    [MenuItem("Tools/替换选中物体为Prefab")]
    static void ShowWindow()
    {
        GetWindow<ReplaceWithPrefabWindow>("替换为Prefab");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("先在下面拖入目标Prefab，再去Hierarchy里框选要替换的物体，最后点下面的按钮。", MessageType.Info);

        prefab = (GameObject)EditorGUILayout.ObjectField("目标 Prefab", prefab, typeof(GameObject), false);

        GUILayout.Space(10);

        if (GUILayout.Button("替换选中的物体", GUILayout.Height(30)))
        {
            if (prefab == null)
            {
                Debug.LogWarning("[替换工具] 请先拖入一个 Prefab");
                return;
            }

            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                Debug.LogWarning("[替换工具] Hierarchy 里没有选中任何物体");
                return;
            }

            int count = 0;
            foreach (GameObject go in selected)
            {
                Transform parent = go.transform.parent;
                Vector3 localPos = go.transform.localPosition;
                Quaternion localRot = go.transform.localRotation;
                Vector3 localScale = go.transform.localScale;
                string originalName = go.name;
                int siblingIndex = go.transform.GetSiblingIndex();

                GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.scene);
                newInstance.transform.SetParent(parent);
                newInstance.transform.localPosition = localPos;
                newInstance.transform.localRotation = localRot;
                newInstance.transform.localScale = localScale;
                newInstance.transform.SetSiblingIndex(siblingIndex);
                newInstance.name = originalName;

                Undo.RegisterCreatedObjectUndo(newInstance, "Replace With Prefab");
                Undo.DestroyObjectImmediate(go);

                count++;
            }

            Debug.Log($"[替换工具] 已替换 {count} 个物体");
        }
    }
}
