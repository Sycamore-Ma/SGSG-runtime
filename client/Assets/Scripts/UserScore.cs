using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class UserScore : MonoBehaviour
{
    public GameObject eventMonitor;         // 绑定 EventMonitor 物体
    private XRInputHandler xrInput;         // XRInputHandler 组件
    public GameObject rightControllerAsCanvasParent;

    private SceneAndMethodSwitcher sceneAndMethodSwitcher;
    public string currentMethodName;
    public string currentSceneID;
    public bool currentIsStartupMethod;

    public int currentScore = 0;            // 初始分数为 0
    private const int minScore = 1;
    private const int maxScore = 5;
    public string csvPath = "./Assets/Python/UserScore.csv";

    private bool canAdjust = true;
    public bool couldSave = true; // 防止重复保存
    private float adjustCooldown = 0.2f;    // 控制调整节奏
    private float saveCooldown = 0.6f;      // 控制保存节奏

    private Text scoreText;                 // ⭐️ UI 文本组件
    private GameObject canvasObj;           // ⭐️ Canvas 根节点

    private float lastInteractTime = 0f;    // 上次交互时间戳
    private float fadeDelay = 0.4f;         // 多久后开始淡出（秒）
    private float fadeDuration = 0.6f;      // 渐隐耗时（秒）

    // Start is called before the first frame update
    void Start()
    {
        if (eventMonitor != null)
        {
            xrInput = eventMonitor.GetComponent<XRInputHandler>();
        }
        CreateScoreUI();                    // ⭐️ 创建一个 World Space Canvas 附着在 Main Camera 上

        sceneAndMethodSwitcher = FindObjectOfType<SceneAndMethodSwitcher>();
        currentMethodName = sceneAndMethodSwitcher.currentMethodName;
        currentSceneID = sceneAndMethodSwitcher.currentSceneID;

        //sceneAndMethodSwitcher.switchMode = sceneAndMethodSwitcher.SwitchMode.UserScore;      // 枚举得通过类名调用
        sceneAndMethodSwitcher.switchMode = SceneAndMethodSwitcher.SwitchMode.UserScore;
    }

    // Update is called once per frame
    void Update()
    {
        if (xrInput == null) return;
        float axis = xrInput.rightJoystickValue.y;

        if (Mathf.Abs(axis) > 0.8f && canAdjust)
        {
            canAdjust = false;
            lastInteractTime = Time.time; // ⭐️ 重置透明度计时
            if (axis > 0.8f)
            {
                IncreaseScore();
            }
            else if (axis < -0.8f)
            {
                DecreaseScore(); 
            }
            ResetAlpha();
            StartCoroutine(AdjustCooldown());
        }

        if (xrInput.Y_buttonPressed && couldSave)
        {
            couldSave = false;
            Save();
            Reset();
            StartCoroutine(SaveCooldown());
        }
        else
        {   // 防止与 SceneAndMethodSwitcher 脚本产生时序问题。Unity 默认脚本执行顺序是不确定的
            currentMethodName = sceneAndMethodSwitcher.currentMethodName;
            currentSceneID = sceneAndMethodSwitcher.currentSceneID;
            currentIsStartupMethod = sceneAndMethodSwitcher.currentIsStartupMethod;
        }

        // ⭐️ 每帧淡出字体
        TextFading();
    }

    public void Save()
    {
        if (currentSceneID == "-1-tutor")
        {
            Debug.Log($"训练场景，无需存储");
            return;
        }

        if (currentIsStartupMethod)
        {
            Debug.Log($"固定起始方法用于绘制笔触，无需存储");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string[] headers = { "Timestamp", "SceneID", "MethodName", "Score" };
        string[] values = {
            timestamp,
            currentSceneID,
            currentMethodName,
            currentScore.ToString()
        };

        bool fileExists = System.IO.File.Exists(csvPath);

        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(csvPath, true))
        {
            if (!fileExists)
            {
                writer.WriteLine(string.Join(",", headers));
            }

            writer.WriteLine(string.Join(",", values));
        }

        Debug.Log($"✅ User score saved: {timestamp}, {currentSceneID}, {currentMethodName}, {currentScore}");
    }


    public void Reset()
    {
        currentScore = 0;
        UpdateScoreText();
    }


    void IncreaseScore()
    {
        if (currentScore < maxScore)
        {
            currentScore++;
            UpdateScoreText();
        }
    }

    void DecreaseScore()
    {
        if (currentScore > minScore)
        {
            currentScore--;
            UpdateScoreText();
        }
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

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"[XR Scroller] inputting...\nScore: {currentScore}";
        }
        TextPseudoColor(); // ⭐️ 调用更新颜色
    }

    void CreateScoreUI()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // ⭐️ 创建 Canvas
        canvasObj = new GameObject("ScoreCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCam;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        canvasObj.AddComponent<GraphicRaycaster>();
        //canvasObj.transform.SetParent(mainCam.transform);

        //// 设置在右上角
        //canvasObj.transform.localPosition = new Vector3(0.0f, 0.2f, 0.6f);
        //canvasObj.transform.localRotation = Quaternion.identity;
        //canvasObj.transform.localScale = Vector3.one * 1.0e-05f;

        canvasObj.transform.SetParent(rightControllerAsCanvasParent.transform);

        // 设置在右上角
        canvasObj.transform.localPosition = new Vector3(0.0f, 0.1f, 0.1f);
        canvasObj.transform.localRotation = Quaternion.Euler(30.0f, 0.0f, 0.0f);
        canvasObj.transform.localScale = Vector3.one * 1.0e-05f;

        // ⭐️ 创建 Text 对象
        GameObject textObj = new GameObject("ScoreText");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = new Vector3(300f, 300f, 300f);

        scoreText = textObj.AddComponent<Text>();
        scoreText.text = $"[XR Scroller] inputting...\nScore: {currentScore}";
        scoreText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        //scoreText.fontStyle = "Italic";       // 在 Unity 中，fontStyle 是一个 枚举类型，不是字符串类型
        scoreText.fontStyle = FontStyle.Italic;
        scoreText.fontSize = 20;
        //scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.alignment = TextAnchor.MiddleLeft;
        scoreText.color = Color.yellow;

        RectTransform rect = scoreText.GetComponent<RectTransform>();
        //rect.sizeDelta = new Vector2(400, 100);
        rect.sizeDelta = new Vector2(240, 100);
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
        switch (currentScore)
        {
            case 1:
                scoreText.color = new Color(0.9f, 0.1f, 0.2f);   // 深红
                break;
            case 2:
                scoreText.color = new Color(1f, 0.5f, 0f);       // 明亮橙
                break;
            case 3:
                scoreText.color = Color.yellow;
                break;
            case 4:
                scoreText.color = new Color(0.2f, 1f, 0.3f);     // 亮黄绿
                break;
            case 5:
                scoreText.color = new Color(0f, 0.95f, 0.4f);     // 更深绿
                break;
            default:
                scoreText.color = new Color(0.9f, 0.1f, 0.2f);
                break;
        }
    }

    void TextFading()
    {
        if (scoreText == null) return;
        float timeSinceLast = Time.time - lastInteractTime;
        // 等待 fadeDelay 秒后才开始渐隐
        if (timeSinceLast > fadeDelay)
        {
            float t = Mathf.Clamp01((timeSinceLast - fadeDelay) / fadeDuration);
            Color c = scoreText.color;
            c.a = Mathf.Lerp(1f, 0f, t);  // alpha 从 1 到 0
            scoreText.color = c;
        }
    }
}
