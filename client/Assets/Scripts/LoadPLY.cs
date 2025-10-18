using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Linq;
using System.Globalization;

using UnityEditor;

public class LoadPLY : MonoBehaviour
{
    public string modelPath = "I:/3RScan/data/3RScan/";                 
    public string sceneID = "3b7b33af-1b11-283e-9abd-170c6329c0e6";     // test 场景 id
    public string plyName = "labels.inseg.ply";                         // "labels.instances.align.annotated.v2.ply"

    [Range(0.1f, 10f)]              // 设置 pointsize 范围
    public float pointsize = 4.0f;

    [Range(0f, 1f)]                 // 设置 alpha 透明度范围
    public float alpha = 1.0f;

    private GameObject pointCloud;
    private MeshRenderer meshRenderer;


    // Start is called before the first frame update
    void Start()
    {
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            meshRenderer.sharedMaterial.SetFloat("_PointSize", pointsize);
            meshRenderer.sharedMaterial.SetFloat("_Alpha", alpha);
        }
    }

    void LoadBinaryPLY(string plyName)
    {
        string plyPath = Path.Combine(modelPath, sceneID, plyName);
        if (!File.Exists(plyPath))
        {
            Debug.LogError("PLY 文件未找到: " + plyPath);
            return;
        }
        Debug.Log("开始解析二进制 PLY: " + plyPath);

        if (ReadBinaryPlyFile(plyPath, out Vector3[] points, out Color32[] colors))
        {
            CreatePointCloud(points, colors);
            Debug.Log("PLY 加载完成: " + plyName);
        }
        else
        {
            Debug.LogError("PLY 解析失败！");
        }
    }

    bool ReadBinaryPlyFile(string filePath, out Vector3[] points, out Color32[] colors)
    {
        points = null;
        colors = null;
        List<Vector3> vertices = new List<Vector3>();
        List<Color32> vertexColors = new List<Color32>();

        int vertexCount = 0;
        int dataStartPosition = 0;

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // 1. 读取 Header
            while (true)
            {
                string line = ReadLine(reader);
                if (line.StartsWith("element vertex"))
                {
                    vertexCount = int.Parse(line.Split(' ')[2]);
                }
                else if (line.StartsWith("end_header"))
                {
                    dataStartPosition = (int)fs.Position;
                    break;
                }
            }

            // 2. 读取二进制数据
            fs.Seek(dataStartPosition, SeekOrigin.Begin);

            for (int i = 0; i < vertexCount; i++)
            {
                float x = reader.ReadSingle(); // 4 bytes float
                float y = reader.ReadSingle(); // 4 bytes float
                float z = reader.ReadSingle(); // 4 bytes float
                reader.ReadUInt16(); // label (2 bytes, 忽略)
                reader.ReadSingle(); // nx (法线, 4 bytes, 忽略)
                reader.ReadSingle(); // ny
                reader.ReadSingle(); // nz
                reader.ReadSingle(); // curvature (4 bytes, 忽略)
                byte r = reader.ReadByte(); // red (1 byte)
                byte g = reader.ReadByte(); // green (1 byte)
                byte b = reader.ReadByte(); // blue (1 byte)
                byte a = reader.ReadByte(); // alpha (1 byte, 可忽略)
                reader.ReadSingle(); // quality (4 bytes, 忽略)
                reader.ReadSingle(); // radius (4 bytes, 忽略)

                //if (i < vertexCount / 2)    // 加上这一句，有些点就显示出来了。Unity 2017+ 及更高版本： 一个 Mesh 只能包含 65535 个顶点（ushort 索引）
                //    continue;

                vertices.Add(new Vector3(x, y, z));
                //vertices.Add(new Vector3(x*0.1f, y*0.1f, z*0.1f));      // 测试点云为什么加载不全，有明显边界。是裁剪了吗？
                vertexColors.Add(new Color32(r, g, b, 255));
            }
        }

        points = vertices.ToArray();
        colors = vertexColors.ToArray();
        return points.Length > 0;
    }

    void CreatePointCloud(Vector3[] points, Color32[] colors)
    {
        pointCloud = new GameObject("PointCloud");
        MeshFilter meshFilter = pointCloud.AddComponent<MeshFilter>();
        meshRenderer = pointCloud.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh
        {
            name = plyName,
            vertices = points,
            colors32 = colors
        };

        int[] indices = new int[points.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        Debug.Log($"Total points: {points.Length}, Total indices: {indices.Length}");
        if (points.Length > 65535)
            Debug.LogError("Warning: Mesh vertex count exceeds 65535, enable UInt32 index!");

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        //mesh.colors32 = colors;     // 赋值颜色
        //meshFilter.mesh = mesh;
        meshFilter.sharedMesh = mesh;
        //mesh.RecalculateBounds();   // 确保 Mesh 更新     但如果 点云包含极端值（如 NaN 或过大坐标），RecalculateBounds() 可能会裁剪数据。
        //mesh.UploadMeshData(true);

        // 使用支持顶点颜色的 Shader
        Material pointMaterial = new Material(Shader.Find("Unlit/VertexColor"));

        pointMaterial.SetFloat("_PointSize", pointsize);  // 调整点大小
        //meshRenderer.material = pointMaterial;            // 由于 material 会实例化一个新材质，如果有多个对象使用 material，可能会造成大量 GPU 资源占用，不适合大规模对象修改。
        meshRenderer.sharedMaterial = pointMaterial;        // 直接修改 sharedMaterial 会影响 所有使用这个材质的对象，因为它修改的是 材质资源本身。如果希望所有同一材质的对象都同步变化，使用 sharedMaterial 更合适。

        pointCloud.transform.SetParent(transform);
        // **添加旋转、缩放、平移**
        pointCloud.transform.localRotation = Quaternion.Euler(-90, 180, 0);
        pointCloud.transform.localPosition = Vector3.zero;
        pointCloud.transform.localScale = new Vector3(-1, 1, 1);  // 设定缩放比例 // 这里暂时还不知道为什么需要比 obj 的对齐多一个 x 轴向翻转才能对齐

        pointCloud.AddComponent<PointCloudGizmos>().SetPoints(points, colors);  // 用于第三视角 Scene 视图下显示点云。shader 里面的 quad 渲染方式在 Scene 视图下面显示不出来
    }

    /// <summary>
    /// 读取 `.ply` 二进制 Header 的一行（Ply 文件的换行符是 `\n`）
    /// </summary>
    string ReadLine(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != '\n')
        {
            bytes.Add(b);
        }
        return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
    }

    public void Reset()     // 不加 public 默认是 pravate
    {
        if (pointCloud != null)
        {
            Debug.Log("释放已加载的点云：" + pointCloud.name);
            Destroy(pointCloud);
            pointCloud = null;
        }

        //LoadBinaryPLY(plyName);
        TimeLogManager.MeasureAndLog("LoadTime", () =>{
            LoadBinaryPLY(plyName);
        });
    }
}
