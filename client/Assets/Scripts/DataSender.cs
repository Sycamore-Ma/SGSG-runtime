using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System.IO;
using System;

public class DataSender : MonoBehaviour
{
    public string serverIP = "192.168.0.187"; // 在 Unity Inspector 里调整
    public int serverPort = 5000;  // 服务器端口

    public GameObject eventMonitor;     // 绑定 EventMonitor 物体
    private XRInputHandler xrInput;     // XRInputHandler 组件

    public string jsonSavingPath = "Assets/Scripts/manual_strokes.json";

    private bool canSend = true; // 发送锁，默认允许发送

    private TimeLogManager logger;

    // Start is called before the first frame update
    void Start()
    {
        if (eventMonitor != null)
        {
            xrInput = eventMonitor.GetComponent<XRInputHandler>();
        }

        logger = FindObjectOfType<TimeLogManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (xrInput != null && xrInput.X_buttonPressed && canSend)
        {
            SendStroke();
        }
    }
    
    IEnumerator LockSending()
    {
        canSend = false;    // 锁定发送
        yield return new WaitForSeconds(2f);    // 等待2秒
        canSend = true;     // 解锁发送
    }

    public void SendSceneID_MethodName(string sceneID, string methodName)               // 只有 LoadOBJ 中调用
    {
        StartCoroutine(SendSceneID_MethodNameData(sceneID, methodName));
    }

    IEnumerator SendSceneID_MethodNameData(string sceneID, string methodName)
    {
        float start = Time.realtimeSinceStartup;                                        // 计时 =========

        string url = $"http://{serverIP}:{serverPort}/id-methodname";
        // 构造 JSON 字符串
        string jsonData = "{\"scene_id\": \"" + sceneID + "\", \"method_name\": \"" + methodName + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Scene ID 和 Method Name 发送成功: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("发送失败: " + request.error);
            }
        }

        float duration = (Time.realtimeSinceStartup - start) * 1000f;                   // 计时 =========
        FindObjectOfType<TimeLogManager>()?.UpdateTime("NetworkInitTime", duration);    // 计时 =========
    }

    public void SendStroke()                // Update() 调用；LoadOBJ 中也会调用，用于新加载场景后传递 -1 笔触用，刷新场景图
    {
        string strokeData = LoadStrokeDataFromFile(); // 读取外部 JSON 文件
        if (!string.IsNullOrEmpty(strokeData))
        {
            StartCoroutine(SendStrokeData(strokeData));
            StartCoroutine(LockSending()); // 启动锁定定时器
        }
    }

    IEnumerator SendStrokeData(string strokeJson)
    {
        float start = Time.realtimeSinceStartup;                                    // 计时 =========

        string url = $"http://{serverIP}:{serverPort}/strokes";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(strokeJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("笔触数据发送成功: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("笔触数据发送失败: " + request.error);
            }
        }

        float duration = (Time.realtimeSinceStartup - start) * 1000f;               // 计时 =========
        FindObjectOfType<TimeLogManager>()?.UpdateTime("CommPushTime", duration);   // 计时 =========
    }

    string LoadStrokeDataFromFile()
    {
        try
        {
            if (File.Exists(jsonSavingPath))
            {
                return File.ReadAllText(jsonSavingPath);
            }
            else
            {
                Debug.LogError("未找到笔触数据文件: " + jsonSavingPath);
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("读取笔触数据失败: " + e.Message);
            return null;
        }
    }

    //// **忽略 SSL 验证**
    //private class BypassCertificate : CertificateHandler
    //{
    //    protected override bool ValidateCertificate(byte[] certificateData) => true;
    //}
}
