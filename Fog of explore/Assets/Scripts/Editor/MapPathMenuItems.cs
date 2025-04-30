using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

// 路径类型样式类
public class PathTypeStyle
{
    public Color color;
    public float width;
    public Material material;
    public string displayName;
    
    public PathTypeStyle(Color color, float width, string displayName)
    {
        this.color = color;
        this.width = width;
        this.displayName = displayName;
    }
}

public static class MapPathMenuItems
{
    // 菜单项分类
    private const string MENU_ROOT = "GameObject/Map Paths/";
    
    // 应用样式到所有选中的路径
    [MenuItem(MENU_ROOT + "应用当前样式到选中路径", false, 50)]
    private static void ApplyStylesToSelected()
    {
        // 获取所有选中的MapPath对象
        List<MapPath> selectedPaths = new List<MapPath>();
        
        foreach (GameObject obj in Selection.gameObjects)
        {
            MapPath path = obj.GetComponent<MapPath>();
            if (path != null)
            {
                selectedPaths.Add(path);
            }
        }
        
        if (selectedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择至少一个包含MapPath组件的游戏对象", "确定");
            return;
        }
        
        // 加载样式配置
        PathTypeStyleConfig styleConfig = LoadStyleConfig();
        
        if (styleConfig == null)
        {
            EditorUtility.DisplayDialog("错误", "找不到路径样式配置资源。请先创建样式配置。", "确定");
            return;
        }
        
        // 应用样式到每个选中的路径
        int appliedCount = 0;
        foreach (MapPath path in selectedPaths)
        {
            MapPath.RoadType roadType = path.roadType;
            
            // 获取样式
            PathTypeStyleConfig.PathTypeStyle style = styleConfig.GetStyleForRoadType(roadType);
            
            LineRenderer lineRenderer = path.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                // 记录撤销操作
                Undo.RecordObject(lineRenderer, "应用路径样式");
                
                // 应用样式
                path.ApplyRoadTypeStyle(style.color, style.width, style.material);
                
                appliedCount++;
            }
        }
        
        EditorUtility.DisplayDialog("完成", $"已应用样式到 {appliedCount} 条路径", "确定");
    }
    
    // 修改所有选中路径的类型
    [MenuItem(MENU_ROOT + "批量修改路径类型", false, 51)]
    private static void ChangeSelectedPathTypes()
    {
        // 获取所有选中的MapPath对象
        List<MapPath> selectedPaths = new List<MapPath>();
        
        foreach (GameObject obj in Selection.gameObjects)
        {
            MapPath path = obj.GetComponent<MapPath>();
            if (path != null)
            {
                selectedPaths.Add(path);
            }
        }
        
        if (selectedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择至少一个包含MapPath组件的游戏对象", "确定");
            return;
        }
        
        // 创建类型选择窗口
        RoadTypeSelectionWindow.ShowWindow(selectedPaths.ToArray());
    }
    
    // 检查菜单项是否应该启用
    [MenuItem(MENU_ROOT + "应用当前样式到选中路径", true)]
    [MenuItem(MENU_ROOT + "批量修改路径类型", true)]
    private static bool ValidatePathSelected()
    {
        // 检查是否至少有一个选中的对象包含MapPath组件
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj.GetComponent<MapPath>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // 加载样式配置
    public static PathTypeStyleConfig LoadStyleConfig()
    {
        PathTypeStyleConfig config = Resources.Load<PathTypeStyleConfig>("PathTypeStyles");
        
        if (config == null)
        {
            Debug.LogWarning("找不到路径样式配置资源。请在Resources文件夹中创建PathTypeStyles资产。");
        }
        
        return config;
    }
}

// 路径类型选择窗口
public class RoadTypeSelectionWindow : EditorWindow
{
    private MapPath[] targetPaths;
    private MapPath.RoadType selectedRoadType = MapPath.RoadType.Main;
    private Dictionary<MapPath.RoadType, string> displayNames = new Dictionary<MapPath.RoadType, string>();
    private bool applyStyleAfterChange = true;
    private PathTypeStyleConfig styleConfig;
    
