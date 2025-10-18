using UnityEngine;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

[System.Serializable]
public class Node
{
    public int idx;
    public float[] center;
    public string label;
    public string gt_label;
}

[System.Serializable]
public class Edge
{
    public int edge_idx;
    public int src;
    public int tgt;
    public float[] src_center;
    public float[] tgt_center;
    public string edge_label;
    public string gt_edge_label;
}

[System.Serializable]
public class ResultResponse
{
    public string scene_id;
    public string result;
    public List<Node> nodes;
    public List<Edge> edges;
    public float CalibrationTime;
    public float InferTime;
    public string ServerTime; // 注意：ServerTime 是字符串类型的 UTC 时间戳
}

[System.Serializable]
public class FetchTimeResponse
{
    public float FetchTime;
}

public class DataReceiver : MonoBehaviour
{
    private HttpListener listener;
    public bool isRunning = true;
    public int port = 8000; // Unity 监听端口
    private Queue<ResultResponse> pendingResults = new Queue<ResultResponse>();
    public GameObject resultsVisualizer;
    private ResultsVisualizer visualizer;

    private TimeLogManager logger;

    void Start()
    {
        visualizer = resultsVisualizer.GetComponent<ResultsVisualizer>();
        logger = FindObjectOfType<TimeLogManager>();

        Thread serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void StartServer()
    {
        listener = new HttpListener();
        //listener.Prefixes.Add($"http://*:8000/receive/");
        listener.Prefixes.Add($"http://*:8000/");  // 注意这里结尾写成 / 才能匹配多个路径
        listener.Start();
        Debug.Log("✅ Unity 服务器已启动，等待 Flask 服务器推送数据...");

        while (isRunning)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                string requestPath = request.Url.AbsolutePath;

                using (System.IO.Stream body = request.InputStream)
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, Encoding.UTF8))
                {
                    string jsonResponse = reader.ReadToEnd();
                    Debug.Log($"📩 收到服务器推送: {jsonResponse}");

                    if (requestPath == "/receive/")
                    {
                        ResultResponse resultResponse = JsonUtility.FromJson<ResultResponse>(jsonResponse);

                        if (logger != null)
                        {
                            logger.UpdateTime("InferTime", resultResponse.InferTime);
                            logger.UpdateTime("CalibrationTime", resultResponse.CalibrationTime);
                        }

                        lock (pendingResults)
                        {
                            pendingResults.Enqueue(resultResponse);
                        }
                    }
                    else if (requestPath == "/fetchtime/")
                    {
                        FetchTimeResponse comm = JsonUtility.FromJson<FetchTimeResponse>(jsonResponse);
                        if (logger != null)
                        {
                            logger.UpdateTime("CommFetchTime", comm.FetchTime);
                            Debug.Log($"✅ CommFetchTime 已记录: {comm.FetchTime:F2} ms");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 未识别的路径: {requestPath}");
                    }
                }

                HttpListenerResponse response = context.Response;
                string responseString = "{\"status\": \"received\"}";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Unity 服务器错误: {e.Message}");
            }
        }
    }

    void Update()
    {
        if (pendingResults.Count > 0)
        {
            float start = Time.realtimeSinceStartup;                                    // 计时 =========
            lock (pendingResults)
            {
                while (pendingResults.Count > 0)
                {
                    ResultResponse resultResponse = pendingResults.Dequeue();
                    if (visualizer != null)
                    {
                        visualizer.UpdateVisualization(resultResponse);
                        //TimeLogManager.MeasureAndLog("GraphTime", () => {
                        //    visualizer.UpdateVisualization(resultResponse);
                        //});
                    }
                }
            }
            float duration = (Time.realtimeSinceStartup - start) * 1000f;               // 计时 =========
            FindObjectOfType<TimeLogManager>()?.UpdateTime("GraphTime", duration);      // 计时 =========
            // 有些时候会因为 InferTime, CalibrationTime 的更新异步，会触发两次 json 从 server 的回传，这样可能会连续更新两次标签
            // 不累加计算 graph 标签时间了，而是单次覆盖更新
 
            logger.couldSave = true;
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        listener.Stop();
    }
}
