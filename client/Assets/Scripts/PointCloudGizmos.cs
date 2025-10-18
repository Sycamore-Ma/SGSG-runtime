using UnityEngine;
using System.Collections.Generic;

public class PointCloudGizmos : MonoBehaviour
{
    private List<Vector3> points = new List<Vector3>();
    private List<Color> colors = new List<Color>();

    public void SetPoints(Vector3[] p, Color32[] c)
    {
        points.Clear();
        colors.Clear();
        points.AddRange(p);
        foreach (var col in c) colors.Add(col);
    }

    private void OnDrawGizmos()
    {
        if (points == null || colors == null) return;
        // for (int i = 0; i < points.Count; i++)
        for (int i = points.Count-1; i >= 0; i--)
        {
            Gizmos.color = colors[i];
            Gizmos.DrawSphere(transform.TransformPoint(points[i]), 0.02f); // 绘制点云
        }
    }
}