    public static void ShowWindow(MapPath[] paths)
    {
        RoadTypeSelectionWindow window = GetWindow<RoadTypeSelectionWindow>("路径类型选择");
        window.targetPaths = paths;
        window.minSize = new Vector2(300, 200);
        window.styleConfig = MapPathMenuItems.LoadStyleConfig();
        window.LoadDisplayNames();
    }
    
    private void LoadDisplayNames()
    {
        // 默认显示名称
        foreach (MapPath.RoadType type in System.Enum.GetValues(typeof(MapPath.RoadType)))
        {
            displayNames[type] = type.ToString();
        }
        
        // 从样式配置加载显示名称
        if (styleConfig != null)
        {
            foreach (MapPath.RoadType type in System.Enum.GetValues(typeof(MapPath.RoadType)))
            {
                PathTypeStyleConfig.PathTypeStyle style = styleConfig.GetStyleForRoadType(type);
                if (!string.IsNullOrEmpty(style.displayName))
                {
                    displayNames[type] = style.displayName;
                }
            }
        }
    }
    
    private void OnGUI()
    {
        if (targetPaths == null || targetPaths.Length == 0)
        {
            EditorGUILayout.HelpBox("未选择有效路径", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField($"批量修改 {targetPaths.Length} 条路径的类型", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 显示当前选择的路径
        EditorGUILayout.LabelField("选中的路径:", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        foreach (MapPath path in targetPaths)
        {
            EditorGUILayout.LabelField(path.name);
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
        
        // 选择新的路径类型
        EditorGUILayout.LabelField("选择新的路径类型:", EditorStyles.boldLabel);
        
        foreach (MapPath.RoadType type in System.Enum.GetValues(typeof(MapPath.RoadType)))
        {
            string displayName = displayNames.ContainsKey(type) ? displayNames[type] : type.ToString();
            bool isSelected = GUILayout.Toggle(selectedRoadType == type, $"{type} ({displayName})");
            
            if (isSelected && selectedRoadType != type)
            {
                selectedRoadType = type;
            }
        }
        
        EditorGUILayout.Space();
        
        // 选择是否同时应用样式
        applyStyleAfterChange = EditorGUILayout.Toggle("同时应用样式", applyStyleAfterChange);
        
        // 如果没有样式配置，显示警告
        if (styleConfig == null && applyStyleAfterChange)
        {
            EditorGUILayout.HelpBox("找不到样式配置资源。样式将无法应用。", MessageType.Warning);
            
            if (GUILayout.Button("创建样式配置"))
            {
                PathTypeStyleConfigUtility.CreatePathTypeStyleConfig();
                styleConfig = MapPathMenuItems.LoadStyleConfig();
                LoadDisplayNames();
            }
        }
        
        EditorGUILayout.Space();
        
        // 应用按钮
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("应用修改", GUILayout.Height(30)))
        {
            ApplyRoadTypeChange();
            Close();
        }
        GUI.backgroundColor = Color.white;
        
        // 取消按钮
        if (GUILayout.Button("取消"))
        {
            Close();
        }
    }
    
    private void ApplyRoadTypeChange()
    {
        // 应用变更到每条路径
        foreach (MapPath path in targetPaths)
        {
            // 记录撤销操作
            Undo.RecordObject(path, "更改路径类型");
            
            // 修改类型
            path.roadType = selectedRoadType;
            
            // 如果需要，同时应用样式
            if (applyStyleAfterChange && styleConfig != null)
            {
                PathTypeStyleConfig.PathTypeStyle style = styleConfig.GetStyleForRoadType(selectedRoadType);
                
                LineRenderer lineRenderer = path.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    Undo.RecordObject(lineRenderer, "应用路径样式");
                    path.ApplyRoadTypeStyle(style.color, style.width, style.material);
                }
            }
            
            // 标记为已修改
            EditorUtility.SetDirty(path);
        }
        
        // 提示完成
        EditorUtility.DisplayDialog("完成", $"已将 {targetPaths.Length} 条路径的类型修改为 {selectedRoadType}", "确定");
    }
} 