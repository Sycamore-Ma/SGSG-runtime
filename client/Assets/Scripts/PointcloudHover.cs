using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointcloudHover : MonoBehaviour
{
    public GameObject eventMonitor;     // 绑定 EventMonitor 物体
    public GameObject modelLoader;      // 绑定 modelLoader 物体
    private XRInputHandler xrInput;     // XRInputHandler 组件
    private LoadPLY loadPLY;            // LoadPLY 组件

    // Start is called before the first frame update
    void Start()
    {
        if (eventMonitor != null)
        {
            xrInput = eventMonitor.GetComponent<XRInputHandler>();
        }
        
        if (modelLoader != null)
        {
            loadPLY = modelLoader.GetComponent<LoadPLY>(); // 获取 LoadPLY 组件
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (xrInput == null)
            return;

        //if (xrInput.leftGripPressed)
        //{
        //    loadPLY.alpha = Mathf.Clamp01(xrInput.leftGripValue); // 更新 LoadPLY 里的 alpha
        //}
        //else
        //{
        //    loadPLY.alpha = 0.3f;
        //}

        loadPLY.alpha = Mathf.Clamp01(xrInput.leftTriggerValue); // 更新 LoadPLY 里的 alpha
    }
}
