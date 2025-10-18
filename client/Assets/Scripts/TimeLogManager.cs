using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

public class TimeLogManager : MonoBehaviour
{
    // Public CSV path
    //public string csvPath = "../Python/TimeLog.csv";
    public string csvPath = "./Assets/Python/TimeLog.csv";

    // Private internal data
    public string roundID;
    public string sceneID = "-1-tutor";

    public string methodName;

    public float loadTime = -1f;
    public float networkInitTime = -1f;
    public float commPushTime = -1f;
    public float commFetchTime = -1f;
    public float inferTime = -1f;
    public float calibrationTime = -1f;
    public float graphTime = -1f;
    public float interactionTime = -1f;

    public string timestamp = "";
    public bool couldSave = false; // 防止重复保存

    // 用于静态句柄调用
    private static TimeLogManager cachedLogger;

    // Public method: create new log
    // 在 LoadOBJ Reset() 中被调用，废弃
    // 改为在 SceneAndMethodSwitcher 中调用，这样可以通过其 "-1-tutor" 筛选掉训练场景
    //string newRoundID = DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    //logger.NewLog(newRoundID, sceneID, methodName);

    public void NewLog(string newRoundID, string newSceneID, string newMethodName, bool keepLoadTime)
    {
        // Clear previous values
        roundID = newRoundID;
        sceneID = newSceneID;
        methodName = newMethodName;

        if (!keepLoadTime)
            loadTime = -1f;
    
        networkInitTime = -1f;

        InRoundReset();
    }

    /// <summary>
    /// 同一个 round 下面，对应着一个唯一的 <时间戳、sceneid、methodname>
    /// 有些时候固定 methodname 换 sceneid
    /// 有些时候固定 sceneid 换 methodname，这样就不会重新 loadOBJ（SwitchToNext 里面好像都统一换了 OBJ，不管了）
    /// 有些时候两个都会换
    /// 所以 NewLog() 不能在 LoadOBJ Reset() 中被调用，而需要在 SceneAndMethodSwitcher SwitchToNext() 中调用
    /// InRound，可能会有多次交互或者预测，代表着有多次 push fetch infer calibra graph inter
    /// </summary>
    public void InRoundReset()      
    {
        commPushTime = -1f;
        commFetchTime = -1f;
        inferTime = -1f;
        calibrationTime = -1f;
        graphTime = -1f;
        interactionTime = 0.0f;
        timestamp = "";
        couldSave = false;
    }

    // Public method: update a time field by name
    public void UpdateTime(string name, float time)
    {
        switch (name)
        {
            case "LoadTime":
                loadTime = (loadTime < 0f) ? time : loadTime + time;
                break;
            case "NetworkInitTime":
                networkInitTime = time;
                break;
            case "CommPushTime":
                commPushTime = time;
                break;
            case "CommFetchTime":
                commFetchTime = time;
                break;
            case "InferTime":
                inferTime = time;
                break;
            case "CalibrationTime":
                calibrationTime = time;
                break;
            case "GraphTime":
                //graphTime = (graphTime < 0f) ? time : graphTime + time;
                graphTime = time;
                break;
            case "InteractionTime":
                interactionTime = time;
                break;
            default:
                Debug.LogWarning($"[TimeLogManager] Unknown time field: {name}");
                break;
        }

        // 自动保存逻辑
        //if (couldSave && AllFieldsFilled())

        // DataReceiver 调用 logger.couldSave = true;
        if (couldSave)
        {
            if (AllFieldsFilled())
            {
                Save();
            }
            // 对于首次加载场景，没有笔触交互信息，也希望直接存一次（interactionTime 设为了 0.0f）
            // 最后一步应该是调用 GraphTime，GraphTime 可能被异步更新多次，所以这里有非法 -1 就不存，直接 reset
            //Save();
            
            //InRoundReset();     // 保证只存一次
        }
    }

    // Public method: save all data into CSV
    public void Save()
    {
        if (sceneID == "-1-tutor")
        {
            InRoundReset();
            return;
        }

        //if (PushTime 太大（每个场景或者method的开始）)
        if (commPushTime > 500.0f)
        {
            networkInitTime += commPushTime;
            //commPushTime = -1.0f;
            commPushTime = commFetchTime * 4.0f;    // dummy init
        }

        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        string[] headers = new string[] {
            "RoundID", "SceneID", "MethodName",
            "LoadTime", "NetworkInitTime", "CommPushTime", "CommFetchTime", "InferTime", "CalibrationTime", "GraphTime", "InteractionTime", 
            "TimeStamp"
        };

        string[] values = new string[] {
            roundID,
            sceneID,
            methodName,
            FormatTime(loadTime),
            FormatTime(networkInitTime),
            FormatTime(commPushTime),
            FormatTime(commFetchTime),
            FormatTime(inferTime),
            FormatTime(calibrationTime),
            FormatTime(graphTime),
            FormatTime(interactionTime),
            timestamp,
        };

        bool fileExists = File.Exists(csvPath);
        using (StreamWriter writer = new StreamWriter(csvPath, true))
        {
            if (!fileExists)
            {
                writer.WriteLine(string.Join(",", headers));
            }
            writer.WriteLine(string.Join(",", values));
        }

        Debug.Log($"[TimeLogManager] Log saved: {roundID}, {sceneID}");

        InRoundReset();
    }

    // Helper to format time values
    private string FormatTime(float value)
    {
        return value >= 0f ? value.ToString("F4", CultureInfo.InvariantCulture) : "NA";
    }

    private bool AllFieldsFilled()
    {
        return loadTime >= 0f &&
               networkInitTime >= 0f &&
               commPushTime >= 0f &&
               commFetchTime >= 0f &&
               inferTime >= 0f &&
               calibrationTime >= 0f &&
               graphTime >= 0f &&
               interactionTime >= 0f;
    }

    // 静态方法，用类名调用，不用初始化对象
    // MeasureAndLog 只支持同步方法（Action），不支持 IEnumerator 异步协程
    public static void MeasureAndLog(string label, Action action)
    {
        if (cachedLogger == null)
            cachedLogger = FindObjectOfType<TimeLogManager>();

        float start = Time.realtimeSinceStartup;
        action?.Invoke();
        float end = Time.realtimeSinceStartup;
        float duration = (end - start) * 1000f;
        cachedLogger?.UpdateTime(label, duration);
        Debug.Log($"[TimeLogManager] {label} 执行耗时: {duration:F2} ms");
    }

}
