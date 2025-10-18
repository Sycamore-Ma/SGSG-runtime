using UnityEngine;
using Microsoft.MixedReality.Toolkit.Diagnostics; // 如果需要引入 Diagnostics 的命名空间

public class ProfilerController : MonoBehaviour
{
    private void Start()
    {
        // 找到场景中现有的 MixedRealityToolkitVisualProfiler
        var profiler = FindObjectOfType<MixedRealityToolkitVisualProfiler>();

        if (profiler != null)
        {
            // 将其设为不可见
            profiler.IsVisible = false;
            Debug.LogWarning("MixedRealityToolkitVisualProfiler 已经设置为不可见！");
        }
        else
        {
            Debug.LogWarning("MixedRealityToolkitVisualProfiler 未在场景中找到。");
        }
    }
}
