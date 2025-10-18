using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class UserCorrect : MonoBehaviour
{
    public bool save = true;

    public GameObject eventMonitor;
    private XRInputHandler xrInput;
    public GameObject rightControllerAsCanvasParent;
    private Transform rightPokeInteractor;

    private SceneAndMethodSwitcher sceneAndMethodSwitcher;
    // 这两行定义了 只读属性，每次访问 currentMethodName 时，其实就是返回 sceneAndMethodSwitcher.currentMethodName 的值。
    //private string currentMethodName => sceneAndMethodSwitcher.currentMethodName;
    //private string currentSceneID => sceneAndMethodSwitcher.currentSceneID;
    public string runMethodName = "VRSG";
    public string controlGoupeName = "EG";      // VRSG: EG; SGFN: CG1; VRSG wo: CG2
    public string currentMethodName;        // todo: 只保留一个
    public string currentSceneID;
    public bool currentIsStartupMethod;

    // ✅ Metrics 数据
    public float cameraDistance;
    public float controllerDistance;
    public int clickNum;
    public float taskTime;
    public float nodeErrorRateAfterCorrect;
    public float edgeErrorRateAfterCorrect;
    public float totalErrorRateAfterCorrect;
    public string metricCsvPath = "./Assets/Python/UserCorrect_metric.csv";


    // ✅ Taskload 四维
    private string[] scoreNames = { "MD", "PD", "E", "F" };
    private string[] fullNames = { "Mental Demand", "Physical Demand", "Effort", "Frustration" };
    public int[] scoreValues = new int[4];       // 对应四维的值
    public int scoreIndex = 0;
    private int minScore = 0;
    private int maxScore = 10;
    public string taskloadCsvPath = "./Assets/Python/UserCorrect_taskload.csv";

    private bool canAdjust = true;
    public bool couldSave = true; // 防止重复保存
    private float adjustCooldown = 0.2f;    // 控制调整节奏
    private float saveCooldown = 0.6f;      // 控制保存节奏

    private Text scoreText;
    private GameObject canvasObj;

    private float lastInteractTime = 0f;
    private float fadeDelay = 1.4f;
    private float fadeDuration = 1.6f;


    private Vector3 lastCameraPos;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    private float startTime;
    public int aPressCount = 0;
    public int joystickCount = 0;
    public int strokeCount = 0;
    private StrokesDrawer drawer;
    private ResultsVisualizer visualizer;

    public bool trajectoryVisible = true;
    public bool strokePosVisible = true;
    public bool checkPosVisible = true;
    [Range(1, 100)]
    public int tailFadeLength = 40;
    public float trajectoryWidth = 0.01f; // 轨迹宽度
    public float trajectoryFadeAlpha = 0.1f; // 轨迹渐变透明度
    private List<Vector3> cameraTrajectory = new List<Vector3>();
    private List<Vector3> cameraCheckPos = new List<Vector3>();
    private List<Vector3> cameraStrokePos = new List<Vector3>();
    private float trajectorySampleInterval = 0.1f;
    private float trajectoryTimer = 0f;
    public GameObject trajectoryRendererObj;
    private LineRenderer trajectoryRenderer; 
    private int lastStrokeCount = 0;        // 用于记录笔触数量发生变化的时候，记录 strokePos 到轨迹中
    
    // 用于可视化轨迹上的交互点
    public GameObject checkPointPrefab;   // 红球
    public GameObject strokePointPrefab;  // 绿球
    private List<GameObject> checkSpheres = new List<GameObject>();
    private List<GameObject> strokeSpheres = new List<GameObject>();

    [Serializable]
    public class TrajectoryData
    {
        public string timestamp;
        public string methodName;
        public string sceneID;
        public List<Vector3> trajectory = new List<Vector3>();
        public List<Vector3> checkPos = new List<Vector3>();
        public List<Vector3> strokePos = new List<Vector3>();
    }

    [System.Serializable]
    public class TrajectoryDataListWrapper
    {
        public List<TrajectoryData> dataList = new List<TrajectoryData>();
    }

    void Start()
    {
        xrInput = eventMonitor.GetComponent<XRInputHandler>();
        sceneAndMethodSwitcher = FindObjectOfType<SceneAndMethodSwitcher>();
        CreateUI();

        lastCameraPos = Camera.main.transform.position;
        lastLeftHandPos = xrInput.leftHandPos;
        lastRightHandPos = xrInput.rightHandPos;
        startTime = Time.time;
        drawer = FindObjectOfType<StrokesDrawer>();
        visualizer = FindObjectOfType<ResultsVisualizer>();

        drawer.enableDrawing = false;
        if (controlGoupeName == "EG")
        {
            drawer.enableDrawing = true;
            runMethodName = "VRSG";
        }
        else if (controlGoupeName == "CG1")
        {
            runMethodName = "3DSSG";
        }
        else if (controlGoupeName == "CG2")
        {
            runMethodName = "SGFN";
        }
        else if (controlGoupeName == "CG3")
        {
            runMethodName = "JointSSG";
        }
        else if (controlGoupeName == "CG4")
        {
            runMethodName = "VRSG";
        }


        //sceneAndMethodSwitcher.switchMode = sceneAndMethodSwitcher.SwitchMode.UserCorrect;      // 枚举得通过类名调用
        sceneAndMethodSwitcher.switchMode = SceneAndMethodSwitcher.SwitchMode.UserCorrect;
        sceneAndMethodSwitcher.currentMethodName = runMethodName;

        // 获取右手 Poke Interactor
        Transform poke = rightControllerAsCanvasParent.transform.Find("Poke Interactor");
        if (poke != null)
        {
            rightPokeInteractor = poke;
        }
        else
        {
            Debug.LogWarning("❗️未找到 RightController 下的 Poke Interactor");
        }

        // 轨迹渲染器初始化
        // // trajectoryRendererObj = new GameObject("CameraTrajectory");
        // // trajectoryRenderer = trajectoryRendererObj.AddComponent<LineRenderer>();
        // // trajectoryRenderer.startWidth = 0.01f;
        // // trajectoryRenderer.endWidth = 0.01f;
        // // trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
        // ✅ 实例化这个 prefab
        GameObject obj = Instantiate(trajectoryRendererObj);
        obj.name = "CameraTrajectory";
        trajectoryRenderer = obj.GetComponent<LineRenderer>();
        trajectoryRenderer.positionCount = 0;

        trajectoryRenderer.startWidth = trajectoryWidth;
        trajectoryRenderer.endWidth = trajectoryWidth;


        ResetMetric();      // 先创建，再用，要不然 null 错误，因为没有 trajectoryRenderer
        ResetTaskload();
    }

    void Update()
    {
        if (xrInput == null) return;
        float axis = xrInput.rightJoystickValue.y;

        //if ((currentSceneID == "last-one" || currentSceneID == "-1-tutor") && Mathf.Abs(axis) > 0.8f && canAdjust && scoreIndex < 4)
        //if (currentSceneID == "last-one" && Mathf.Abs(axis) > 0.8f && canAdjust && scoreIndex < 4)
        if (Mathf.Abs(axis) > 0.8f && canAdjust && scoreIndex < 4)
        {
            canAdjust = false;
            if (currentSceneID == "last-one") 
            {             
                lastInteractTime = Time.time; // ⭐️ 重置透明度计时
                if (axis > 0.8f)
                {
                    scoreValues[scoreIndex] = Mathf.Min(scoreValues[scoreIndex] + 1, maxScore);
                    UpdateScoreText();
                }
                else if (axis < -0.8f)
                {
                    scoreValues[scoreIndex] = Mathf.Max(scoreValues[scoreIndex] - 1, minScore);
                    UpdateScoreText();
                }
                ResetAlpha();
            }
            else
            {
                joystickCount++;
                if (axis > 0.8f)
                {
                    visualizer.activeLabelSelectorToPrev();
                }
                else if (axis < -0.8f)
                {
                    visualizer.activeLabelSelectorToNext();
                }

                Vector3 pos = rightControllerAsCanvasParent.transform.position;
                
                cameraCheckPos.Add(pos);
                if (checkPosVisible)
                {
                    var sphere = Instantiate(checkPointPrefab, pos, Quaternion.identity);
                    checkSpheres.Add(sphere);
                }
            }
            StartCoroutine(AdjustCooldown());
        }

        //if ((currentSceneID == "last-one" || currentSceneID == "-1-tutor") && xrInput.X_buttonPressed && canAdjust && couldSave)
        if (currentSceneID == "last-one" && xrInput.X_buttonPressed && canAdjust && couldSave)
        {
            canAdjust = false;
            couldSave = false;
            lastInteractTime = Time.time; // ⭐️ 重置透明度计时
            
            scoreIndex++;
            
            UpdateScoreText();
            
            if (scoreIndex == 4)
            {
                SaveTaskloadScores();
                ResetTaskload();
            }
            StartCoroutine(AdjustCooldown());
            StartCoroutine(SaveCooldown());
        }

        if (currentSceneID == "last-one")
        {
            var yAction = xrInput.inputActions
                .FindActionMap("XRI Left Interaction")
                .FindAction("Y");

            if (yAction != null)
            {
                yAction.Disable();  // ⛔ 禁用 Y 键监听
                Debug.Log("🛑 已禁用 Y 按钮");
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到 Y 按钮 Action");
            }
        }


        if (xrInput.A_buttonPressed && canAdjust)
        {
            canAdjust = false;
            aPressCount++;
            // ✅ 调用 check() 方法
            if (rightPokeInteractor != null && visualizer != null)
            {
                //Vector3 pos = new Vector3(Camera.main.transform.position);
                Vector3 pos = rightControllerAsCanvasParent.transform.position;
                //pos += rightPokeInteractor.position;
                //pos /= 2;

                visualizer.check(rightPokeInteractor.position);
                cameraCheckPos.Add(pos);
                if (checkPosVisible)
                {
                    var sphere = Instantiate(checkPointPrefab, pos, Quaternion.identity);
                    checkSpheres.Add(sphere);
                }
            }
            StartCoroutine(AdjustCooldown());
        }

        // 记录 Stroke 点（B、RightTrigger、RightGrab）
        if (strokeCount > lastStrokeCount)
        {
            Vector3 pos = rightControllerAsCanvasParent.transform.position;
            cameraStrokePos.Add(pos);
            lastStrokeCount = strokeCount;
            if (strokePosVisible)
            {
                var sphere = Instantiate(strokePointPrefab, pos, Quaternion.identity);
                strokeSpheres.Add(sphere);
            }
        }

        if (xrInput.Y_buttonPressed && couldSave)
        {
            couldSave = false;
            SaveMetricData();
            ResetMetric();
            StartCoroutine(SaveCooldown());
        }
        else
        {   // 防止与 SceneAndMethodSwitcher 脚本产生时序问题。Unity 默认脚本执行顺序是不确定的
            currentMethodName = sceneAndMethodSwitcher.currentMethodName;
            currentSceneID = sceneAndMethodSwitcher.currentSceneID;
            currentIsStartupMethod = sceneAndMethodSwitcher.currentIsStartupMethod;
            UpdateMetrics();
        }

        TextFading();
    }

    public void UpdateMetrics()
    {
        // 1. 主相机轨迹长度
        Vector3 currentCamPos = Camera.main.transform.position;
        cameraDistance += Vector3.Distance(lastCameraPos, currentCamPos);
        lastCameraPos = currentCamPos;

        // 2. 控制器累计位移
        controllerDistance += Vector3.Distance(lastLeftHandPos, xrInput.leftHandPos);
        controllerDistance += Vector3.Distance(lastRightHandPos, xrInput.rightHandPos);
        lastLeftHandPos = xrInput.leftHandPos;
        lastRightHandPos = xrInput.rightHandPos;

        // 3. 点击次数
        strokeCount = drawer != null ? drawer.GetStrokeCount() : 0;
        clickNum = aPressCount + joystickCount + strokeCount;

        // 4. 任务时长
        taskTime = Time.time - startTime;

        // 5. 错误率
        if (visualizer != null)
        {
            nodeErrorRateAfterCorrect = visualizer.GetNodeErrorRateAfterCorrection(); 
            edgeErrorRateAfterCorrect = visualizer.GetEdgeErrorRateAfterCorrection(); 
            totalErrorRateAfterCorrect = visualizer.GetTotalErrorRateAfterCorrection(); 
        }

        // ⭐️ 相机轨迹记录与可视化
        trajectoryTimer += Time.deltaTime;
        if (trajectoryTimer >= trajectorySampleInterval)
        {
            Vector3 currentPos = Camera.main.transform.position;
            cameraTrajectory.Add(currentPos);
            int count = cameraTrajectory.Count;

            trajectoryRenderer.positionCount = count;
            trajectoryRenderer.SetPosition(count - 1, currentPos);
            trajectoryRendererObj.SetActive(trajectoryVisible);
            trajectoryTimer = 0f;

            // ✅ 渐变透明度设置（最多 8 个 Alpha Key）
            if (trajectoryVisible && count >= 2)
            {
                Gradient gradient = new Gradient();

                Color baseColor = trajectoryRenderer.material.HasProperty("_Color")
                    ? trajectoryRenderer.material.color
                    : Color.magenta;

                GradientColorKey[] colorKeys = new GradientColorKey[2]
                {
                    new GradientColorKey(baseColor, 0f),
                    new GradientColorKey(baseColor, 1f)
                };

                List<GradientAlphaKey> alphaKeyList = new List<GradientAlphaKey>();

                float fadeStartTime = Mathf.Clamp01((float)(count - tailFadeLength) / (count - 1));

                alphaKeyList.Add(new GradientAlphaKey(trajectoryFadeAlpha, 0f));               // 所有之前都是 0.1
                alphaKeyList.Add(new GradientAlphaKey(0.1f, fadeStartTime));    // 渐变起点
                alphaKeyList.Add(new GradientAlphaKey(1.0f, 1f));               // 渐变终点

                gradient.SetKeys(colorKeys, alphaKeyList.ToArray());
                trajectoryRenderer.colorGradient = gradient;
            }

        }

    }

    void SaveMetricData()
    {
        if (!save)
        {
            return;
        }

        if (currentSceneID == "-1-tutor" || currentSceneID == "last-one")
        {
            Debug.Log($"训练场景，无需存储");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string[] headers = { "Timestamp", "MethodName", "SceneID", "CameraDistance", "ControllerDistance", "ClickNum", "TaskTime", 
            "NodeErrorRateAfterCorrect", "EdgeErrorRateAfterCorrect", "TotalErrorRateAfterCorrect" };
        string[] values = {
            timestamp,
            controlGoupeName,
            currentSceneID,
            cameraDistance.ToString("F3"),
            controllerDistance.ToString("F3"),
            clickNum.ToString(),
            taskTime.ToString("F2"),
            nodeErrorRateAfterCorrect.ToString("F2"),
            edgeErrorRateAfterCorrect.ToString("F2"),
            totalErrorRateAfterCorrect.ToString("F2")
        };

        bool exists = System.IO.File.Exists(metricCsvPath);
        using (var writer = new System.IO.StreamWriter(metricCsvPath, true))
        {
            if (!exists) writer.WriteLine(string.Join(",", headers));
            writer.WriteLine(string.Join(",", values));
        }

        SaveTrajectory();

        Debug.Log("✅ Metric 已保存");
    }

    void SaveTaskloadScores()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string[] headers = { "Timestamp", "MethodName", "MD", "PD", "E", "F", "UnweightedTLX"};
        int md = scoreValues[0];
        int pd = scoreValues[1];
        int e = scoreValues[2];
        int f = scoreValues[3];
        float unweightedTLX = (float)(md + pd + e + f) / 4.0f;
        string[] values = {
            timestamp,
            controlGoupeName,
            md.ToString(),
            pd.ToString(),
            e.ToString(),
            f.ToString(),
            unweightedTLX.ToString("F2")  // 保留 2 位小数
        };

        bool exists = System.IO.File.Exists(taskloadCsvPath);
        using (var writer = new System.IO.StreamWriter(taskloadCsvPath, true))
        {
            if (!exists) writer.WriteLine(string.Join(",", headers));
            writer.WriteLine(string.Join(",", values));
        }

        Debug.Log($"✅ Taskload 四维评分和 UnweightedTLX（{unweightedTLX:F2}）已保存");
    }

    void SaveTrajectory()
    {
        string filePath = $"./Assets/Python/Trajectory/Trajectory_{currentSceneID}_{controlGoupeName}.json";
        
        // ✅ 创建当前数据
        TrajectoryData newData = new TrajectoryData
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            methodName = currentMethodName,
            sceneID = currentSceneID,
            trajectory = new List<Vector3>(cameraTrajectory),
            checkPos = new List<Vector3>(cameraCheckPos),
            strokePos = new List<Vector3>(cameraStrokePos)
        };

        // ✅ 尝试读取原始 JSON（如果有）
        TrajectoryDataListWrapper wrapper = new TrajectoryDataListWrapper();
        if (File.Exists(filePath))
        {
            string existingJson = File.ReadAllText(filePath);
            wrapper = JsonUtility.FromJson<TrajectoryDataListWrapper>(existingJson);
            if (wrapper == null || wrapper.dataList == null) wrapper = new TrajectoryDataListWrapper();
        }

        // ✅ 添加新数据
        wrapper.dataList.Add(newData);

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"📍 Appended trajectory to {filePath}");
    }


    void ResetMetric()
    {
        cameraDistance = 0f;
        controllerDistance = 0f;
        clickNum = 0;
        taskTime = 0f;
        nodeErrorRateAfterCorrect = 0f;
        edgeErrorRateAfterCorrect = 0f;
        totalErrorRateAfterCorrect = 0f;

        startTime = Time.time;
        aPressCount = 0;
        joystickCount = 0;
        strokeCount = 0;

        cameraTrajectory.Clear();
        cameraCheckPos.Clear();
        cameraStrokePos.Clear();
        trajectoryRenderer.positionCount = 0;
        lastStrokeCount = 0;

        ClearSpheres();
    }

    void ResetTaskload()
    {
        for (int i = 0; i < scoreValues.Length; i++) scoreValues[i] = -1;
        //scoreIndex = 0;
        UpdateScoreText();
    }

    void ClearSpheres()
    {
        foreach (var obj in checkSpheres) Destroy(obj);
        foreach (var obj in strokeSpheres) Destroy(obj);
        checkSpheres.Clear();
        strokeSpheres.Clear();
    }

    IEnumerator AdjustCooldown()
    {
        yield return new WaitForSeconds(adjustCooldown);
        canAdjust = true;
    }

    IEnumerator SaveCooldown()
    {
        yield return new WaitForSeconds(saveCooldown);
        couldSave = true;
    }

    void CreateUI()
    {
        Camera mainCam = Camera.main;
        canvasObj = new GameObject("CorrectCanvas");
        canvasObj.transform.SetParent(rightControllerAsCanvasParent.transform);
        canvasObj.transform.localPosition = new Vector3(0f, 0.2f, 0.1f);
        canvasObj.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        canvasObj.transform.localScale = Vector3.one * 1e-5f;

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCam;
        canvasObj.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("CorrectScoreText");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = new Vector3(300f, 300f, 300f);

        scoreText = textObj.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        scoreText.fontStyle = FontStyle.Italic;
        scoreText.fontSize = 20;
        scoreText.alignment = TextAnchor.MiddleLeft;
        scoreText.color = Color.white;
        scoreText.text = "";

        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 100);
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText == null) return;

        // 如果 index 超出有效范围，显示结束语
        if (scoreIndex >= scoreValues.Length || scoreIndex >= scoreNames.Length || scoreIndex >= 4)
        {
            scoreText.text = "Thanks\nYou've completed all scores!";
            return;
        }

        int tempScore = scoreValues[scoreIndex];
        string explain = "";
        if (tempScore <= 2)
            explain = "Very Easy";
        else if (tempScore <= 5)
            explain = "Easy";
        else if (tempScore <= 7)
            explain = "Medium";
        else
            explain = "Hard";

        string fullName = fullNames[scoreIndex];

        scoreText.text = $"[XR Scroller] Scoring: {scoreNames[scoreIndex]}\n{fullName}\nTask Load: {tempScore}\t{explain}";
        TextPseudoColor();
    }

    void ResetAlpha()
    {
        if (scoreText == null) return;

        Color c = scoreText.color;
        c.a = 1f;
        scoreText.color = c;
    }

    void TextPseudoColor()
    {
        if (scoreText == null) return;

        // 获取当前分数（0~10）
        int score = Mathf.Clamp(scoreValues[scoreIndex], 0, 10);
        float t = score / 10f;  // 归一化到 0~1

        // 分两段，避免颜色脏
        if (t < 0.5f)
        {
            // 0 ~ 5 分：绿色 -> 黄色
            scoreText.color = Color.Lerp(Color.green, Color.yellow, t * 2f);  // 放大到 0~1
        }
        else
        {
            // 5 ~ 10 分：黄色 -> 红色
            scoreText.color = Color.Lerp(Color.yellow, Color.red, (t - 0.5f) * 2f);
        }
    }


    void TextFading()
    {
        if (scoreText == null) return;

        if (scoreIndex >= scoreValues.Length || scoreIndex >= scoreNames.Length || scoreIndex >= 4)
        {
            Color c = scoreText.color;
            c.a = 1.0f;
            return;
        }

        if (scoreIndex >= 4) fadeDelay = 1.5f;

        float timeSinceLast = Time.time - lastInteractTime;
        if (timeSinceLast > fadeDelay)
        {
            float t = Mathf.Clamp01((timeSinceLast - fadeDelay) / fadeDuration);
            Color c = scoreText.color;
            c.a = Mathf.Lerp(1f, 0.0f, t);
            scoreText.color = c;
        }
    }
}
