using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

public class StrokesDrawer : MonoBehaviour
{
    public bool enableDrawing = false;

    public GameObject eventMonitor;     // 指向 EventMonitor 对象（用于获取 XR 输入等）
    public GameObject xrOrigin;         // 指向 XR Origin 对象
    private XRInputHandler xrInput;     // XR 输入处理器实例
    private Transform pokeInteractor;   // 右手 Poke Interactor 的 Transform
    public LineRenderer greyLineRendererPrefab;   // 预制的 LineRenderer：用于绘制灰色（临时）轨迹
    public LineRenderer type0LineRendererPrefab;  // 预制的 LineRenderer：用于绘制 Type0 轨迹
    public LineRenderer type1LineRendererPrefab;  // 预制的 LineRenderer：用于绘制 Type1 轨迹
    public LineRenderer type2LineRendererPrefab;  // 预制的 LineRenderer：用于绘制 Type2 轨迹

    private LineRenderer currentLine;
    private List<Vector3> strokePoints = new List<Vector3>();
    private List<LineRenderer> allStrokes = new List<LineRenderer>(); // **用于存储所有绘制的轨迹**
    private bool isDrawing = false;
    private int strokeType = -1; // 记录当前绘制的轨迹类型

    public string jsonSavingPath = "Assets/Scripts/manual_strokes.json"; // **手动保存 JSON 路径**

    private float interactionStartTime = -1f;  // 记录开始交互的时间

    public bool shouldKeepStrokesUI = false;

    public int strokesCount = 0;

    public int GetStrokeCount()
    {
        return strokesCount;
    }

    void Start()
    {
        if (eventMonitor != null)
        {
            xrInput = eventMonitor.GetComponent<XRInputHandler>();
        }
        if (xrOrigin != null)
        {
            Transform rightController = xrOrigin.transform.Find("Camera Offset/Right Controller");
            if (rightController != null)
            {
                pokeInteractor = rightController.Find("Poke Interactor");
            }
        }
    }

    void Update()
    {
        if (xrInput == null)
            return;

        if (enableDrawing)
        {
            if (xrInput.rightTriggerPressed)
            {
                if (!isDrawing)
                {
                    StartNewStroke(0); // 记录当前绘制的轨迹类型为 0
                }
                strokePoints.Add(GetPokeInteractorPosition());      // 获取 xrInput.rightHandPos
                UpdateLine();
            }
            else if (xrInput.rightGripPressed)
            {
                if (!isDrawing)
                {
                    StartNewStroke(1); // 记录当前绘制的轨迹类型为 1
                }
                strokePoints.Add(GetPokeInteractorPosition());
                UpdateLine();
            }
            else if (xrInput.B_buttonPressed)
            {
                if (!isDrawing)
                {
                    StartNewStroke(2); // 记录当前绘制的轨迹类型为 2
                }
                strokePoints.Add(GetPokeInteractorPosition());
                UpdateLine();
            }
            else if (isDrawing)
            {
                EndStroke();                // isDrawing 状态变为 false
                // 结束绘制时记录一次
                DataSender dataSender = FindObjectOfType<DataSender>();
                if (dataSender != null)
                {
                    dataSender.SendStroke();
                }
                StartCoroutine(UpdateStrokeApperance());
            }
        }

        // **检测 Y 按钮是否被按下**
        //if (xrInput.Y_buttonPressed)
        //{
        //    ClearAllStrokes();
        //}
        // 这里的逻辑应该是 LoadOBJ 和 Reset 相关的内容，应该与 ClearAllStrokes() 结合起来，形成一个完整的逻辑
        // 这里面应该有对当前状态的判断，以及对服务器的请求
    }

    void StartNewStroke(int type)
    {
        isDrawing = true;
        strokePoints.Clear();
        strokeType = type;

        interactionStartTime = Time.realtimeSinceStartup;  // 记录开始时间

        // 生成新的 LineRenderer
        currentLine = Instantiate(greyLineRendererPrefab, Vector3.zero, Quaternion.identity);

        if (currentLine != null)
        {
            // currentLine.widthMultiplier = 0.01f;
            currentLine.widthMultiplier = 0.05f;
        }
    }

