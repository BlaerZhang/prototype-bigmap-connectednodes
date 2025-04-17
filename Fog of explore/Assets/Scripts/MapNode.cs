using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNode : MonoBehaviour
{
    [Header("节点设置")]
    public string nodeName = "未命名节点";
    
    [Header("连接路径")]
    public List<MapPath> connectedPaths = new List<MapPath>();
    
    // 可见性控制
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    // 将指定路径添加到节点的连接路径列表中
    public void AddConnectedPath(MapPath path)
    {
        if (!connectedPaths.Contains(path))
        {
            connectedPaths.Add(path);
        }
    }
    
    // 获取相邻节点列表
    public List<MapNode> GetConnectedNodes()
    {
        List<MapNode> connectedNodes = new List<MapNode>();
        
        foreach (MapPath path in connectedPaths)
        {
            MapNode otherNode = path.GetOtherNode(this);
            if (otherNode != null)
            {
                connectedNodes.Add(otherNode);
            }
        }
        
        return connectedNodes;
    }
    
    // 根据输入方向获取最接近的路径
    public MapPath GetPathInDirection(Vector2 direction)
    {
        if (connectedPaths.Count == 0)
            return null;
            
        MapPath bestPath = null;
        float bestDot = -1f; // 最小的点积值为-1
        
        foreach (MapPath path in connectedPaths)
        {
            // 获取从当前节点到路径另一端的方向
            Vector2 pathDirection = path.GetDirectionFrom(this);
            
            // 计算方向的点积（dot product）
            float dot = Vector2.Dot(direction.normalized, pathDirection.normalized);
            
            // 如果这个方向比之前找到的更接近输入方向
            if (dot > bestDot)
            {
                bestDot = dot;
                bestPath = path;
            }
        }
        
        // 如果最佳点积太负（方向相反），可能需要返回null
        // 或者设定一个阈值，例如只有当dot>0时才返回路径
        if (bestDot < 0)
            return null;
            
        return bestPath;
    }
    
    // 在Unity编辑器中绘制可视化辅助线
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 绘制节点名称
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, nodeName);
        #endif
    }
} 