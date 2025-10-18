using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class ResultsVisualizer : MonoBehaviour
{
    public bool showSceneGraph = true; // 是否显示场景图
    public bool sceneViewVisible = true; // 在场景视图中是否可见

    public bool teaserMode = false;

    [Header("Tooltip 样式控制")]
    public int tooltipFontSize = 48;
    public float tooltipContentScale = 2.0f;

    //public GameObject xrOrigin;         // 绑定 XR Origin 物体

    public GameObject eventMonitor;     // 绑定 EventMonitor 物体
    private XRInputHandler xrInput;     // XRInputHandler 组件
    public GameObject nodeSpherePrefab;
    public GameObject tooltipPrefab;
    private Color originalTooltipColor;
    public GameObject labelSelectorPrefab; // 拖入 prefab
    private Dictionary<int, GameObject> nodeLabelSelectors = new(); // 挂载的 selector
    private GameObject activeLabelSelector = null;

    private Dictionary<int, GameObject> nodeObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> nodeTooltips = new Dictionary<int, GameObject>(); 
    private Dictionary<int, bool> nodeCheckFlag = new Dictionary<int, bool>(); // 节点是否被 check
    private List<Node> latestNodes = new List<Node>();

    public Material edgeMaterial;  // ✅ 默认 Material
    public Material edgeTextMaterial;  // ✅ 默认 Material
    private Dictionary<int, GameObject> edgeObjects = new Dictionary<int, GameObject>();
    //private Dictionary<int, TextMesh> edgeLabels = new Dictionary<int, TextMesh>(); 
    private Dictionary<int, TextMeshPro> edgeLabels = new Dictionary<int, TextMeshPro>(); 
    private Dictionary<int, string> edgeTexts = new Dictionary<int, string>();
    private List<Edge> latestEdges = new List<Edge>();

    [SerializeField]
    private Color nodeBlinkColor = Color.green; // 闪烁时的颜色
    [SerializeField]
    private Color edgeBlinkColor = Color.blue; // 闪烁时的颜色
    [SerializeField]
    private Color edgeFadeColor = Color.red; // 闪烁时的颜色
    [SerializeField]
    private float blinkDuration = 5f;      // 闪烁持续时间（秒）
    [SerializeField]
    private float blinkSpeed = 1f;         // 闪烁速度

    public bool visible = true;
    public bool visContentIsBuffer = false;
    public bool blinkable = true;
    private ResultResponse bufferResultResponse;        // 用于暂存本轮不做显示的场景图结果。

    public bool showInvisibleAsQuestionMark = true;

    public bool visualizeIndex = false;

    void Start()
    {
        //GameObject Camera = xrOrigin.Find("Main Camera");
        //GameObject Camera = GameObject.Find("Main Camera");
        //Camera.main.depthTextureMode = DepthTextureMode.Depth; // 启用深度纹理，Camera 是个类
        if (eventMonitor != null)
        {
            xrInput = eventMonitor.GetComponent<XRInputHandler>();
        }

        // ⭐️ 预取 tooltipPrefab 的 TipBackground 原始颜色
        if (tooltipPrefab != null)
        {
            var tipBg = tooltipPrefab.transform.Find("Pivot/ContentParent/TipBackground");
            if (tipBg != null)
            {
                var renderer = tipBg.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalTooltipColor = renderer.sharedMaterial.color;
                    Debug.Log($"🎨 原始 Tooltip 背景颜色: {originalTooltipColor}");
                }
            }
        }
    }

    void Update()
    {
        if (!showSceneGraph){
            // TODO:
            SetAllGraphObjectsActive(false); // ❌ 隐藏节点和边
            return;
        }

        SetAllGraphObjectsActive(true);  // ✅ 显示节点和边
        UpdateTooltipAppearance();
        float alpha = xrInput.leftGripValue;
        foreach (var label in edgeLabels.Values)
        {
            if (label == null || label.fontSharedMaterial == null) continue;

            Material mat = label.fontSharedMaterial;    // 在 C# 中，所有 class 类型变量都是 引用类型，传的是地址，不是值。

            if (mat.HasProperty("_FaceColor"))
            {
                Color faceColor = mat.GetColor("_FaceColor");
                faceColor.a = alpha;
                mat.SetColor("_FaceColor", faceColor);
            }
        }

        SetAllGraphObjectsSceneVisible(sceneViewVisible);
    }

    public void SetAllGraphObjectsActive(bool isActive)
    {
        foreach (var node in nodeObjects.Values)
        {
            if (node != null)
                node.SetActive(isActive);
        }

        foreach (var tooltip in nodeTooltips.Values)
        {
            if (tooltip != null)
                tooltip.SetActive(isActive);
        }

        foreach (var edge in edgeObjects.Values)
        {
            if (edge != null)
                edge.SetActive(isActive);
        }

        foreach (var label in edgeLabels.Values)
        {
            if (label != null)
                label.gameObject.SetActive(isActive);
        }

        foreach (var selector in nodeLabelSelectors.Values)
        {
            if (selector != null)
                selector.SetActive(isActive);
        }
    }

    public void SetAllGraphObjectsSceneVisible(bool isVisible)
    {
    #if UNITY_EDITOR
        void SetVisible(GameObject go)
        {
            if (go == null) return;
            var svm = UnityEditor.SceneVisibilityManager.instance;
            if (isVisible)
                // svm.Show(go, false);
                svm.Show(go, true);   // ✅ 显示该物体及其所有子物体
            else
                svm.Hide(go, true);
        }

        foreach (var node in nodeObjects.Values)
            SetVisible(node);

        foreach (var tooltip in nodeTooltips.Values)
            SetVisible(tooltip);

        foreach (var edge in edgeObjects.Values)
            SetVisible(edge);

        foreach (var label in edgeLabels.Values)
            SetVisible(label?.gameObject);

        foreach (var selector in nodeLabelSelectors.Values)
            SetVisible(selector);
    #endif
    }




    public void UpdateTooltipAppearance()
    {
        foreach (var kvp in nodeTooltips)
        {
            GameObject tooltipObj = kvp.Value;

            // 1️⃣ 更新 ToolTip 组件
            ToolTip tooltip = tooltipObj.GetComponent<ToolTip>();
            if (tooltip != null)
            {
                tooltip.ContentScale = tooltipContentScale;
                tooltip.FontSize = Mathf.RoundToInt(tooltipFontSize);
            }

            // 2️⃣ 更新 Label 子物体中的 TextMeshPro 字体大小
            Transform labelTransform = tooltipObj.transform.Find("Pivot/ContentParent/Label");
            if (labelTransform != null)
            {
                TMPro.TextMeshPro tmp = labelTransform.GetComponent<TMPro.TextMeshPro>();
                if (tmp != null)
                {
                    tmp.fontSize = tooltipFontSize;
                }
                else
                {
                    Debug.LogWarning($"⚠️ 未找到 TextMeshPro 组件于节点 {kvp.Key}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ 未找到 Label 对象于节点 {kvp.Key}");
            }
        }
    }



    public void check(Vector3 checkpos)
    {
        float minDist = float.MaxValue;
        int closestNodeId = -1;
        float threshold = 0.2f; // 可调阈值（单位：米）

        foreach (var kvp in nodeTooltips)
        {
            int nodeId = kvp.Key;
            GameObject tooltip = kvp.Value;
            Transform tipBg = tooltip.transform.Find("Pivot/ContentParent/TipBackground");
            if (tipBg == null) continue;
            float dist = Vector3.Distance(tipBg.position, checkpos);
            if (dist < threshold && dist < minDist)
            {
                minDist = dist;
                closestNodeId = nodeId;
            }
        }
        if (closestNodeId != -1)
        {
            if (!nodeCheckFlag.ContainsKey(closestNodeId))
            {
                nodeCheckFlag[closestNodeId] = true; // 初次点击
            }
            else
            {
                nodeCheckFlag[closestNodeId] = !nodeCheckFlag[closestNodeId]; // 状态取反
            }
            Debug.Log($"🟡 Node {closestNodeId} check flag = {nodeCheckFlag[closestNodeId]}");

            // ✅ 设置 TipBackground 材质颜色为红色
            GameObject tooltip = nodeTooltips[closestNodeId];
            var tipBgTransform = tooltip.transform.Find("Pivot/ContentParent/TipBackground");
            if (tipBgTransform != null)
            {
                var renderer = tipBgTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (nodeCheckFlag[closestNodeId])
                    {
                        renderer.material.color = Color.red;

                        // ⭐️ 添加 LabelSelector 到 TipBackground 上
                        if (!nodeLabelSelectors.ContainsKey(closestNodeId))
                        {
                            GameObject labelUI = Instantiate(labelSelectorPrefab);
                            labelUI.transform.SetParent(tipBgTransform, false); // ⭐️ 附着在 TipBackground 上
                            labelUI.transform.localPosition = new Vector3(0f, -0.05f, 0f); // ⭐️ 显示在下方（Y 值为负）
                            labelUI.transform.localRotation = Quaternion.identity;
                            nodeLabelSelectors[closestNodeId] = labelUI;
                            activeLabelSelector = labelUI;
                            activeLabelSelector.GetComponent<LabelSelectorController>().SetLabelText(tooltip.GetComponent<ToolTip>().ToolTipText); // 设置 LabelSelector 的文本
                        }
                    }
                    else
                    {
                        //renderer.material.color = Color.white; // 恢复原色
                        renderer.material.color = originalTooltipColor;

                        // ⭐️ 删除已有的 LabelSelector
                        if (nodeLabelSelectors.TryGetValue(closestNodeId, out GameObject selector))
                        {
                            if (activeLabelSelector == selector)
                                activeLabelSelector = null;

                            Destroy(selector);
                            nodeLabelSelectors.Remove(closestNodeId);
                        }

                    }
                }
            }
        }
        else
        {
            Debug.Log("⚠️ 未找到足够接近的标签 TipBackground。");
        }
    }


    private bool IsChecked(int nodeId)
    {
        return nodeCheckFlag.ContainsKey(nodeId) && nodeCheckFlag[nodeId];
    }

    public void activeLabelSelectorToNext()
    {
        if (activeLabelSelector != null)
        {
            var controller = activeLabelSelector.GetComponent<LabelSelectorController>();
            if (controller != null)
            {
                controller.NextLabel();
            }
        }
    }

    public void activeLabelSelectorToPrev()
    {
        if (activeLabelSelector != null)
        {
            var controller = activeLabelSelector.GetComponent<LabelSelectorController>();
            if (controller != null)
            {
                controller.PrevLabel();
            }
        }
    }

    public float GetNodeErrorRateAfterCorrection()
    {
        if (latestNodes == null || latestNodes.Count == 0)
            return 0f;

        int total = 0;
        int wrong = 0;
        foreach (var node in latestNodes)
        {
            if (string.IsNullOrEmpty(node.gt_label)) continue;

            total++;
            string predictedLabel = node.label;

            // ✅ 如果该节点被 check，并且有 selector，使用 selector 中的标签作为预测标签
            if (IsChecked(node.idx) && nodeLabelSelectors.TryGetValue(node.idx, out GameObject selector))
            {
                var controller = selector.GetComponent<LabelSelectorController>();
                if (controller != null)
                {
                    predictedLabel = controller.GetCurrentLabel();
                }
            }

            // ✅ 比较是否错误
            if (predictedLabel != node.gt_label)
            {
                wrong++;
            }
        }

        //Debug.Log($"📊 错误节点数: {wrong} / 总节点数: {total}，错误率 = {(total == 0 ? 0f : (float)wrong / total):F2}");

        if (total == 0) return 0f;
        return (float)wrong / total;
    }

    public float GetEdgeErrorRateAfterCorrection()
    {
        if (latestEdges == null || latestEdges.Count == 0)
            return 0f;

        int total = 0;
        int wrong = 0;
        foreach (var edge in latestEdges)
        {
            if (string.IsNullOrEmpty(edge.gt_edge_label)) continue;
            if (edge.gt_edge_label == "none") continue;
            total++;
            //if ((edge.edge_label != edge.gt_edge_label) ^ IsChecked(edge.edge_idx))
            if (edge.edge_label != edge.gt_edge_label)
                wrong++;
        }

        //Debug.Log($"📊 错误关系数: {wrong} / 总关系数: {total}，错误率 = {(total == 0 ? 0f : (float)wrong / total):F2}");

        if (total == 0) return 0f;
        return (float)wrong / total;
    }

    public float GetTotalErrorRateAfterCorrection()
    {
        int nodeTotal = 0, nodeWrong = 0;
        int edgeTotal = 0, edgeWrong = 0;

        // ✅ Node 错误统计
        if (latestNodes != null)
        {
            foreach (var node in latestNodes)
            {
                if (string.IsNullOrEmpty(node.gt_label)) continue;
                nodeTotal++;

                string predictedLabel = node.label;

                if (IsChecked(node.idx) && nodeLabelSelectors.TryGetValue(node.idx, out GameObject selector))
                {
                    var controller = selector.GetComponent<LabelSelectorController>();
                    if (controller != null)
                    {
                        predictedLabel = controller.GetCurrentLabel();
                    }
                }

                if (predictedLabel != node.gt_label)
                    nodeWrong++;
            }
        }

        // ✅ Edge 错误统计（保持不变）
        if (latestEdges != null)
        {
            foreach (var edge in latestEdges)
            {
                if (string.IsNullOrEmpty(edge.gt_edge_label)) continue;
                if (edge.gt_edge_label == "none") continue;
                edgeTotal++;
                if (edge.edge_label != edge.gt_edge_label)
                    edgeWrong++;
            }
        }

        int total = nodeTotal + edgeTotal;
        int wrong = nodeWrong + edgeWrong;

        return total == 0 ? 0f : (float)wrong / total;
    }

    public void UpdateVisualization(ResultResponse resultResponse)      // DataReceiver 里面调用
    {
        Debug.Log($"🎯 预测完成: Scene ID = {resultResponse.scene_id}, 结果 = {resultResponse.result}");

        if (visible)      
        {
            if (visContentIsBuffer)
            {
                resultResponse = bufferResultResponse;
            }
            VisualizeNodes(resultResponse.nodes);
            VisualizeEdges(resultResponse.edges);
        }
        else // UserScore 中每个场景的第一个 VRSG 方法，隐藏标签
        {
            bufferResultResponse = resultResponse;
            if (showInvisibleAsQuestionMark)
            {
                VisualizeNodes(resultResponse.nodes);
                VisualizeEdges(resultResponse.edges);
            }
            else
            {
                ClearNodes();  // ⭐️ 显示前清除旧的节点和边
                ClearEdges();  // ⭐️ 显示前清除旧的节点和边
            }
        }

        // 在这里设置场景视图的可编辑视图可见性，补充一句，防止在更新后发生闪烁
        SetAllGraphObjectsSceneVisible(sceneViewVisible);       // ✅ 是否显示在场景编辑视图中
    }

    //private void VisualizeNodes(List<Node> nodesReference)      // List 传进来是引用数据
    private void VisualizeNodes(List<Node> nodes)
    {
        latestNodes = nodes;  // 存下来供后续算 GetErrorRateAfterCorrection() 使用

        //var nodes = new List<Node>(nodesReference); // 拷贝一份，防止修改原始数据。但也是浅拷贝，里面的每一个 node 还是没变化的。
        foreach (var node in nodes)
        {
            Vector3 position = new Vector3(node.center[0], node.center[2], node.center[1]);
            string labelText = node.label;      // 防止直接修改引用类型内容
            if (!visible && showInvisibleAsQuestionMark)
            {
                labelText = "?";
            }

            string idText = "";
            if (visualizeIndex)
                idText = $"GT: {node.gt_label}\nID: {node.idx}\n";

            if (!nodeObjects.ContainsKey(node.idx))
            {
                //GameObject nodeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject nodeSphere = Instantiate(nodeSpherePrefab);
                nodeSphere.transform.position = position;
                //nodeSphere.transform.localScale = Vector3.one * 0.02f;

                GameObject tooltip = Instantiate(tooltipPrefab);
                tooltip.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
                tooltip.GetComponent<ToolTipConnector>().Target = nodeSphere;

                PositionAnchor anchor = tooltip.AddComponent<PositionAnchor>();
                anchor.Target = nodeSphere.transform;
                anchor.Offset = 0.2f;
                anchor.Camera = Camera.main;

                ToolTip tooltipComponent = tooltip.GetComponent<ToolTip>();
                if (tooltipComponent != null)
                {
                    //tooltipComponent.ToolTipText = $"ID: {node.idx}\n{labelText}\nGT: {node.gt_label}";
                    tooltipComponent.ToolTipText = idText + $"{labelText}";
                }

                nodeObjects[node.idx] = nodeSphere;
                nodeTooltips[node.idx] = tooltip;
            }
            else
            {
                nodeObjects[node.idx].transform.position = position;
                //string currentText = $"ID: {node.idx}\n{labelText}\nGT: {node.gt_label}";
                string currentText = idText + $"{labelText}";
                if (currentText != nodeTooltips[node.idx].GetComponent<ToolTip>().ToolTipText)
                {
                    nodeTooltips[node.idx].GetComponent<ToolTip>().ToolTipText = currentText;
                    BlinkTooltipBackground(nodeTooltips[node.idx]);
                }
            }

            nodeTooltips[node.idx].GetComponent<ToolTip>().ContentScale = 2.0f;     // 3.0f;
            nodeTooltips[node.idx].GetComponent<ToolTip>().FontSize = 48;    // 40 TODO: CHANGE TEXT SIZE
        }
    }

    private void VisualizeEdges(List<Edge> edges)
    {
        latestEdges = edges;

        foreach (var edgeObj in edgeObjects.Values)
        {
            Destroy(edgeObj);
        }
        edgeObjects.Clear();
        edgeLabels.Clear();

        // ✅ 处理 edges
        foreach (var edge in edges)
        {
            string labelText = edge.edge_label;

            if (labelText == "none")
            {
                continue;
            }

            if (!visible && showInvisibleAsQuestionMark)
            {
                labelText = "?";
            }

            if (nodeObjects.ContainsKey(edge.src) && nodeObjects.ContainsKey(edge.tgt))
            {
                //string labelText = $"Edge {edge.edge_idx}: {edge.edge_label}";
               
                Vector3 srcPos = new Vector3(edge.src_center[0], edge.src_center[2], edge.src_center[1]);
                Vector3 tgtPos = new Vector3(edge.tgt_center[0], edge.tgt_center[2], edge.tgt_center[1]);
                //Vector3 midPoint = (srcPos + tgtPos) / 2;
                Vector3 midPoint = Vector3.Lerp(srcPos, tgtPos, 0.33f);

                //// **偏移有向边的标签**
                //Vector3 edgeDirection = (tgtPos - srcPos).normalized;
                //Vector3 upVector = Vector3.up;
                //Vector3 offsetDirection = Vector3.Cross(edgeDirection, upVector).normalized; // 计算法向量
                //float offsetAmount = 0.01f * (edge.edge_idx % 2 == 0 ? 1 : -1); // 交错偏移
                //midPoint += offsetDirection * offsetAmount;

                GameObject edgeObj = new GameObject($"Edge_{edge.edge_idx}");
                edgeObjects[edge.edge_idx] = edgeObj;

                LineRenderer lineRenderer = edgeObj.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.01f;
                lineRenderer.endWidth = 0.01f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(new Vector3[] { srcPos, tgtPos });

                // ✅ 默认使用全局 edgeMaterial
                if (edgeMaterial != null)
                {
                    lineRenderer.material = edgeMaterial;
                    if (labelText == "same part")
                    {
                        labelText = "SP";
                        //lineRenderer.material.color = new Color(1, 1, 0, 0.25f);
                        var c = lineRenderer.material.color;
                        lineRenderer.material.color = new Color(1, 1, 0, c.a); // ✅ 保持 alpha 不变，只改颜色
                    }
                    if (labelText == "none")
                    {
                        lineRenderer.material.color = new Color(0, 0, 0, 0);
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ EdgeMaterial 未分配，使用默认白色材质");
                    lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
                }

                if (labelText != "none")
                {
                    GameObject textObj = new GameObject($"EdgeLabel_{edge.edge_idx}");
                    textObj.transform.position = midPoint;
                    textObj.transform.parent = edgeObj.transform;   // 让 textObj 作为 edgeObj 的子对象

                    //TextMesh textMesh = textObj.AddComponent<TextMesh>();       // TextMesh 是 Unity 中用来显示 3D 文本的组件，它内部使用的是 Unity 的默认字体 Shader（一般是 TextMesh/Text），并且它渲染的是字体网格，而不是用来画线的几何体。
                    TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();

                    //textMesh.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    // 复制一份材质（防止共享修改）
                    Material instanceMat = new Material(edgeTextMaterial);

                    textMesh.text = labelText;
                    //textMesh.fontSize = 80;
                    textMesh.fontSize = 100;
                    instanceMat.SetColor("_FaceColor", new Color(1, 1, 1, 1)); // 白色         // 初始透明，后面按下 left grip 的时候才会有显示
                    if (labelText == "SP")
                    {
                        instanceMat.SetColor("_FaceColor", new Color(1, 1, 0, 1)); // 黄色
                    }

                    textMesh.fontSharedMaterial = instanceMat; // 不是 .material，而是 .fontSharedMaterial！
                    textMesh.alignment = TextAlignmentOptions.Center; // 包括水平和垂直居中
                    textMesh.fontSize = 0.5f; // 字号大小用 fontSize 控制，自己调一下合适的数值

                    FaceCameraAlongEdge aligner = textObj.AddComponent<FaceCameraAlongEdge>();
                    aligner.edgeDirection = (tgtPos - srcPos).normalized;

                    edgeLabels[edge.edge_idx] = textMesh;
                }

                if (!edgeTexts.ContainsKey(edge.edge_idx) || edgeTexts[edge.edge_idx] != labelText)
                {
                    edgeTexts[edge.edge_idx] = labelText;
                    if (labelText == "none")
                    {
                        BlinkEdge(edge.edge_idx, edgeFadeColor, blinkDuration, blinkSpeed);
                    }
                    else
                    {
                        BlinkEdge(edge.edge_idx, edgeBlinkColor, blinkDuration, blinkSpeed);
                    }
                }
            }
        }
    }

    // ⭐️ 清除节点对象及其 tooltip
    public void ClearNodes()
    {
        foreach (var nodeObj in nodeObjects.Values)
        {
            Destroy(nodeObj);
        }
        nodeObjects.Clear();

        foreach (var tooltipObj in nodeTooltips.Values)
        {
            Destroy(tooltipObj);
        }
        nodeTooltips.Clear();

        // ✅ 清除 check 状态
        nodeCheckFlag.Clear();

        // ✅ 清除挂载的 LabelSelector UI
        if (nodeLabelSelectors != null)
        {
            foreach (var selector in nodeLabelSelectors.Values)
            {
                Destroy(selector);
            }
            nodeLabelSelectors.Clear();
        }
    }


    // ⭐️ 清除边对象及其标签
    public void ClearEdges()
    {
        foreach (var edgeObj in edgeObjects.Values)
        {
            Destroy(edgeObj);
        }
        edgeObjects.Clear();
        edgeLabels.Clear();
        edgeTexts.Clear();
    }


    private void BlinkTooltipBackground(GameObject tooltip)
    {
        if (blinkable == false)
        {
            return;
        }
        var tipBgTransform = tooltip.transform.Find("Pivot/ContentParent/TipBackground");
        if (tipBgTransform != null)
        {
            var bgRenderer = tipBgTransform.GetComponent<Renderer>();
            if (bgRenderer != null)
            {
                StartCoroutine(BlinkMaterialColor(bgRenderer.material));
            }
        }
    }

    private IEnumerator BlinkMaterialColor(Material mat)
    {
        if (teaserMode)
        {
            mat.color = nodeBlinkColor;
        }

        Color originalColor = mat.color;
        
        float timer = 0f;

        while (timer < blinkDuration)
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            mat.color = Color.Lerp(originalColor, nodeBlinkColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        mat.color = originalColor;
    }

    public void BlinkEdge(int edgeIdx, Color blinkColor, float duration = 2f, float speed = 2f)
    {
        if (blinkable == false)
        {
            return;
        }
        if (edgeObjects.ContainsKey(edgeIdx))
        {
            LineRenderer lineRenderer = edgeObjects[edgeIdx].GetComponent<LineRenderer>();
            Material blinkMaterial = new Material(lineRenderer.material);
            lineRenderer.material = blinkMaterial;
            StartCoroutine(BlinkMaterial(blinkMaterial, blinkColor, duration, speed));
        }
    }

    private IEnumerator BlinkMaterial(Material material, Color blinkColor, float duration, float speed)
    {
        Color originalColor = material.color;
        float timer = 0f;

        while (timer < duration)
        {
            float t = Mathf.PingPong(Time.time * speed, 1f);
            material.color = Color.Lerp(originalColor, blinkColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        material.color = originalColor;
    }
}


public class FaceCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // 旋转 180° 以便文本正确显示
        }
    }
}