    void UpdateLine()
    {
        if (currentLine == null) return;
        currentLine.positionCount = strokePoints.Count;     // positionCount 是 LineRenderer 的一个属性，用于设置当前绘制的点的数量
        currentLine.SetPositions(strokePoints.ToArray());   // SetPositions(Vector3[]) 是 LineRenderer 的一个方法，用于设置当前绘制的点的位置，参数是一个 Vector3[] 数组，这里将 List<Vector3> 转换为 Vector3[] 数组
    }

    void EndStroke()
    {
        strokesCount ++;
        SaveStrokesToJson(strokeType, currentLine);           // **记录绘制结束并保存为 JSON**

        // 记录持续时间
        if (interactionStartTime >= 0f)
        {
            float duration = (Time.realtimeSinceStartup - interactionStartTime) * 1000f;
            TimeLogManager logger = FindObjectOfType<TimeLogManager>();
            logger?.UpdateTime("InteractionTime", duration);
            Debug.Log($"[TimeLogManager] InteractionTime: {duration:F2} ms");
            interactionStartTime = -1f; // 重置开始时间
        }

        isDrawing = false;
    }

    IEnumerator<WaitForSeconds> UpdateStrokeApperance()
    {
        yield return new WaitForSeconds(0.0f);

        // 生成新的 LineRenderer
        LineRenderer finalLine = null;
        switch (strokeType)
        {
            case 0:
                finalLine = Instantiate(type0LineRendererPrefab, Vector3.zero, Quaternion.identity);
                break;
            case 1:
                finalLine = Instantiate(type1LineRendererPrefab, Vector3.zero, Quaternion.identity);
                break;
            case 2:
                finalLine = Instantiate(type2LineRendererPrefab, Vector3.zero, Quaternion.identity);
                break;
        }

        if (finalLine != null)
        {
            // finalLine.widthMultiplier = 0.01f;
            finalLine.widthMultiplier = 0.05f;
            finalLine.positionCount = currentLine.positionCount;
            finalLine.SetPositions(strokePoints.ToArray()); // 生成新的 LineRenderer

            allStrokes.Add(finalLine); // **将新的 LineRenderer 添加到列表中**
        }

        Destroy(currentLine); // 释放当前 LineRenderer
        strokeType = -1;

        //yield break; // **结束协程**
    }

    public void ClearAllStrokes()
    {
        strokesCount = 0;
        // **重置为默认 JSON 结构**
        string defaultJson = "{\n    \"strokeType\": -1,\n    \"lineData\": []\n}";
        File.WriteAllText(jsonSavingPath, defaultJson);

        if (shouldKeepStrokesUI)    // 如果用户选择保留
        {
            return;
        }

        foreach (var stroke in allStrokes)
        {
            if (stroke != null)
            {
                Destroy(stroke);
            }
        }
        allStrokes.Clear(); // **清空所有绘制的线条**
    }

    [System.Serializable]
    public class StrokeData
    {
        public int strokeType;
        public List<Vector3> lineData;
    }

    void SaveStrokesToJson(int strokeType, LineRenderer finalLine)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < finalLine.positionCount; i+=6)
        {
            Vector3 pos = finalLine.GetPosition(i);
            positions.Add(new Vector3(pos.x, pos.z, pos.y)); // 将 y 轴和 z 轴互换
        }
        Debug.Log($"finalLine.positionCount: {finalLine.positionCount}");
        Debug.Log($"finalLine stroke.positionCount: {finalLine.positionCount / 6}");

        // 组织数据结构
        var strokeData = new StrokeData
        {
            strokeType = strokeType,
            lineData = positions // 直接使用 List<Vector3>
        };

        // 将 JSON 数据写入文件
        File.WriteAllText(jsonSavingPath, JsonUtility.ToJson(strokeData, true));
    }


    Vector3 GetPokeInteractorPosition()
    {
        //return xrInput.rightHandPos;
        return pokeInteractor.position;     // pokeInteractor.position 是相对于世界坐标系的，pokeInteractor.localPosition 是相对于父物体的坐标系
    }
}
