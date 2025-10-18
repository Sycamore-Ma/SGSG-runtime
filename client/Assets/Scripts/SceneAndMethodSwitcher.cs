using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;
using System.IO;

public class SceneAndMethodSwitcher : MonoBehaviour
{
    public GameObject modelLoader;  // 绑定 modelLoader 物体
    public GameObject eventMonitor; // 绑定 EventMonitor 物体

    [Header("场景根目录路径")]
    public string rootPath = "I:/3RScan/data/3RScan/";

    // 一开始是 LoaOBJ 外部显式赋值的
    public List<string> sceneIDsList;   // 可用的 Scene ID 列表
    public int currentSceneIndex = -1;  // 当前 Scene ID 索引
    public string currentSceneID = "-1-tutor";

    // ⭐️ 新增：一开始直接跑 VRSG，开始的时候默认固定到 currentMethodIndex 为 0，但是不记录。跑一次结果不显示。
    public List<string> methodNameList = new List<string> { "VRSG", "3DSSG", "SGFN", "JointSSG" }; 
    public int currentMethodIndex = 0;
    private bool hasRunStartupMethod = false;  // ⭐️ 新增：当前 scene 是否已经跑过固定起始方法

    public bool currentIsStartupMethod = true;

    public string currentMethodName = "VRSG";
    private string startupMethodName = "VRSG"; // ⭐️ 新增：用于新场景第一轮的指定方法
    public List<string> usedMethodsInCurrentScene = new List<string>();         // 【新增】追踪当前 scene 中已使用的方法

    public enum SwitchMode { Test, UserScore, UserCorrect }
    public SwitchMode switchMode = SwitchMode.Test;

    private LoadOBJ loadOBJ;  // LoadOBJ 组件
    private LoadPLY loadPLY;  // LoadPLY 组件
    private XRInputHandler xrInput; // XRInputHandler 组件

    private bool canSwitch = true; // 互斥锁，防止连续切换
    private float switchCooldown = 2.0f; // 冷却时间（秒）

    private ResultsVisualizer resultsVisualizer;
    private StrokesDrawer strokesDrawer;        // strokesDrawer.shouldKeepStrokesUI
    private TimeLogManager logger;

    void Start()
    {
        if (modelLoader != null)
        {
            loadOBJ = modelLoader.GetComponent<LoadOBJ>();
            loadPLY = modelLoader.GetComponent<LoadPLY>();
        }

        if (eventMonitor != null)
        {
            xrInput = eventMonitor.GetComponent<XRInputHandler>();
        }

        // 确保 Scene ID 有效
        if (sceneIDsList == null || sceneIDsList.Count == 0)
        {
            //Debug.LogError("SceneSwitcher: sceneIDsList 为空，请在 Inspector 中设置！");
            initsceneIDsList();
        }

        resultsVisualizer = FindObjectOfType<ResultsVisualizer>();
        strokesDrawer = FindObjectOfType<StrokesDrawer>();
        logger = FindObjectOfType<TimeLogManager>();

        resultsVisualizer.visible = true;
        resultsVisualizer.visContentIsBuffer = false;
        resultsVisualizer.blinkable = true;
    }

    void Update()
    {
        if (xrInput == null || sceneIDsList.Count == 0)
            return;

        // 当 leftTriggerPressed 被按下时，只有在 canSwitch 允许的情况下才切换场景
        if (xrInput.Y_buttonPressed && canSwitch)
        {
            switch (switchMode)
            {
                case SwitchMode.Test:
                case SwitchMode.UserCorrect:
                    updateSceneID();
                    break;
                case SwitchMode.UserScore:
                    updateMethodName();
                    break;
                default:
                    Debug.LogWarning("未知的切换模式");
                    break;
            }
            SwitchToNext();
            StartCoroutine(SwitchCooldown()); // 开启冷却协程
        }
    }

    void updateSceneID()
    {
        // 更新 Scene ID
        currentSceneIndex = (currentSceneIndex + 1) % sceneIDsList.Count;
        currentSceneID = sceneIDsList[currentSceneIndex];

        usedMethodsInCurrentScene.Clear();
        hasRunStartupMethod = false; // ⭐️ 新场景，尚未跑 startup 方法

        resultsVisualizer.ClearNodes();
        resultsVisualizer.ClearEdges();
        strokesDrawer.shouldKeepStrokesUI = false;
    }

