#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;      // 运行时+编辑器双模态

[ExecuteAlways]
public class SceneViewBillboard : MonoBehaviour
{
    void Update()
    {
        // if (!Application.isPlaying)      // 运行时+编辑器双模态
        // {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                Transform camTransform = sceneView.camera.transform;
                transform.LookAt(transform.position + camTransform.forward);
            }
        // }
    }
}
#endif
