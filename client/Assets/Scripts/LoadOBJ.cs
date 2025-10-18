using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Dummiesman; // 需要导入 OBJ 加载库 Runtime OBJ Importer - Dummiesman

using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Linq;
using System.Globalization;

using System;

public class LoadOBJ : MonoBehaviour
{
    public string methodName = "VRSG";
    public string modelPath = "I:/3RScan/data/3RScan/";                 
    public string sceneID = "3b7b33af-1b11-283e-9abd-170c6329c0e6";     // test 场景 id
    public string objName = "mesh.refined.align.v2.obj";                // "mesh.refined.v2.obj"
    public GameObject Communication;
    private DataSender dataSender; // 引用 DataSender
    private GameObject loadedModel;

    public GameObject cameraOffset; // 在 Inspector 中拖入 Camera Offset，用于 resetcamerapos。XRI 不能直接修改 camera.transform.position

    //private TimeLogManager logger;

    void Start()
    {
        dataSender = Communication.GetComponent<DataSender>(); // 获取 DataSender 组件
        if (dataSender == null)
        {
            Debug.LogError("DataSender 组件未找到，请确保 Communication 挂载在 ModelLoader 上！");
            return;
        }

        //logger = FindObjectOfType<TimeLogManager>();

        Reset();
    }

    void LoadModel(string objName)
    {
        string objPath = Path.Combine(modelPath + sceneID, objName);
        string mtlPath = Path.Combine(modelPath + sceneID, "mesh.refined.mtl");
        string texturePath = Path.Combine(modelPath + sceneID, "mesh.refined_0.png");

        if (!File.Exists(objPath))
        {
            Debug.LogError("OBJ 文件未找到: " + objPath);
            return;
        }

        // 读取 OBJ 文件
        loadedModel = new OBJLoader().Load(objPath);

        if (loadedModel == null)
        {
            Debug.LogError("OBJ 加载失败");
            return;
        }

        loadedModel.transform.SetParent(transform);
        // 旋转模型 (Z-up -> Y-up)
        //loadedModel.transform.localRotation = Quaternion.identity;
        loadedModel.transform.localRotation = Quaternion.Euler(-90, 180, 0);
        loadedModel.transform.localPosition = Vector3.zero;
        //loadedModel.transform.localScale = Vector3.one;   

        // 加载 MTL 材质
        if (File.Exists(mtlPath))
        {
            Material objMaterial = LoadMTL(mtlPath, texturePath);
            if (objMaterial != null)
            {
                ApplyMaterial(loadedModel, objMaterial);
            }
        }
    }

    Material LoadMTL(string mtlPath, string texturePath)
    {
        Material material = new Material(Shader.Find("Standard"));

        if (File.Exists(texturePath))
        {
            byte[] texData = File.ReadAllBytes(texturePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(texData);
            texture.Apply();

            material.mainTexture = texture;
        }

        return material;
    }

    void ApplyMaterial(GameObject model, Material material)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = material;
        }
    }

    public void Reset()     // 不加 public 默认是 pravate
    {
        if (loadedModel != null)
        {
            Debug.Log("释放已加载的 OBJ 模型：" + loadedModel.name);
            Destroy(loadedModel);
            loadedModel = null;
        }

        StrokesDrawer strokesDrawer = FindObjectOfType<StrokesDrawer>();
        if (strokesDrawer != null)
        {
            strokesDrawer.ClearAllStrokes();
        }

        // **调用 DataSender 发送 Scene ID**
        Debug.Log("准备发送 Scene ID: " + sceneID + "和 Method Name: " + methodName);
        dataSender.SendSceneID_MethodName(sceneID, methodName);
        //dataSender.SendStroke();

        // LoadModel(objName); 
        TimeLogManager.MeasureAndLog("LoadTime", () => {
            LoadModel(objName);     // () => { LoadModel(objName); } 是一个 匿名方法（lambda）
        });

        // 这个样做其实也不对，因为并不知道 server 有没有加载成功
        dataSender.SendStroke();

        Debug.Log("OBJ 模型加载完成");

        RelocateCameraOffsetToModel();
    }


    public void RelocateCameraOffsetToModel()
    {
        if (loadedModel != null && cameraOffset != null)
        {
            Renderer[] renderers = loadedModel.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    bounds.Encapsulate(r.bounds);

                Vector3 min = bounds.min;
                Vector3 center = bounds.center;

                // ✅ 设置位置：放到 min.x, min.z，保持当前 Y 高度
                Vector3 currentOffset = cameraOffset.transform.position;
                //Vector3 newOffset = new Vector3(min.x, currentOffset.y, min.z - 1f); // -1f 视距稍后退
                Vector3 newOffset = new Vector3(min.x + 1f, currentOffset.y, min.z + 1f); // -1f 视距稍后退
                // Vector3 newOffset = new Vector3(min.x +8f, currentOffset.y, min.z + 2f); // -1f 视距稍后退
                cameraOffset.transform.position = newOffset;

                // ✅ 设置朝向：Y轴朝向模型中心
                Vector3 lookTarget = new Vector3(center.x, newOffset.y, center.z);
                Vector3 forward = lookTarget - newOffset;
                forward.y = 0; // 保证只绕 Y 轴旋转

                if (forward.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(forward);
                    cameraOffset.transform.rotation = targetRot;
                }

                Debug.Log($"📍 CameraOffset 移动到 {newOffset}，朝向中心 {center}");
            }
        }
    }

}