public class FaceCameraAlongEdge : MonoBehaviour
{
    public Vector3 edgeDirection;          // 外部设置的边方向
    public float verticalOffset = 0.05f;   // 与边的法向方向偏移距离
    private Vector3 oriPos;                // 初始位置缓存，防止累计偏移
    private bool initialized = false;      // 确保 oriPos 只初始化一次

    void Update()
    {
        if (Camera.main == null) return;

        // ✅ 初始化 oriPos（只做一次）
        if (!initialized)
        {
            oriPos = transform.position;
            initialized = true;
        }

        // 先让标签朝向相机（仅仅是旋转到面向摄像机）
        transform.LookAt(Camera.main.transform);
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0); // 保持文字不反转

        // 再绕自身“面朝方向”的轴转动，使得水平对齐边方向
        Vector3 forward = transform.forward; // 朝向相机的方向
        Vector3 projectedEdgeDir = Vector3.ProjectOnPlane(edgeDirection, forward).normalized;

        if (projectedEdgeDir.sqrMagnitude > 0.0001f)
        {
            Quaternion alignRotation = Quaternion.FromToRotation(transform.right, projectedEdgeDir);
            transform.rotation = alignRotation * transform.rotation;
        }

        // Step 3: 确保字体始终“正立”，防止旋转超过 -90 到 +90 后上下颠倒
        Vector3 up = transform.up;
        if (Vector3.Dot(up, Vector3.up) < 0)
        {
            transform.Rotate(0, 0, 180);
        }

        // Step 4: 根据边方向在相机视角下是“左到右”还是“右到左”，决定上下偏移
        //Vector3 camRight = Camera.main.transform.right;
        //float dot = Vector3.Dot(projectedEdgeDir, camRight); // 正值说明左→右

        //Vector3 positionOffset = Vector3.up * verticalOffset * Mathf.Sign(dot);
        //transform.position += positionOffset;

        // 4️⃣ 沿法向量方向偏移（避免文字与边重合）
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 normalDir = Vector3.Cross(edgeDirection.normalized, camForward).normalized;     // 双向边，能算出来叉积的双向区分

        transform.position = oriPos + normalDir * verticalOffset;
    }
}