    void updateMethodName()
    {
        resultsVisualizer.visible = true;
        resultsVisualizer.visContentIsBuffer = false;
        resultsVisualizer.blinkable = true;

        if (currentSceneID == "-1-tutor")
        {
            updateSceneID(); // 切换场景
            Debug.Log("结束训练场景，切换到新场景: " + currentSceneID);
            resultsVisualizer.visible = false;
        }
        // 如果当前 scene 中所有方法都用完了，就切换到下一个 scene
        else if (usedMethodsInCurrentScene.Count >= methodNameList.Count + 1)  // ⭐️ +1 是因为 startupMethod 也算一次
        {
            updateSceneID(); // 切换场景
            Debug.Log("所有方法都用过，切换到新场景: " + currentSceneID);
            resultsVisualizer.visible = false;
        }

        // ⭐️ 新场景第一次强制用 startupMethod
        if (!hasRunStartupMethod)
        {
            resultsVisualizer.visible = false;

            currentMethodName = startupMethodName;
            currentMethodIndex = methodNameList.IndexOf(startupMethodName);
            usedMethodsInCurrentScene.Add("__startup__"); // 特别标记不与 methodName 冲突
            hasRunStartupMethod = true;
            currentIsStartupMethod = true;
            return;
        }

        currentIsStartupMethod = false;

        // 从剩余未使用的方法中随机选择一个
        resultsVisualizer.visible = true;
        List<string> remainingMethods = new List<string>();
        foreach (string method in methodNameList)
        {
            if (!usedMethodsInCurrentScene.Contains(method))
            {
                remainingMethods.Add(method);
            }
        }

        if (remainingMethods.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, remainingMethods.Count);
            currentMethodName = remainingMethods[randomIndex];
            currentMethodIndex = methodNameList.IndexOf(currentMethodName); // 更新索引
            usedMethodsInCurrentScene.Add(currentMethodName); // 标记为已使用
            if (currentMethodName == "VRSG")
            { 
                resultsVisualizer.visContentIsBuffer = true;
            }
        }
        else
        {
            Debug.LogWarning("未找到可用方法，但按理说不应出现这个情况。");
        }
    }

    void SwitchToNext()
    {
        // 先上 log 后重置
        string newRoundID = DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        logger.NewLog(newRoundID, currentSceneID, currentMethodName, false);    // keepLoadTime=false


        Debug.Log("切换到 Scene ID: " + currentSceneID + ", 当前方法: " + currentMethodName);

        // 更新 LoadOBJ 和 LoadPLY 的 SceneID
        if (loadOBJ != null && loadPLY != null)
        {
            loadOBJ.sceneID = currentSceneID;
            loadOBJ.methodName = currentMethodName;
            loadOBJ.Reset();
            loadPLY.sceneID = currentSceneID;
            loadPLY.Reset();
        }

        if (switchMode == SwitchMode.UserScore)
        {
            strokesDrawer.shouldKeepStrokesUI = true;       // 换场景 ID 为 false，else 重新置回
        }
    }

    // 协程：设置冷却时间
    private IEnumerator SwitchCooldown()
    {
        canSwitch = false;  // 进入冷却状态
        yield return new WaitForSeconds(switchCooldown); // 等待冷却时间
        canSwitch = true;   // 重新允许切换
    }

    void initsceneIDsList()
    {
        sceneIDsList = new List<string>();

        if (!Directory.Exists(rootPath))
        {
            Debug.LogError($"❌ 路径不存在: {rootPath}");
            return;
        }

        // 只获取第一层子目录
        string[] subDirs = Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly);

        foreach (string dir in subDirs)
        {
            string folderName = Path.GetFileName(dir);
            if (!string.IsNullOrEmpty(folderName))
            {
                sceneIDsList.Add(folderName);
            }
        }

        Debug.Log($"✅ 已加载 {sceneIDsList.Count} 个场景文件夹名：");
        Debug.Log(string.Join(", ", sceneIDsList));

    }
}
