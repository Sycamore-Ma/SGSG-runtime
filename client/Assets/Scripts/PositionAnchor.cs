using UnityEngine;

/// <summary>
/// 让 `Tooltip` 保持稳定在 `Target` 上方，并不会随头盔视角移动
/// </summary>
public class PositionAnchor : MonoBehaviour
{
    public Transform Target;  // 🎯 绑定目标对象（Sphere）
    //public Vector3 Offset = new Vector3(0, 0.06f, 0); // 🎯 默认在目标上方 6cm
    public float Offset;
    public Camera Camera;

    void Update()
    {
        if (Target != null && Camera != null)
        {
            // 🎯 计算 front 向量（指向 Camera 的方向）
            Vector3 front = (Camera.transform.position - Target.position).normalized;
            //transform.position = Target.position + Offset; // 🎯 始终保持固定偏移
            transform.position = Target.position + front * Offset; // 🎯 始终保持固定偏移
        }
    }
}
