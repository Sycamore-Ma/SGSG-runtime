using UnityEngine;

public class ProximityAndVerticalFadeShaderController : MonoBehaviour
{
    [Header("🎯 绑定透明目标 Transform")]
    public Transform leftTarget;
    public Transform rightTarget;

    [Header("🧵 所使用的材质")]
    public Material shaderMaterial;

    void Update()
    {
        if (shaderMaterial != null)
        {
            if (leftTarget != null)
            {
                shaderMaterial.SetVector("_TargetPosA", leftTarget.position);
            }

            if (rightTarget != null)
            {
                shaderMaterial.SetVector("_TargetPosB", rightTarget.position);
            }
        }
    }
}
