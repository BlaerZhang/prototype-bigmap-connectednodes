using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPath : MonoBehaviour
{
    [Header("路径节点")]
    public MapNode nodeA;
    public MapNode nodeB;
    
    [Header("路径形状")]
    public PathType pathType = PathType.Straight;
    public Transform[] controlPoints; // 用于贝塞尔曲线或折线的控制点
    
    [Header("路径类型")]
    public RoadType roadType = RoadType.Main; // 新增道路类型
    
    // 路径类型枚举
    public enum PathType
    {
        Straight,    // 直线
        Bezier,      // 贝塞尔曲线
        Segmented    // 折线
    }
    
    // 道路类型枚举
    public enum RoadType
    {
        Main,       // 主路
        Secondary,  // 小路
        Gravel,      // 碎石路
        Unpaved,     // 土路
        Trail      // 小径
    }
    
    // 路径可视组件
    private LineRenderer lineRenderer;
    
    // 添加一个公共方法来应用样式 - 用于编辑器工具
    public void ApplyRoadTypeStyle(Color color, float width, Material material = null)
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) return;
        }
        
        // 应用样式设置
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        
        if (material != null)
        {
            lineRenderer.material = material;
        }
        
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        
        // 更新路径形状
        UpdatePathVisual();
    }
    
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // 确保节点A和节点B知道它们连接到这条路径
        if (nodeA != null) nodeA.AddConnectedPath(this);
        if (nodeB != null) nodeB.AddConnectedPath(this);
        
        if (lineRenderer != null)
        {
            // 设置路径的形状
            UpdatePathVisual();
        }
    }
    
    // 更新路径视觉效果
    public void UpdatePathVisual()
    {
        if (lineRenderer == null || nodeA == null || nodeB == null)
            return;
            
        switch (pathType)
        {
            case PathType.Straight:
                DrawStraightPath();
                break;
                
            case PathType.Bezier:
                DrawBezierPath();
                break;
                
            case PathType.Segmented:
                DrawSegmentedPath();
                break;
        }
    }
    
    // 绘制直线路径
    private void DrawStraightPath()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, nodeA.transform.position);
        lineRenderer.SetPosition(1, nodeB.transform.position);
    }
    
    // 绘制贝塞尔曲线路径
    private void DrawBezierPath()
    {
        if (controlPoints == null || controlPoints.Length == 0)
        {
            Debug.LogWarning("贝塞尔曲线需要控制点！回退到直线路径。");
            DrawStraightPath();
            return;
        }
        
        // 使用20个点来绘制平滑曲线
        int segments = 20;
        lineRenderer.positionCount = segments + 1;
        
        Vector3 startPos = nodeA.transform.position;
        Vector3 endPos = nodeB.transform.position;
        
        if (controlPoints.Length == 1)
        {
            // 二次贝塞尔曲线
            Vector3 controlPoint = controlPoints[0].position;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = CalculateQuadraticBezierPoint(t, startPos, controlPoint, endPos);
                lineRenderer.SetPosition(i, point);
            }
        }
        else if (controlPoints.Length >= 2)
        {
            // 三次贝塞尔曲线
            Vector3 controlPoint1 = controlPoints[0].position;
            Vector3 controlPoint2 = controlPoints[1].position;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = CalculateCubicBezierPoint(t, startPos, controlPoint1, controlPoint2, endPos);
                lineRenderer.SetPosition(i, point);
            }
        }
    }
    
    // 计算二次贝塞尔曲线上的点
    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2 * (1-t) * t * P1
        p += tt * p2; // t^2 * P2
        
        return p;
    }
    
    // 计算三次贝塞尔曲线上的点
    private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector3 p = uuu * p0; // (1-t)^3 * P0
        p += 3 * uu * t * p1; // 3 * (1-t)^2 * t * P1
        p += 3 * u * tt * p2; // 3 * (1-t) * t^2 * P2
        p += ttt * p3; // t^3 * P3
        
        return p;
    }
    
    // 绘制折线路径
    private void DrawSegmentedPath()
    {
        if (controlPoints == null || controlPoints.Length == 0)
        {
            Debug.LogWarning("折线需要控制点！回退到直线路径。");
            DrawStraightPath();
            return;
        }
        
        // 设置线段数量
        lineRenderer.positionCount = controlPoints.Length + 2;
        
        // 设置起点
        lineRenderer.SetPosition(0, nodeA.transform.position);
        
        // 设置中间点
        for (int i = 0; i < controlPoints.Length; i++)
        {
            lineRenderer.SetPosition(i + 1, controlPoints[i].position);
        }
        
        // 设置终点
        lineRenderer.SetPosition(controlPoints.Length + 1, nodeB.transform.position);
    }
    
    // 获取在路径上给定位置的点（t从0到1）
    public Vector3 GetPointOnPath(float t)
    {
        t = Mathf.Clamp01(t); // 确保t在0到1之间
        
        switch (pathType)
        {
            case PathType.Straight:
                return Vector3.Lerp(nodeA.transform.position, nodeB.transform.position, t);
                
            case PathType.Bezier:
                if (controlPoints == null || controlPoints.Length == 0)
                    return Vector3.Lerp(nodeA.transform.position, nodeB.transform.position, t);
                    
                // 对于贝塞尔曲线，使用弧长参数化实现匀速运动
                if (useArcLengthParameterization)
                {
                    // 使用弧长参数化t值，实现匀速移动
                    t = ArcLengthParameterize(t);
                }
                    
                if (controlPoints.Length == 1)
                    return CalculateQuadraticBezierPoint(t, nodeA.transform.position, controlPoints[0].position, nodeB.transform.position);
                    
                return CalculateCubicBezierPoint(t, nodeA.transform.position, controlPoints[0].position, controlPoints[1].position, nodeB.transform.position);
                
            case PathType.Segmented:
                if (controlPoints == null || controlPoints.Length == 0)
                    return Vector3.Lerp(nodeA.transform.position, nodeB.transform.position, t);
                    
                // 计算每段线段的总长度
                float totalLength = 0;
                List<float> segmentLengths = new List<float>();
                
                Vector3 prevPoint = nodeA.transform.position;
                
                // 添加每段的长度
                foreach (Transform point in controlPoints)
                {
                    float length = Vector3.Distance(prevPoint, point.position);
                    segmentLengths.Add(length);
                    totalLength += length;
                    prevPoint = point.position;
                }
                
                // 添加最后一段到终点
                float lastLength = Vector3.Distance(prevPoint, nodeB.transform.position);
                segmentLengths.Add(lastLength);
                totalLength += lastLength;
                
                // 现在，基于距离找到正确的线段
                float targetDistance = t * totalLength;
                float distanceSoFar = 0;
                
                prevPoint = nodeA.transform.position;
                for (int i = 0; i < segmentLengths.Count; i++)
                {
                    Vector3 nextPoint;
                    if (i < controlPoints.Length)
                        nextPoint = controlPoints[i].position;
                    else
                        nextPoint = nodeB.transform.position;
                        
                    float segmentLength = segmentLengths[i];
                    
                    if (distanceSoFar + segmentLength >= targetDistance)
                    {
                        // 找到了包含目标点的线段
                        float segmentT = (targetDistance - distanceSoFar) / segmentLength;
                        return Vector3.Lerp(prevPoint, nextPoint, segmentT);
                    }
                    
                    distanceSoFar += segmentLength;
                    prevPoint = nextPoint;
                }
                
                // 如果到这里，说明计算有误，返回终点
                return nodeB.transform.position;
                
            default:
                return Vector3.Lerp(nodeA.transform.position, nodeB.transform.position, t);
        }
    }
    
    // 控制是否使用弧长参数化（用于实现匀速移动）
    public bool useArcLengthParameterization = true;
    
    // 贝塞尔曲线的弧长参数化查找表大小
    private const int ARC_LENGTH_SAMPLES = 100;
    private float[] arcLengthLUT = null;
    private float totalArcLength = 0f;
    
    // 初始化弧长查找表
    private void InitializeArcLengthLUT()
    {
        if (arcLengthLUT != null) return;
        
        arcLengthLUT = new float[ARC_LENGTH_SAMPLES + 1];
        
        // 设置起点距离为0
        arcLengthLUT[0] = 0f;
        
        Vector3 prevPoint;
        if (controlPoints.Length == 1)
            prevPoint = nodeA.transform.position;
        else
            prevPoint = nodeA.transform.position;
        
        // 计算每个样本点之间的距离
        float accumDistance = 0f;
        for (int i = 1; i <= ARC_LENGTH_SAMPLES; i++)
        {
            float t = i / (float)ARC_LENGTH_SAMPLES;
            Vector3 currentPoint;
            
            if (controlPoints.Length == 1)
                currentPoint = CalculateQuadraticBezierPoint(t, nodeA.transform.position, controlPoints[0].position, nodeB.transform.position);
            else
                currentPoint = CalculateCubicBezierPoint(t, nodeA.transform.position, controlPoints[0].position, controlPoints[1].position, nodeB.transform.position);
            
            accumDistance += Vector3.Distance(prevPoint, currentPoint);
            arcLengthLUT[i] = accumDistance;
            prevPoint = currentPoint;
        }
        
        // 记录曲线的总长度
        totalArcLength = accumDistance;
    }
    
    // 使用弧长参数化t值，实现匀速移动
    private float ArcLengthParameterize(float t)
    {
        // 确保查找表已初始化
        if (arcLengthLUT == null)
        {
            InitializeArcLengthLUT();
        }
        
        // 计算目标距离
        float targetDistance = t * totalArcLength;
        
        // 在查找表中找到最接近的段
        int low = 0;
        int high = ARC_LENGTH_SAMPLES;
        
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (arcLengthLUT[mid] < targetDistance)
                low = mid + 1;
            else
                high = mid;
        }
        
        // 确保索引在有效范围内
        int index = Mathf.Clamp(low, 1, ARC_LENGTH_SAMPLES);
        
        // 计算两个采样点之间的插值系数
        float lengthBefore = arcLengthLUT[index - 1];
        float lengthAfter = arcLengthLUT[index];
        float segmentLength = lengthAfter - lengthBefore;
        
        // 如果线段长度为0，直接返回当前t值
        if (segmentLength < 0.0001f)
            return (index - 1) / (float)ARC_LENGTH_SAMPLES;
        
        // 计算在这个段内的位置
        float segmentFactor = (targetDistance - lengthBefore) / segmentLength;
        
        // 返回参数化后的t值
        return ((index - 1) + segmentFactor) / ARC_LENGTH_SAMPLES;
    }
    
    // 根据插值参数获取路径上的方向
    public Vector3 GetDirectionAtPoint(float t)
    {
        // 为了获得在点t的方向，我们计算t附近的两个点，然后计算它们之间的方向
        const float delta = 0.01f;
        
        Vector3 p1 = GetPointOnPath(Mathf.Max(0, t - delta));
        Vector3 p2 = GetPointOnPath(Mathf.Min(1, t + delta));
        
        // 确保点不相同
        if (p1 == p2)
        {
            // 如果是起点
            if (t <= delta)
                return (GetPointOnPath(t + 2 * delta) - p1).normalized;
                
            // 如果是终点
            if (t >= 1 - delta)
                return (p1 - GetPointOnPath(t - 2 * delta)).normalized;
        }
        
        return (p2 - p1).normalized;
    }
    
    // 根据路径上一个点的位置找到最近的t值
    public float GetClosestTFromPoint(Vector3 point)
    {
        // 对于简单的直线，我们可以用向量投影
        if (pathType == PathType.Straight)
        {
            Vector3 pathStart = nodeA.transform.position;
            Vector3 pathEnd = nodeB.transform.position;
            Vector3 pathDirection = pathEnd - pathStart;
            float pathLength = pathDirection.magnitude;
            
            if (pathLength == 0)
                return 0;
                
            pathDirection = pathDirection / pathLength;
            
            Vector3 pointVector = point - pathStart;
            float projection = Vector3.Dot(pointVector, pathDirection);
            
            return Mathf.Clamp01(projection / pathLength);
        }
        
        // 对于曲线或折线，我们用采样搜索
        float bestT = 0;
        float closestDistanceSqr = float.MaxValue;
        
        int steps = 100;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 pathPoint = GetPointOnPath(t);
            float distSqr = (pathPoint - point).sqrMagnitude;
            
            if (distSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distSqr;
                bestT = t;
            }
        }
        
        return bestT;
    }
    
    // 给定一个节点，返回路径上的另一个节点
    public MapNode GetOtherNode(MapNode node)
    {
        if (node == nodeA)
            return nodeB;
        else if (node == nodeB)
            return nodeA;
        else
            return null; // 如果给定的节点不是路径的端点
    }
    
    // 获取从给定节点出发沿路径的方向
    public Vector2 GetDirectionFrom(MapNode node)
    {
        if (node == nodeA)
        {
            // 从A到B的方向
            return (Vector2)(nodeB.transform.position - nodeA.transform.position).normalized;
        }
        else if (node == nodeB)
        {
            // 从B到A的方向
            return (Vector2)(nodeA.transform.position - nodeB.transform.position).normalized;
        }
        
        // 如果不是端点，返回零向量
        return Vector2.zero;
    }
    
    // 在Unity编辑器中绘制可视化辅助线
    private void OnDrawGizmos()
    {
        if (nodeA != null && nodeB != null)
        {
            Gizmos.color = Color.yellow;
            
            if (pathType == PathType.Straight)
            {
                Gizmos.DrawLine(nodeA.transform.position, nodeB.transform.position);
            }
            else if (pathType == PathType.Bezier && controlPoints != null && controlPoints.Length > 0)
            {
                // 绘制贝塞尔曲线的控制点
                Gizmos.color = Color.cyan;
                
                // 绘制到第一个控制点
                if (controlPoints.Length >= 1)
                {
                    Gizmos.DrawLine(nodeA.transform.position, controlPoints[0].position);
                    Gizmos.DrawWireSphere(controlPoints[0].position, 0.2f);
                }
                
                // 如果有第二个控制点
                if (controlPoints.Length >= 2)
                {
                    Gizmos.DrawLine(controlPoints[0].position, controlPoints[1].position);
                    Gizmos.DrawLine(controlPoints[1].position, nodeB.transform.position);
                    Gizmos.DrawWireSphere(controlPoints[1].position, 0.2f);
                }
                else if (controlPoints.Length == 1)
                {
                    Gizmos.DrawLine(controlPoints[0].position, nodeB.transform.position);
                }
                
                // 使用较多的点绘制曲线的近似形状
                Gizmos.color = Color.yellow;
                Vector3 prevPoint = nodeA.transform.position;
                for (int i = 1; i <= 20; i++)
                {
                    float t = i / 20f;
                    Vector3 point = GetPointOnPath(t);
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }
            else if (pathType == PathType.Segmented && controlPoints != null && controlPoints.Length > 0)
            {
                // 绘制折线的每一段
                Vector3 prevPoint = nodeA.transform.position;
                
                // 绘制到每个控制点
                foreach (Transform point in controlPoints)
                {
                    Gizmos.DrawLine(prevPoint, point.position);
                    Gizmos.DrawWireSphere(point.position, 0.2f);
                    prevPoint = point.position;
                }
                
                // 绘制最后一段到终点
                Gizmos.DrawLine(prevPoint, nodeB.transform.position);
            }
        }
    }
} 