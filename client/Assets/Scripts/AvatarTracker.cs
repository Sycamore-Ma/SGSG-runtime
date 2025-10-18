using UnityEngine;

public class AvatarTracker : MonoBehaviour
{
    [Header("💡 目标绑定对象")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform hipsTarget; // 如果没有腰部追踪设备，可以为空

    [Header("🦴 模型骨骼")]
    private Transform headBone; 
    private Transform leftHandBone;
    private Transform rightHandBone;
    private Transform hipsBone;

    public Transform hipsVisualTarget; // 在 Inspector 中拖入 HipsTarget

    [Header("⚙️ 参数调节")]
    public Vector3 hipsOffset = new Vector3(0f, -0.66f, 0f); // 默认略低于头部或腰部追踪点
    public float hipsFollowSpeed = 5f; // 阻尼速度，越小越慢（比如 3~8）
    private Vector3 hipsVelocity; // 平滑阻尼时用的缓存变量（可选）

    void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = null; // 清空控制器
        }
        else
        {
            Debug.LogError("❌ 未找到 Animator 组件，请确保该脚本挂载在包含 Animator 的 GameObject 上。");
        }

        // ✅ 正确路径绑定
        headBone = transform.Find("Rig/B-root/B-hips/B-spine/B-chest/B-neck/B-head");
        leftHandBone = transform.Find("Rig/B-root/B-hips/B-spine/B-chest/B-shoulder.L/B-upperArm.L/B-forearm.L/B-hand.L");
        rightHandBone = transform.Find("Rig/B-root/B-hips/B-spine/B-chest/B-shoulder.R/B-upperArm.R/B-forearm.R/B-hand.R");
        hipsBone = transform.Find("Rig/B-root/B-hips");

        // ✅ Debug 检查
        if (headBone == null) Debug.LogError("❌ 未找到头部骨骼");
        if (leftHandBone == null) Debug.LogError("❌ 未找到左手骨骼");
        if (rightHandBone == null) Debug.LogError("❌ 未找到右手骨骼");
        if (hipsBone == null) Debug.LogError("❌ 未找到髋部骨骼");
    }

    void LateUpdate()
    {
        if (headTarget != null && headBone != null)
        {
            headBone.position = headTarget.position;
            headBone.rotation = headTarget.rotation;
        }

        if (leftHandTarget != null && leftHandBone != null)
        {
            leftHandBone.position = leftHandTarget.position;
            leftHandBone.rotation = leftHandTarget.rotation;
        }

        if (rightHandTarget != null && rightHandBone != null)
        {
            rightHandBone.position = rightHandTarget.position;
            rightHandBone.rotation = rightHandTarget.rotation;
        }

        if (hipsTarget != null && hipsBone != null)
        {
            // hipsBone.position = hipsTarget.position + hipsOffset;

            // // ✅ 默认直接同步 rotation，如果需要只跟随 Y 轴，可在此扩展

            // Vector3 forward = hipsTarget.forward;
            // forward.y = 0;          // 在 Unity 中，Vector3 是一个值类型（struct），不是引用类型（class），所以直接修改 forward 的 y 分量不会影响 hipsTarget 的 forward 向量
            // if (forward.sqrMagnitude > 0.001f)
            // {
            //     hipsBone.rotation = Quaternion.LookRotation(forward);
            // }


            // // hipsBone.rotation = hipsTarget.rotation;

            // ✅ 平滑移动 hips
            Vector3 targetPos = hipsTarget.position + hipsOffset;
            Vector3 hipsPos = Vector3.Lerp(hipsBone.position, targetPos, Time.deltaTime * hipsFollowSpeed);
            hipsBone.position = hipsPos;


            // ✅ 平滑旋转（只绕 Y 轴）
            float targetY = hipsTarget.rotation.eulerAngles.y;
            float currentY = hipsBone.rotation.eulerAngles.y;
            float smoothY = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * hipsFollowSpeed * 0.1f);
            hipsBone.rotation = Quaternion.Euler(0f, smoothY, 0f);

            
            if (hipsVisualTarget != null){
                hipsVisualTarget.position = hipsPos;
                hipsVisualTarget.rotation = hipsBone.rotation;
            }

        }
    }
}
