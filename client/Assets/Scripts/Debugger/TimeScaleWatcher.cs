using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;


public class TimeScaleWatcher : MonoBehaviour
{
    private float lastScale;

    void Start()
    {
        lastScale = Time.timeScale;
    }

    void Update()
    {
        if (Time.timeScale != lastScale)
        {
            Debug.LogWarning($"[TimeScaleWatcher] Time.timeScale 从 {lastScale} 改变为 {Time.timeScale}, 调用栈：\n{System.Environment.StackTrace}");
            lastScale = Time.timeScale;
        }
    }
}
